# Tournament Management — сервисы «Задача» и «Запрос на проверку»

Учебный проект по микросервисной архитектуре (7 семестр). В репозитории реализованы
два сервиса по EventStorming-диаграмме
[`Tournament Management Flow.png`](Tournament%20Management%20Flow.png):

- **TaskService** — агрегат **«Задача»** (подготовка и публикация игровых задач);
- **ReviewService** — агрегат **«Запрос на проверку»** (очередь проверок, назначение жюри,
  вердикт).

Остальные агрегаты диаграммы («Турнир», «Игра», «Команда», «Решение», «Апелляция»,
«Вопрос», «Итоги турнира», «Диплом», «Форма», «Участие в турнире», «Счёт игры»,
«Набор форм») в рамках этой работы **не реализуются**.

**Авторы:** Гавриличев М.А., Кокорин А.В., Ильин М.В.

## Содержание

- [Что реализовано](#что-реализовано)
- [Соответствие EventStorming](#соответствие-eventstorming)
- [Архитектура](#архитектура)
- [Структура репозитория](#структура-репозитория)
- [Запуск через Docker Compose](#запуск-через-docker-compose)
- [Локальный запуск](#локальный-запуск)
- [API и сквозной сценарий](#api-и-сквозной-сценарий)
- [Модели состояний](#модели-состояний)
- [Ошибки](#ошибки)
- [Приватность данных жюри](#приватность-данных-жюри)
- [Тесты](#тесты)
- [Проектные решения и ограничения](#проектные-решения-и-ограничения)

## Что реализовано

| Возможность | TaskService | ReviewService |
|---|---|---|
| Создание агрегата | создать задачу | создать запрос на проверку |
| Редактирование | условие и правильный ответ только в `Draft` | — |
| Публикация | `Draft → Published`, после публикации изменения запрещены | — |
| Очередь | — | `Created → Queued`, выдача очереди |
| Назначение | — | конкретному жюри или «следующему доступному» |
| Работа жюри | — | принять, отказаться, передать старшему, завершить с вердиктом |
| Чтение | список с фильтрами, карточка, внутренний snapshot | список для смотрителя, очередь, проверки жюри |
| Интеграция | отдаёт снимок опубликованной задачи | получает снимок по HTTP при создании запроса |
| Инфраструктура | PostgreSQL, EF Core, Swagger, health checks, ProblemDetails | PostgreSQL, EF Core, Swagger, health checks, ProblemDetails |

Дополнительно:

- Clean Architecture в каждом сервисе: `Domain → Application → Infrastructure → Api`;
- CQRS через MediatR, валидация команд через FluentValidation;
- отдельная база данных у каждого сервиса, без общих таблиц и общего доменного кода;
- миграции EF Core и автоматическое применение миграций в Docker;
- доменные события агрегатов (записываются в агрегате, публикация во внешнюю шину
  в текущей итерации не выполняется);
- xUnit-тесты домена, прикладного слоя и API, включая проверку отсутствия данных
  команды в ответах для жюри;
- `docker-compose.yml` для запуска обоих сервисов и двух экземпляров PostgreSQL.

## Соответствие EventStorming

### Агрегат «Задача» (TaskService)

| Команда на диаграмме | Реализация | Событие |
|---|---|---|
| Создать задачу | `POST /api/tasks` | `TaskCreated` |
| Изменить условие задачи | `PUT /api/tasks/{id}/statement` | `TaskStatementChanged` |
| Изменить правильный ответ | `PUT /api/tasks/{id}/correct-answer` | `TaskCorrectAnswerChanged` |
| Опубликовать задачу | `POST /api/tasks/{id}/publish` | `TaskPublished` |

Инварианты:

- задача создаётся в статусе `Draft`;
- условие и правильный ответ можно менять только в `Draft`;
- публикация требует непустых условия и правильного ответа;
- опубликованная задача неизменяема;
- внутренний `GET /api/tasks/{id}/snapshot` отдаёт снимок **только опубликованной**
  задачи; для черновика возвращается `409 Conflict`.

### Агрегат «Запрос на проверку» (ReviewService)

| Команда на диаграмме | Реализация | Событие |
|---|---|---|
| Создать запрос на проверку | `POST /api/review-requests` | `ReviewRequestCreated` |
| Добавить в очередь | `POST /api/review-requests/{id}/queue` | `ReviewRequestQueued` |
| Назначить проверку | `POST /api/reviews/{id}/assign` | `ReviewAssigned` |
| Принять проверку | `POST /api/reviews/{id}/accept` | `ReviewAccepted` |
| Отказаться от проверки | `POST /api/reviews/{id}/refuse` | `ReviewRefused` |
| Передать старшему | `POST /api/reviews/{id}/transfer-to-senior` | `ReviewTransferredToSenior` |
| Завершить проверку | `POST /api/reviews/{id}/complete` | `ReviewCompleted` |

Политика «назначить следующую работу доступному члену жюри» реализована командой
`POST /api/reviews/assign-next`: сервис берёт самый ранний элемент очереди и назначает
его указанному жюри. Если очередь пуста — `204 No Content`.

### Политики доступа с диаграммы

- **Жюри проверяет решение, не зная команду.** Ответы `JuryReviewDto` и
  `ReviewQueueItemDto` не содержат `TeamId`, `SolutionId`, `SourceReferenceId`
  и рейтинг. Жюри видит условие задачи, ответ участника и правильный ответ.
- **Старший член жюри получает переданные проверки.** После отказа доступна команда
  `transfer-to-senior`; передать проверку самому себе нельзя.
- **Смотритель управляет турниром, командами, формами, результатами и дипломами.**
  В реализованном срезе смотрителю соответствуют административные ответы
  `ReviewRequestDetailsDto` (с `TeamId` и `SolutionId`) и полный API задач.
- **Участник видит результаты своей команды.** Агрегаты «Команда» и «Итоги турнира»
  вне scope; в текущем API нет публичного контура участника.

## Архитектура

```text
                         HTTP (snapshot опубликованной задачи)
  ┌────────────────┐  ───────────────────────────────────────▶  ┌────────────────┐
  │  ReviewService │                                              │  TaskService   │
  │  «Запрос на    │  ◀───────────────────────────────────────  │  «Задача»      │
  │   проверку»    │            JSON TaskSnapshotDto               │                │
  └───────┬────────┘                                              └───────┬────────┘
          │                                                               │
          ▼                                                               ▼
  ┌────────────────┐                                              ┌────────────────┐
  │  review-db     │                                              │   task-db      │
  │  PostgreSQL    │                                              │  PostgreSQL    │
  └────────────────┘                                              └────────────────┘
```

Ключевые решения:

1. **Database per service.** У сервисов разные базы (`reviews` и `tasks`), нет общих
   таблиц, EF-моделей и доменных классов.
2. **Снимок задачи (snapshot).** При создании запроса на проверку ReviewService
   синхронно запрашивает у TaskService условие и правильный ответ и сохраняет их
   в агрегате. Дальнейшие изменения задачи не влияют на уже созданную проверку.
   Черновик получить нельзя.
3. **Anti-corruption layer.** ReviewService не использует DTO TaskService напрямую:
   `TaskCatalogClient` преобразует внешний контракт в собственный `TaskSnapshot`,
   проверяет согласованность ответа и переводит ошибки в прикладные исключения.
4. **CQRS без разделения хранилищ.** Команды меняют агрегат через репозиторий и
   `IUnitOfWork`; запросы читают те же таблицы через EF Core и возвращают DTO.
5. **Тонкие контроллеры.** Контроллеры только вызывают `ISender` MediatR; бизнес-логика
   находится в домене и обработчиках команд.

### Слои

| Проект | Зависимости | Ответственность |
|---|---|---|
| `*.Domain` | нет внешних зависимостей | агрегаты, инварианты, доменные события, исключения |
| `*.Application` | Domain, MediatR, FluentValidation | команды, запросы, DTO, интерфейсы репозиториев и внешних сервисов |
| `*.Infrastructure` | Application, Domain, EF Core, Npgsql | DbContext, конфигурации EF, репозитории, HTTP-клиент TaskService |
| `*.Api` | Application, Infrastructure, Domain | контроллеры, контракты HTTP, DI, Swagger, обработка ошибок, health checks |

## Структура репозитория

```text
TournamentManagement.sln
docker-compose.yml
Directory.Build.props
global.json
src/
  TaskService/
    TaskService.Domain/          # агрегат GameTask, статусы, события
    TaskService.Application/     # команды, запросы, DTO, валидаторы
    TaskService.Infrastructure/  # EF Core, миграции, репозиторий
    TaskService.Api/             # контроллеры, Program.cs, Dockerfile
  ReviewService/
    ReviewService.Domain/        # агрегат ReviewRequest, статусы, события
    ReviewService.Application/   # команды, запросы, DTO, валидаторы
    ReviewService.Infrastructure/# EF Core, миграции, HTTP-клиент TaskService
    ReviewService.Api/           # контроллеры, Program.cs, Dockerfile
tests/
  TaskService.Domain.Tests/
  TaskService.Application.Tests/
  TaskService.Api.Tests/
  ReviewService.Domain.Tests/
  ReviewService.Application.Tests/
  ReviewService.Api.Tests/
```

## Запуск через Docker Compose

Требования: Docker Desktop (Linux containers) с Compose v2.

```bash
docker compose up --build -d
```

Будут запущены:

| Сервис | Адрес | Назначение |
|---|---|---|
| `task-service` | http://localhost:5101 | TaskService API, Swagger: `/swagger` |
| `review-service` | http://localhost:5102 | ReviewService API, Swagger: `/swagger` |
| `task-db` | `localhost:5432` | PostgreSQL для задач, БД `tasks` |
| `review-db` | `localhost:5433` | PostgreSQL для проверок, БД `reviews` |

Проверка состояния:

```bash
curl http://localhost:5101/health
curl http://localhost:5102/health
```

Оба сервиса применяют миграции при старте (`Database__ApplyMigrations=true`).
Дождитесь статуса `healthy` у контейнеров БД; сервисы запускаются после этого.

Остановка и удаление данных:

```bash
docker compose down          # остановить, сохранить данные
docker compose down -v       # остановить и удалить тома БД
```

## Локальный запуск

Требования: .NET SDK 10.0 и PostgreSQL 17 (или Docker для баз).

1. Поднимите базы:

   ```bash
   docker compose up -d task-db review-db
   ```

2. Примените миграции и запустите TaskService:

   ```powershell
   $env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=tasks;Username=postgres;Password=postgres"
   $env:Database__ApplyMigrations = "true"
   dotnet run --project src/TaskService/TaskService.Api
   ```

3. Во втором терминале запустите ReviewService:

   ```powershell
   $env:ConnectionStrings__Postgres = "Host=localhost;Port=5433;Database=reviews;Username=postgres;Password=postgres"
   $env:TaskService__BaseUrl = "http://localhost:5101"
   $env:Database__ApplyMigrations = "true"
   dotnet run --project src/ReviewService/ReviewService.Api
   ```

4. Откройте Swagger:
   - http://localhost:5101/swagger
   - http://localhost:5102/swagger

Альтернативно миграции можно применить командой:

```powershell
dotnet ef database update `
  --project src/TaskService/TaskService.Infrastructure `
  --startup-project src/TaskService/TaskService.Api

dotnet ef database update `
  --project src/ReviewService/ReviewService.Infrastructure `
  --startup-project src/ReviewService/ReviewService.Api
```

## API и сквозной сценарий

Swagger UI: `/swagger`, OpenAPI JSON: `/swagger/v1/swagger.json`.
Перечисления в JSON сериализуются строками (`Draft`, `Published`, `Correct`, `Incorrect`).

### TaskService (`http://localhost:5101`)

| Метод | Маршрут | Описание |
|---|---|---|
| `POST` | `/api/tasks` | создать задачу |
| `GET` | `/api/tasks` | список: `status`, `search`, `page`, `pageSize` |
| `GET` | `/api/tasks/{id}` | карточка задачи |
| `PUT` | `/api/tasks/{id}/statement` | изменить условие (только `Draft`) |
| `PUT` | `/api/tasks/{id}/correct-answer` | изменить правильный ответ (только `Draft`) |
| `POST` | `/api/tasks/{id}/publish` | опубликовать |
| `GET` | `/api/tasks/{id}/snapshot` | внутренний снимок для ReviewService |
| `GET` | `/health` | health check |

### ReviewService (`http://localhost:5102`)

| Метод | Маршрут | Описание |
|---|---|---|
| `POST` | `/api/review-requests` | создать запрос (снимок задачи берётся из TaskService) |
| `GET` | `/api/review-requests` | список для смотрителя: `status`, `page`, `pageSize` |
| `GET` | `/api/review-requests/{id}` | полная карточка для смотрителя |
| `GET` | `/api/review-requests/queue` | очередь без данных команды |
| `POST` | `/api/review-requests/{id}/queue` | добавить в очередь |
| `POST` | `/api/reviews/{id}/assign` | назначить конкретному жюри |
| `POST` | `/api/reviews/assign-next` | назначить следующую работу из очереди |
| `POST` | `/api/reviews/{id}/accept` | принять |
| `POST` | `/api/reviews/{id}/refuse` | отказаться |
| `POST` | `/api/reviews/{id}/transfer-to-senior` | передать старшему |
| `POST` | `/api/reviews/{id}/complete` | завершить с вердиктом |
| `GET` | `/api/reviews/assigned/{juryMemberId}` | проверки жюри без данных команды |
| `GET` | `/api/reviews/{id}/jury/{juryMemberId}` | проверка для жюри |
| `GET` | `/health` | health check |

### Пример сквозного сценария (PowerShell)

```powershell
$task = Invoke-RestMethod -Method Post `
  -Uri http://localhost:5101/api/tasks `
  -ContentType 'application/json' `
  -Body (@{
      title         = 'Сумма двух чисел'
      statement     = 'Даны два числа. Найдите их сумму.'
      correctAnswer = '42'
  } | ConvertTo-Json)

Invoke-RestMethod -Method Post -Uri "http://localhost:5101/api/tasks/$($task.id)/publish"

$review = Invoke-RestMethod -Method Post `
  -Uri http://localhost:5102/api/review-requests `
  -ContentType 'application/json' `
  -Body (@{
      solutionId       = [guid]::NewGuid()
      taskId           = $task.id
      participantAnswer = '42'
      teamId           = [guid]::NewGuid()
      source           = 'Solution'
  } | ConvertTo-Json)

Invoke-RestMethod -Method Post -Uri "http://localhost:5102/api/review-requests/$($review.id)/queue"

$juryId = [guid]::NewGuid()
$assigned = Invoke-RestMethod -Method Post `
  -Uri http://localhost:5102/api/reviews/assign-next `
  -ContentType 'application/json' `
  -Body (@{ juryMemberId = $juryId } | ConvertTo-Json)

Invoke-RestMethod -Method Post -Uri "http://localhost:5102/api/reviews/$($assigned.id)/accept" `
  -ContentType 'application/json' `
  -Body (@{ juryMemberId = $juryId } | ConvertTo-Json)

Invoke-RestMethod -Method Post -Uri "http://localhost:5102/api/reviews/$($assigned.id)/complete" `
  -ContentType 'application/json' `
  -Body (@{
      juryMemberId = $juryId
      verdict      = 'Correct'
      comment      = 'Ответ совпадает с эталоном.'
  } | ConvertTo-Json)
```

Проверка, что жюри не получает данные команды:

```powershell
$juryView = Invoke-RestMethod -Uri "http://localhost:5102/api/reviews/$($assigned.id)/jury/$juryId"
$juryView.PSObject.Properties.Name   # нет teamId, solutionId, rating
```

## Модели состояний

**Задача:**

```text
Draft ── publish ──▶ Published
  │                      │
  └── изменение условия  └── изменение запрещено
      и ответа
```

**Запрос на проверку:**

```text
Created ── queue ──▶ Queued ── assign ──▶ Assigned ── accept ──▶ Accepted
                                             │                     │
                                             ├── refuse ──▶ Refused│
                                             │                │    │
                                             │                ▼    │
                                             │       TransferredToSenior
                                             │                │
                                             └──── complete ──┴──▶ Completed

Из TransferredToSenior доступны accept, refuse и complete.
Из Accepted доступны refuse и complete.
Повторный assign для уже назначенной проверки запрещён.
```

## Ошибки

Все ошибки возвращаются в формате `ProblemDetails` (RFC 7807) с `traceId`.

| HTTP | Причина |
|---|---|
| `400 Bad Request` | ошибка валидации команды (FluentValidation), некорректный JSON |
| `404 Not Found` | агрегат не найден; задача не найдена в TaskService |
| `409 Conflict` | задача ещё не опубликована; попытка изменить опубликованную задачу |
| `422 Unprocessable Entity` | нарушен доменный инвариант или недопустимый переход состояния |
| `503 Service Unavailable` | TaskService недоступен или вернул некорректный ответ |

## Приватность данных жюри

Требование диаграммы — жюри проверяет решение, не зная, чьё оно. В коде это
обеспечено **разными DTO**, а не фильтрацией на клиенте:

- `JuryReviewDto` — `Id`, `TaskId`, `TaskStatement`, `ParticipantAnswer`,
  `CorrectAnswer`, `Status`, `Verdict`, `Comment`, временные метки. **Нет**
  `TeamId`, `SolutionId`, `SourceReferenceId`, рейтинга.
- `ReviewQueueItemDto` — `Id`, `TaskId`, `TaskStatement`, `CreatedAt`, `QueuedAt`.
  **Нет** команды, решения и рейтинга.
- `ReviewRequestDetailsDto` — административное представление, содержит `TeamId`,
  `SolutionId` и используется только API смотрителя.

Интеграционный тест `tests/ReviewService.Api.Tests/ReviewsApiTests.cs` проверяет,
что JSON ответов для жюри не содержит `teamId` и `solutionId`.

## Тесты

```bash
dotnet test TournamentManagement.sln
```

Сейчас в решении **39 тестов**. Покрыты:

- инварианты и переходы состояний `GameTask` и `ReviewRequest`;
- обработчики команд и запросов, включая `assign-next`;
- API-контракты TaskService и ReviewService через `WebApplicationFactory` + EF InMemory;
- приватность ответов жюри (отсутствие идентификаторов команды и решения);
- сквозной сценарий: публикация задачи → создание проверки → очередь → назначение →
  принятие → завершение.

## Проектные решения и ограничения

- **Синхронный HTTP вместо брокера.** Для снимка задачи использован прямой вызов
  TaskService с таймаутом и standard resilience handler. Это осознанное упрощение:
  в production-варианте уместны Outbox/Inbox и брокер сообщений, но для учебного
  среза важнее явно показать контракт и отсутствие общей БД.
- **Нет аутентификации и авторизации.** Идентификаторы жюри, смотрителя и команды
  передаются в запросе и считаются доверенными. API предназначен для внутреннего
  контура; перед публикацией нужен API gateway и проверка ролей.
- **Нет публичного API участника.** Агрегаты «Команда», «Итоги турнира» и рейтинг
  не реализованы, поэтому требование «участник видит результаты своей команды»
  вынесено за границы текущего среза.
- **Доменные события не публикуются во внешнюю шину.** Они фиксируются в агрегате
  и очищаются после сохранения; это задел для будущей интеграции.
- **Остальные агрегаты диаграммы не реализованы.** Сервисы не пытаются частично
  моделировать турнир, игру или решение: их данные приходят в ReviewService в виде
  идентификаторов и снимка задачи.
- **Пароли PostgreSQL в compose — учебные.** Для реального окружения используйте
  секреты и не публикуйте порты БД наружу.
