# Листок — подготовка игровых задач и форм

Простой демонстрационный микросервис: **один ASP.NET Core 8 Web API-проект**, EF Core + PostgreSQL, статический frontend на HTML/CSS/JavaScript. Сервиса команд нет: при генерации комплектов передаётся полный список `TeamId`.

## Структура

```text
.
├── docker-compose.yml                 # backend, frontend, PostgreSQL + volume
├── GameFormsService/
│   ├── GameFormsService.csproj
│   ├── Program.cs                     # DI, Swagger, ошибки, миграции, health
│   ├── appsettings.json
│   ├── Dockerfile
│   ├── Models/
│   │   ├── Entities.cs                # Game, GameTask, FormSet, Form + enum
│   │   └── Requests.cs                 # минимальные DTO с валидацией
│   ├── Data/
│   │   ├── AppDbContext.cs             # связи и уникальные индексы
│   │   └── Migrations/                 # начальная миграция и snapshot
│   ├── Services/WorkflowService.cs     # проверки и переходы статусов
│   └── Controllers/                    # Games, Tasks, FormSets, Forms
└── frontend/
    ├── index.html
    ├── styles.css
    ├── app.js
    ├── nginx.conf                     # /api и /swagger → backend
    └── Dockerfile
```

## Запуск

Нужны Docker и Docker Compose. Из корня проекта:

```bash
docker compose up --build
```

* **Frontend:** http://localhost:8081
* **Swagger:** http://localhost:8081/swagger (также http://localhost:8080/swagger)
* **Проверка backend:** http://localhost:8080/api/health

При первом запуске backend автоматически применяет включённую EF Core-миграцию. Compose ожидает успешного healthcheck PostgreSQL до старта backend; в этом демо запускается **один** экземпляр backend. PostgreSQL хранит данные в именованном volume `postgres_data` между перезапусками. Чтобы удалить данные: `docker compose down -v`. Чтобы остановить без удаления данных: `docker compose down`.

Пароль локальной БД по умолчанию `gameforms_dev_password`; при необходимости задайте переменную окружения `POSTGRES_PASSWORD` **до первого запуска**. При уже созданном volume пароль изменением переменной не обновится. Порт PostgreSQL наружу не опубликован; backend подключается по `Host=db`, а не через localhost. Для боевой среды настройте секреты и сетевые ограничения отдельно.

## Проверка основного процесса

1. В разделе **Игры** создайте игру, задайте правила игры и правила формы, опубликуйте её.
2. В разделе **Задачи** создайте задачу для игры, заполните условие и правильный ответ, опубликуйте. Черновик можно менять; для изменения опубликованной задачи сначала снимите публикацию. Если задача уже назначена форме, снятие публикации запрещено.
3. В разделе **Комплекты форм** укажите опубликованную игру и список TeamId (например, `team-01`, `team-02`). Для каждого ID создаётся **ровно один** набор. Список фиксируется одной генерацией; число наборов равно числу переданных команд.
4. В каждом наборе выберите опубликованную задачу и добавьте форму. Одна форма содержит одну задачу; в наборе одна задача назначается не более одного раза.
5. Подготовьте формы по отдельности или весь комплект кнопкой **Подготовить комплект**. Напечатайте формы по отдельности или весь комплект кнопкой **Напечатать комплект**. Кнопка **Лист для печати** открывает браузерный печатный вид без правильных ответов.
6. Нажмите **Выдать команде** для каждого напечатанного комплекта. Повторная выдача запрещена.

## REST API

Все запросы с телом используют `Content-Type: application/json`. Статусы в JSON — строки (`Draft`, `Published`, `Created`, `Prepared`, `Printed`, `Issued`). При ошибках бизнес-логики возвращается `{ "error": "..." }` и код `400`, `404` или `409`; ошибки валидации ASP.NET Core возвращает в стандартном формате `ValidationProblemDetails` с кодом `400`.

| Метод | Путь | Назначение |
|---|---|---|
| POST / GET | `/api/games` | Создание / список игр |
| GET | `/api/games/{id}` | Игра |
| PUT | `/api/games/{id}/rules` | `{ "value": "..." }` |
| PUT | `/api/games/{id}/form-rules` | `{ "value": "..." }` |
| POST | `/api/games/{id}/publish` | Публикация игры |
| POST / GET | `/api/tasks` | Создание / список задач (`?gameId=1`) |
| GET / PUT | `/api/tasks/{id}` | Задача / изменение названия, условия, баллов |
| PUT | `/api/tasks/{id}/answer` | `{ "value": "..." }` |
| POST | `/api/tasks/{id}/publish` | Публикация задачи |
| POST | `/api/tasks/{id}/unpublish` | Снятие публикации, пока нет назначений |
| POST | `/api/formsets/generate` | `{ "gameId": 1, "teamIds": ["team-01", "team-02"] }` |
| GET | `/api/formsets`, `/api/formsets/{id}` | Список / набор с формами |
| POST | `/api/formsets/{id}/prepare`, `/print`, `/issue` | Переходы статуса набора |
| POST | `/api/forms` | `{ "formSetId": 1, "taskId": 1 }` |
| GET | `/api/forms`, `/api/forms/{id}` | Список / форма (`?formSetId=1`) |
| POST | `/api/forms/{id}/prepare`, `/print` | Переходы статуса формы |

### Пример через curl

```bash
curl -X POST http://localhost:8080/api/games -H 'Content-Type: application/json' -d '{"name":"Весенний квиз","description":"Игра для двух команд"}'
curl -X PUT http://localhost:8080/api/games/1/rules -H 'Content-Type: application/json' -d '{"value":"На ответ 2 минуты"}'
curl -X PUT http://localhost:8080/api/games/1/form-rules -H 'Content-Type: application/json' -d '{"value":"Пишите разборчиво"}'
curl -X POST http://localhost:8080/api/games/1/publish
curl -X POST http://localhost:8080/api/tasks -H 'Content-Type: application/json' -d '{"gameId":1,"title":"Столица Франции","condition":"Назовите столицу Франции","correctAnswer":"Париж","points":1}'
curl -X POST http://localhost:8080/api/tasks/1/publish
curl -X POST http://localhost:8080/api/formsets/generate -H 'Content-Type: application/json' -d '{"gameId":1,"teamIds":["team-01","team-02"]}'
curl -X POST http://localhost:8080/api/forms -H 'Content-Type: application/json' -d '{"formSetId":1,"taskId":1}'
curl -X POST http://localhost:8080/api/formsets/1/prepare
curl -X POST http://localhost:8080/api/formsets/1/print
curl -X POST http://localhost:8080/api/formsets/1/issue
```

## Разработка без Docker

Установите .NET SDK 8 и PostgreSQL; задайте `ConnectionStrings__Default` в окружении, затем выполните `dotnet run --project GameFormsService`. Для создания последующих миграций установите `dotnet-ef` версии 8 и выполните `dotnet ef migrations add Имя --project GameFormsService --output-dir Data/Migrations`.
