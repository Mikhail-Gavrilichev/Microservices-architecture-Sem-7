# Вердикт — микросервис проверки работ

Учебный проект: **один** ASP.NET Core 8 Web API-проект, Entity Framework Core + **собственная PostgreSQL**, статический frontend на HTML/CSS/JavaScript, Nginx и Docker Compose. Нет общей базы с микросервисом подготовки форм, брокера и авторизации.

## Структура

```text
checking-service/
├── docker-compose.yml              # PostgreSQL, backend, frontend и два volume
├── .dockerignore
├── CheckingService/
│   ├── CheckingService.csproj       # EF Core, Npgsql, Swagger, SkiaSharp
│   ├── Program.cs                   # DI, Swagger, ошибки, миграции, health
│   ├── appsettings.json
│   ├── Dockerfile
│   ├── Models/
│   │   ├── Entities.cs              # 4 сущности и enum
│   │   └── Requests.cs              # минимальные входные DTO
│   ├── Data/
│   │   ├── AppDbContext.cs          # связи и индексы
│   │   └── Migrations/              # начальная миграция и snapshot
│   ├── Services/CheckingWorkflow.cs # бизнес-процесс и каталог демонстрационных форм
│   └── Controllers/                # Submissions, Checks, Judges, Results
└── frontend/
    ├── index.html
    ├── styles.css
    ├── app.js
    ├── nginx.conf
    └── Dockerfile
```

## Запуск

Из папки `checking-service`:

```bash
docker compose up --build
```

* Frontend: **http://localhost:8091**
* Swagger: **http://localhost:8091/swagger** (или http://localhost:8090/swagger)
* Health: **http://localhost:8090/api/health**

Backend подключается к PostgreSQL по `Host=db`. База `checking_db` и фотографии `/app/photos` лежат в **двух именованных Docker volume**, отдельно от других проектов. Compose ожидает healthcheck PostgreSQL. В учебном запуске работает один backend: миграция применяется до старта HTTP-сервера. `docker compose down` сохраняет данные; `docker compose down -v` удаляет оба volume. Пароль по умолчанию учебный, задаётся переменной `CHECKING_DB_PASSWORD` **перед первым запуском**; в рабочей среде нужен секрет и настоящая авторизация.

## Как пройти сценарий в интерфейсе

1. **Участник:** выберите демонстрационную форму, укажите `TeamId` и ответ. В каталоге с сервера есть игра `1`, формы `101` (задача `1`), `102` (задача `2`), `103` (задача `3`).
2. Загрузите **настоящее изображение JPEG/PNG** и подтвердите номер формы на фото (`PhotoFormId`). Затем отметьте «Фото читаемо» и «Ответ виден» и подтвердите проверку. Если подтверждение отрицательное, решение получает `PhotoInvalid`; используйте «Отправить повторно», исправьте ответ и загрузите новый снимок.
3. **Жюри:** добавьте обычного члена жюри. Новая работа назначится свободному автоматически. Если он уже был создан, назначение произойдёт при проверке фото. Назначенная работа показывает условие, ответ участника, правильный ответ и снимок **без TeamId**. Нажмите «Отказаться».
4. **Старшее жюри:** добавьте старшего члена жюри. Работа с `Escalated` назначится ему автоматически; если он занят, она остаётся в очереди старшего до освобождения. Выберите старшего, примите работу, укажите верность, неотрицательные баллы и комментарий, завершите проверку.
5. **Результаты:** появится сохранённая запись. Повторное завершение невозможно. Результат содержит `SubmissionId`, по которому отдельный сервис подсчёта сможет связать его с командой.

### Важное упрощение проверки формы

У сервиса нет доступа к базе подготовки форм и нет OCR. Поэтому доверенный **серверный** демонстрационный каталог (`TaskCatalog` в `Services/CheckingWorkflow.cs`) задаёт допустимые `(GameId, FormId, TaskId)` и правильный ответ. При загрузке сервер **самостоятельно** сравнивает введённый `PhotoFormId` с `FormId` решения, проверяет наличие файла, расширение **и реально декодирует** JPEG/PNG (SkiaSharp). Подтверждение читаемости и видимости ответа — ручное. Эта проверка не доказывает визуально напечатанный номер формы: без OCR/внешнего доверенного сервиса это невозможно. Для реальной интеграции замените каталог на защищённый API сервиса форм, а подтверждение доверяйте смотрителю, не самому участнику.

## API

Статусы: решения — `Created`, `PhotoAttached`, `PhotoInvalid`, `AcceptedForChecking`, `Completed`; проверки — `Queued`, `Assigned`, `Accepted`, `Declined` (переходное состояние), `Escalated`, `Completed`. Роли — `Judge`, `SeniorJudge`. Ошибки бизнес-логики — JSON `{ "error": "..." }`, HTTP 400/404/409; ошибки обязательных полей — стандартный ответ ASP.NET Core 400.

| Метод | Путь | Что делает |
|---|---|---|
| GET | `/api/catalog/forms` | Публичный демо-каталог **без** правильных ответов |
| POST/GET | `/api/submissions` | Отправить / список решений |
| GET | `/api/submissions/{id}` | Решение и статус (без правильного ответа) |
| POST | `/api/submissions/{id}/photo` | `multipart/form-data`: `file`, `photoFormId` |
| POST | `/api/submissions/{id}/validate-photo` | `{ "isPhotoReadable": true, "isAnswerVisible": true }`; создаёт запрос проверки и назначает работу |
| POST | `/api/submissions/{id}/resubmit` | `{ "answer": "Исправленный ответ" }` после `PhotoInvalid` |
| POST | `/api/checks` | `{ "submissionId": 1 }`; идемпотентно возвращает уже созданную при подтверждении проверку |
| GET | `/api/checks/queue`, `/api/checks/{id}` | Очередь / работа жюри (без TeamId) |
| GET | `/api/checks/{id}/photo` | Снимок для жюри |
| POST | `/api/checks/{id}/assign` | `{ "judgeId": 1 }` для ожидающей работы |
| POST | `/api/checks/{id}/accept`, `/decline` | `{ "judgeId": 1 }`; отказ автоматически передаёт старшему |
| POST | `/api/checks/{id}/escalate?seniorId=2` | Повторно назначить ожидающую эскалацию старшему; параметр необязателен |
| POST | `/api/checks/{id}/complete` | `{ "judgeId": 2, "isCorrect": true, "pointsAwarded": 1, "comment": "Верно" }` |
| POST/GET | `/api/judges` | Создать / список членов жюри |
| GET | `/api/judges/{id}/checks` | Работы жюри без TeamId |
| GET | `/api/results`, `/api/results/{id}` | Неизменяемые результаты |

Пример создания жюри: `POST /api/judges` с телом `{ "name": "Анна", "role": "Judge", "isAvailable": true }`. Один член жюри имеет не больше **одной активной проверки** — правило проверяется сервисом и частичным уникальным индексом PostgreSQL. При одновременных запросах конфликт возвращает 409.

Без Docker можно задать `ConnectionStrings__Default` и выполнить `dotnet run --project CheckingService`. Для следующих миграций: `dotnet ef migrations add Название --project CheckingService --output-dir Data/Migrations` (нужен dotnet-ef версии 8).
