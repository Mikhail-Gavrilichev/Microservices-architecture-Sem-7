# AGENTS.md — правила для AI-агентов

Этот файл — основной контекст для AI-агентов (Codex, Claude Code, OpenCode, Cursor и др.),
которые меняют репозиторий. Прочитайте его целиком до начала работы. При конфликте
инструкций пользователя и этого файла приоритет у инструкций пользователя; отступления
от архитектурных правил фиксируйте в ответе.

## 1. Что это за проект

Учебный проект по микросервисной архитектуре, 7 семестр. Реализованы ровно **два**
микросервиса по EventStorming-диаграмме `Tournament Management Flow.png`:

- **TaskService** — агрегат **«Задача»** (`GameTask`);
- **ReviewService** — агрегат **«Запрос на проверку»** (`ReviewRequest`).

Стек: **.NET 10 / ASP.NET Core 10**, C# 13, MediatR 12, FluentValidation 12,
EF Core 10 + Npgsql, PostgreSQL, Swagger (Swashbuckle), xUnit, Docker Compose.
Пользовательский интерфейс: **React 19 + Vite 8 + TypeScript 6 + React Router 7**,
в Docker раздаётся через **nginx**.

## 2. Границы задачи

### Входит в scope

- домен, приложение, инфраструктура и API двух перечисленных агрегатов;
- интеграция ReviewService → TaskService для получения снимка опубликованной задачи;
- React SPA для сквозного сценария: задачи, проверки, очередь, кабинет жюри;
- миграции EF Core, Docker Compose, тесты, документация.

### Не входит в scope

Не реализовывайте и не добавляйте заготовки для агрегатов:
«Турнир», «Игра», «Команда», «Решение», «Апелляция», «Вопрос», «Итоги турнира»,
«Диплом», «Форма», «Участие в турнире», «Счёт игры», «Набор форм».

Идентификаторы `SolutionId`, `TeamId`, `SourceReferenceId` в ReviewService — это
**внешние ссылки**, а не повод создавать локальные сущности «Решение» или «Команда».
Не добавляйте таблицы, EF-конфигурации и доменные классы для чужих агрегатов.

## 3. Карта репозитория

```text
src/TaskService/
  TaskService.Domain/          # GameTask, GameTaskStatus, доменные события
  TaskService.Application/     # команды, запросы, DTO, валидаторы, интерфейсы
  TaskService.Infrastructure/  # TaskDbContext, EF-конфигурации, репозиторий, миграции
  TaskService.Api/             # контроллеры, контракты, Program.cs, Dockerfile

src/ReviewService/
  ReviewService.Domain/          # ReviewRequest, статусы, вердикты, события
  ReviewService.Application/     # команды, запросы, DTO, валидаторы, интерфейсы
  ReviewService.Infrastructure/  # ReviewDbContext, EF, TaskCatalogClient, миграции
  ReviewService.Api/             # контроллеры, контракты, Program.cs, Dockerfile

frontend/
  src/api/                       # типизированные клиенты и типы HTTP-контрактов
  src/components/                # layout, модальные окна, drawer-ы, общие состояния
  src/hooks/                     # useJuryId и localStorage
  src/lib/                       # форматирование, статусы, pluralize
  src/pages/                     # dashboard, tasks, reviews, queue, jury, 404
  src/styles.css                 # дизайн-система и адаптивность
  Dockerfile                     # Vite build + nginx runtime
  nginx.conf                     # SPA fallback и same-origin proxy
  vite.config.ts                 # dev-прокси к TaskService и ReviewService

tests/
  <Service>.Domain.Tests/
  <Service>.Application.Tests/
  <Service>.Api.Tests/
```

## 4. Архитектурные правила

### 4.1. Направление зависимостей

```text
Domain ← Application ← Infrastructure
   ↑          ↑              ↑
   └──────────┴──────────────┴──── Api
```

- `*.Domain` **не зависит** ни от каких других проектов и NuGet-пакетов (кроме BCL).
  Здесь нет EF Core, MediatR, ASP.NET Core, JSON-атрибутов и HTTP.
- `*.Application` зависит только от своего `Domain` и прикладных пакетов
  (MediatR, FluentValidation). Здесь **нельзя** использовать `DbContext`, EF Core,
  `HttpClient`, контроллеры и `ProblemDetails`.
- `*.Infrastructure` реализует интерфейсы Application: EF Core, репозитории,
  `IUnitOfWork`, HTTP-клиенты. Здесь допустимы Npgsql, EF Core, `HttpClient`.
- `*.Api` — composition root: DI, middleware, контроллеры, HTTP-контракты, Swagger.
  Контроллеры не содержат бизнес-логики, только вызывают `ISender.Send`.
- **Между TaskService и ReviewService не должно быть ссылок на проекты, общих
  NuGet-пакетов с доменом, общих таблиц или общего кода.** Только HTTP-контракт.

### 4.2. CQRS

- Команда — отдельный класс `*Command` + `*CommandHandler` + `*CommandValidator`.
- Запрос — отдельный класс `*Query` + `*QueryHandler` (+ валидатор, если есть параметры).
- Обработчик команды загружает агрегат через репозиторий, вызывает доменный метод,
  сохраняет через `IUnitOfWork.SaveChangesAsync`.
- Обработчик запроса не меняет состояние и возвращает DTO, а не доменный агрегат.
- Не добавляйте `SaveChanges` в контроллеры и репозитории. Единица работы — команда.

### 4.3. DTO и домен

- Доменный агрегат не сериализуется в HTTP-ответы. Всегда используйте DTO.
- DTO — `sealed record` с фабрикой `FromDomain` (см. существующие примеры).
- HTTP-контракты (`*Request`) живут в `*.Api/Contracts` и не переиспользуются
  в Application.
- Не добавляйте в домен свойства «на будущее», если они не нужны текущим инвариантам.

### 4.4. Frontend

- `frontend/` не ссылается на .NET-проекты и не делит с ними код; связь только
  через HTTP-контракты.
- Браузер использует относительные `/task-api` и `/review-api`. В dev их проксирует
  Vite, в Docker — nginx. Не включайте permissive CORS в сервисах ради UI.
- Все запросы идут через типизированные клиенты `frontend/src/api`; не добавляйте
  разрозненные `fetch` в страницах и компонентах.
- Кабинет жюри (`JuryPage`, `JuryReviewDrawer`) использует только jury-эндпоинты
  и `JuryReviewDto`. Административные данные отображаются только в `/reviews`.
- `Modal` и `Drawer` рендерятся через `createPortal` в `document.body`: страницы
  используют анимации/transform, создающие stacking context. Не заменяйте портал
  на обычный `position: fixed` внутри страницы.
- Изменения черновика задачи отправляются последовательно: каждый PUT загружает
  и сохраняет весь агрегат, параллельные запросы могут перезаписать друг друга.
- Аутентификации нет; ID жюри хранится в `localStorage` только для демо и считается
  доверенным. Не добавляйте «наполовину» auth без отдельного согласования.

## 5. Доменные инварианты

### 5.1. GameTask (TaskService)

Статусы: `Draft = 1`, `Published = 2`.

- Создание: `Title` обязателен, максимум 200 символов; `Statement` — до 4000;
  `CorrectAnswer` — до 2000. Создаётся в `Draft`.
- `ChangeStatement` и `ChangeCorrectAnswer` разрешены **только в `Draft`**.
- `Publish` требует непустых `Statement` и `CorrectAnswer`; повторная публикация
  запрещена; после публикации задача неизменяема.
- `UpdatedAt` меняется при изменении; `PublishedAt` — только при публикации.
- Доменные события: `TaskCreated`, `TaskStatementChanged`, `TaskCorrectAnswerChanged`,
  `TaskPublished`.
- `GET /api/tasks/{id}/snapshot` отдаёт **только опубликованную** задачу.
  Черновик → `409 Conflict`, отсутствующая задача → `404`.

### 5.2. ReviewRequest (ReviewService)

Статусы: `Created`, `Queued`, `Assigned`, `Accepted`, `Refused`,
`TransferredToSenior`, `Completed`.

- `Create` требует непустых `SolutionId`, `TaskId`, `TeamId`, `ParticipantAnswer`;
  `Source = Appeal` требует `SourceReferenceId`.
- Условие задачи и правильный ответ приходят из TaskService в момент создания
  и сохраняются в агрегате. **Не запрашивайте TaskService повторно при проверке.**
- `Enqueue`: только из `Created`.
- `Assign`: только из `Queued`.
- `Accept`: из `Assigned` или `TransferredToSenior`.
- `Refuse`: из `Assigned`, `Accepted` или `TransferredToSenior`; причина обязательна.
- `TransferToSenior`: только из `Refused`; нельзя передать тому же жюри,
  которое отказалось.
- `Complete`: из `Assigned`, `Accepted` или `TransferredToSenior`; вердикт
  `Correct`/`Incorrect`; комментарий до 2000 символов.
- Все действия жюри (`accept`, `refuse`, `complete`) проверяют, что проверка
  назначена именно этому `JuryMemberId`.
- `assign-next` берёт самый ранний элемент очереди; пустая очередь → `204`.

Любое изменение переходов — это изменение бизнес-правил. Сначала обновите тесты
домена и таблицу в README, затем код.

## 6. Приватность (критично)

Требование диаграммы: **жюри проверяет решение, не зная команду и её рейтинг.**

- `JuryReviewDto` и `ReviewQueueItemDto` **никогда** не должны содержать
  `TeamId`, `SolutionId`, `SourceReferenceId`, название команды и рейтинг.
- Административные данные доступны только в `ReviewRequestDetailsDto`.
- Не «прячьте» поля через `[JsonIgnore]` в общем DTO. Разделяйте DTO по сценариям.
- Не добавляйте team-данные в логи, метрики, трассировки и тексты ошибок для
  jury-эндпоинтов.
- Во frontend не расширяйте `JuryReviewDto` и не запрашивайте административные
  эндпоинты из `JuryPage`/`JuryReviewDrawer`; данные команды не должны попадать
  даже в props jury-компонентов.
- Тест `tests/ReviewService.Api.Tests/ReviewsApiTests.cs` проверяет отсутствие
  `teamId`/`solutionId` в JSON. Его нельзя удалять или ослаблять.

## 7. API-соглашения

- Маршруты: `api/tasks`, `api/review-requests`, `api/reviews`; kebab-case.
- Создание → `201 Created` + `CreatedAtAction`; команды без результата → `204`;
  чтение → `200` + DTO.
- Ошибки — `ProblemDetails` (RFC 7807) через `GlobalExceptionHandler`:
  - `ValidationException` → `400`;
  - `NotFoundException` → `404`;
  - `ConflictException` → `409`;
  - `DomainException` → `422`;
  - `ExternalServiceException` → `503`.
- Не возвращайте `500` для ожидаемых бизнес-ошибок.
- Перечисления сериализуются строками (`JsonStringEnumConverter`).
- Swagger-комментарии (`<summary>`) — на русском, как в существующем коде.
- Новые эндпоинты обязательно описывайте атрибутами `ProducesResponseType`.
- Валидация формата — FluentValidation в Application; бизнес-инварианты — в домене.
  Не дублируйте доменные проверки в валидаторах, если они уже есть в агрегате.

## 8. Персистенция и EF Core

- У каждого сервиса свой `DbContext` и своя база. Никаких общих миграций.
- Конфигурации — отдельные классы `IEntityTypeConfiguration<T>` в
  `Persistence/Configurations`.
- Обязательные поля — `IsRequired()`, длины — `HasMaxLength(...)`.
- Приватные сеттеры и конструктор для EF сохраняются.
- Новые миграции создавайте только через `dotnet ef migrations add`; не редактируйте
  уже применённые миграции вручную.
- Время — `DateTimeOffset` в UTC через `TimeProvider`; не используйте
  `DateTime.Now`/`DateTime.UtcNow` напрямую в обработчиках.
- Не включайте lazy loading и не возвращайте `IQueryable` из репозиториев.
- Для API-тестов используется EF InMemory с фиксированным именем базы; не меняйте
  фабрики без необходимости.

## 9. Стиль кода

- File-scoped namespaces, `sealed` для классов, которые не предполагают наследование.
- `record` для DTO, команд, запросов и HTTP-контрактов.
- Primary constructors для handler-ов и клиентов — как в существующем коде.
- Nullable reference types включены; не подавляйте предупреждения без причины.
- Асинхронные методы принимают `CancellationToken` последним параметром и передают его.
- Сообщения доменных исключений и валидации — на русском.
- Не добавляйте `#region`, лишние абстракции и «универсальные» базовые репозитории.
- Соблюдайте `.editorconfig`; перед завершением проверяйте сборку без новых
  предупреждений.

## 10. Тесты

- Фреймворк — xUnit. Имена тестов: `Method_Scenario_ExpectedResult` или
  содержательные русские/английские предложения в существующем стиле.
- Каждое новое правило домена сопровождается тестом в `*.Domain.Tests`.
- Каждый обработчик — тестом в `*.Application.Tests` (успех + ключевая ошибка).
- Новый HTTP-контракт — тестом в `*.Api.Tests` через `WebApplicationFactory`.
- Тесты не должны зависеть от реального PostgreSQL, сети или порядка запуска.
- Запрещено удалять/пропускать существующие тесты ради «зелёного» прогона.
- Для frontend минимальная проверка — `npm run build` и `npm run lint`; ручной
  smoke-тест обязателен для изменений приватности и jury-сценариев.
- Минимальный набор перед сдачей: `dotnet test TournamentManagement.sln`,
  `npm run build`, `npm run lint`.

## 11. Команды

```bash
# сборка
dotnet build TournamentManagement.sln

# тесты
dotnet test TournamentManagement.sln

# запуск сервисов локально
dotnet run --project src/TaskService/TaskService.Api
dotnet run --project src/ReviewService/ReviewService.Api

# frontend: dev с прокси на 5101/5102
cd frontend
npm ci
npm run dev

# frontend: проверка перед коммитом
npm run build
npm run lint

# миграции
dotnet ef migrations add <Name> \
  --project src/TaskService/TaskService.Infrastructure \
  --startup-project src/TaskService/TaskService.Api \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/TaskService/TaskService.Infrastructure \
  --startup-project src/TaskService/TaskService.Api

# Docker
docker compose up --build -d
docker compose down
docker compose down -v
```

Порты: frontend — `3000` (Docker) и `5173` (Vite dev), TaskService — `5101`,
ReviewService — `5102`, task-db — `5432`, review-db — `5433`. Внутри Docker
frontend слушает `80`, сервисы — `8080`.

## 12. Definition of Done

Задача считается выполненной, только если:

- [ ] `dotnet build TournamentManagement.sln` завершается без ошибок;
- [ ] `dotnet test TournamentManagement.sln` — все тесты проходят;
- [ ] `npm run build` и `npm run lint` в `frontend/` проходят без ошибок;
- [ ] новые бизнес-правила покрыты тестами;
- [ ] приватность jury-DTO не нарушена, включая frontend-компоненты;
- [ ] миграция создана, если менялась схема;
- [ ] обновлены README (API/поведение) и, при необходимости, этот файл;
- [ ] `docker compose up --build -d` поднимает оба сервиса и frontend,
      `/health` и `/healthz` отвечают `Healthy`/`ok`;
- [ ] в ответе пользователю перечислены изменённые файлы и команды проверки.

## 13. Чего делать нельзя

- Реализовывать агрегаты вне scope или добавлять им таблицы/эндпоинты.
- Связывать сервисы общей БД, общими проектами, общими доменными классами.
- Добавлять в `JuryReviewDto`/`ReviewQueueItemDto` данные команды, решения или рейтинг.
- Включать permissive CORS в сервисах ради frontend: используйте same-origin proxy.
- Запрашивать административные эндпоинты проверки из кабинета жюри или передавать
  team-данные в props jury-компонентов.
- Вызывать TaskService из обработчиков accept/refuse/complete: снимок уже сохранён.
- Возвращать доменные агрегаты из API или репозиториев.
- Делать `SaveChanges` в контроллере, репозитории или валидаторе.
- Менять применённые миграции и вручную править snapshot модели.
- Коммитить секреты, пароли production, строки подключения к реальным БД.
- Удалять существующие тесты, особенно тест приватности, чтобы обойти ошибку.
- Добавлять аутентификацию/авторизацию «наполовину» без отдельного согласования:
  текущий срез сознательно не содержит auth, и это описано в README.
