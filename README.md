# LightChat - мессенджер с поддержкой реального времени

[![CI](https://github.com/gevgeni/LightChat/actions/workflows/ci.yml/badge.svg)](https://github.com/gevgeni/LightChat/actions/workflows/ci.yml)
[![CD](https://github.com/gevgeni/LightChat/actions/workflows/cd.yml/badge.svg)](https://github.com/gevgeni/LightChat/actions/workflows/cd.yml)

**LightChat** — это полнофункциональный мессенджер, построенный на современном стеке .NET. Проект демонстрирует Clean Architecture, CQRS с MediatR, асинхронное взаимодействие через SignalR, event-driven доставкой уведомлений через RabbitMQ + MassTransit (с EF Outbox), Web Push и работу с PostgreSQL + Redis.

---
## Основные возможности

-  Регистрация и аутентификация пользователей (JWT)
-  Создание групповых и личных (direct) чатов
-  Отправка и получение сообщений в реальном времени через SignalR
-  История сообщений с курсорной пагинацией
-  Статус «онлайн/офлайн» пользователей (Redis)
-  Индикатор набора текста (typing indicator)
-  Отметка о прочтении сообщений (двойная галочка)
-  Уведомления о новых сообщениях и приглашениях
- Web Push уведомления (VAPID) для offline-пользователей
- Создание/удаление чатов, выход из чата
-  Минималистичный веб-клиент (vanilla JS + Tailwind, PWA)
-  Полное тестовое покрытие: unit-тесты + интеграционные тесты с Testcontainers

---
## Технологический стек

| Слой           | Технологии                                                    |
| -------------- | ------------------------------------------------------------- |
| Backend        | .NET 9, ASP.NET Core (Minimal API), MediatR, FluentValidation |
| Данные         | PostgreSQL 16, EF Core 9, Redis 7                             |
| Realtime       | SignalR (WebSockets)                                          |
| Messaging      | RabbitMQ, MassTransit, EF Core Outbox / Inbox                 |
| Аутентификация | JWT, BCrypt                                                   |
| Push           | Web Push (VAPID)                                              |
| Логирование    | Serilog                                                       |
| Тесты          | xUnit, Moq, FluentAssertions, Testcontainers, Respawn         |
| Инфраструктура | Docker, Docker Compose, GitHub Actions                        |

---
## Структура проекта
```text
LightChat/  
├── Core/ # Домен: сущности, события, CQRS-хендлеры, абстракции  
│ ├── Entities/ # User, Chat, ChatMember, Message, UserPushSubscription  
│ ├── Events/ # MessageSentEvent (публикуется в RabbitMQ)  
│ ├── Features/ # CQRS через MediatR: команды, запросы, хендлеры  
│ │ ├── Chats/  
│ │ ├── Messages/  
│ │ ├── Notifications/  
│ │ └── Users/  
│ ├── Interfaces/ # ICacheInvalidator, IPasswordHasher, IUserStatusManager,  
│ └── Repositories/ # Абстракции репозиториев  
│  
├── Infrastructure/ # Реализации  
│ ├── Consumers/ # MessageSentConsumer (Web Push через MassTransit)  
│ ├── Persistence/ # ApplicationDbContext + 7 миграций (включая Outbox)  
│ ├── Repositories/ # EF-репозитории + декоратор с Redis-кэшем  
│ ├── Security/ # BCryptPasswordHasher, JwtTokenGenerator  
│ └── Services/ # RedisCacheInvalidator, UserStatusManager  
│  
├── Web/ # Хост-проект  
│ ├── Extensions/ # AuthenticationSetup (JWT + SignalR access_token)  
│ ├── Hubs/ # ChatHub (SignalR)  
│ ├── Middlwares/ # CustomExceptionHandler, CachingBehavior  
│ ├── Requests/ # DTO для Minimal API  
│ ├── Services/ # (регистрация runtime-сервисов)  
│ └── wwwroot/ # index.html, js, css, service-worker.js  
│  
├── UnitTests/ # 40+ unit-тестов хендлеров и валидаторов  
└── IntegrationTests/ # 30+ интеграционных: API + SignalR + Testcontainers
```
---
## Ключевые паттерны
- **CQRS + MediatR** — команды (`CreateChatCommand`), запросы (`GetUserChatsQuery`), единый pipeline.
- **Pipeline Behavior** — `CachingBehavior<TRequest, TResponse>` кэширует запросы с `ICacheableQuery`.
- **Декоратор репозитория** — `CachedUserRepository` оборачивает `EfUserRepository`, кэшируя `GetByIdAsync`.
- **Transactional Outbox** — MassTransit `AddEntityFrameworkOutbox<ApplicationDbContext>` с PostgreSQL. Событие сохраняется в БД в той же транзакции, что и сообщение; фоновый publisher перекладывает его в RabbitMQ.
- **Inbox** — идемпотентная обработка consumer'ов на стороне получателя.
- **Presence через Redis** — `IUserStatusManager` хранит связь `connectionId → userId` и Set'ы `userId → [connectionId]`. Multi-tab корректен.
---

## Запуск проекта (локально)

### 1. Требования

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Локально через `dotnet run`

```bash
git clone https://github.com/gevgeni/LightChat.git
cd LightChat

# Поднимаем инфраструктуру: Postgres, Redis, RabbitMQ
docker-compose up -d

# Секреты (не коммитятся)
cd Web
dotnet user-secrets set "ConnectionStrings:PostgresConnection" "Host=localhost;Port=5432;Database=litechat_db;Username=postgres;Password=devpassword"
dotnet user-secrets set "ConnectionStrings:RedisConnection" "localhost:6379"
dotnet user-secrets set "JwtSettings:Secret" "your-super-secret-key-at-least-32-bytes-long"
dotnet user-secrets set "Vapid:PrivateKey" "<ваш-vapid-private-key>"

dotnet run
```
Открой `https://localhost:7114/index.html` (или `http://localhost:5041/index.html`).

**Миграции применяются автоматически** при старте (кроме окружения `Testing`).

### Готовый Docker-образ

Образ публикуется в GHCR при каждом push в `master`:

```bash
docker pull ghcr.io/gevgeni/lightchat:latest
docker run -p 8080:8080 \
  -e ConnectionStrings__PostgresConnection="Host=host.docker.internal;Port=5432;Database=litechat_db;Username=postgres;Password=devpassword" \
  -e ConnectionStrings__RedisConnection="host.docker.internal:6379" \
  -e RabbitMq__Host="host.docker.internal" \
  -e JwtSettings__Secret="your-super-secret-key-at-least-32-bytes-long" \
  ghcr.io/gevgeni/lightchat:latest
```

---
## API

Все защищённые эндпоинты требуют `Authorization: Bearer <JWT>`.

### Пользователи

|Метод|Путь|Описание|Auth|
|---|---|---|---|
|POST|`/users`|Регистрация|—|
|POST|`/auth/login`|Получить JWT|—|
|GET|`/users`|Все пользователи (кроме себя)|✔|

### Чаты

|Метод|Путь|Описание|Auth|
|---|---|---|---|
|POST|`/chats`|Создать групповой чат|✔|
|POST|`/chats/direct`|Создать личный чат|✔|
|GET|`/chats`|Чаты текущего пользователя|✔|
|DELETE|`/chats/{chatId}`|Удалить чат (только создатель)|✔|
|DELETE|`/chats/{chatId}/leave`|Покинуть чат|✔|
|GET|`/chats/{chatId}/members`|Участники|✔|
|POST|`/chats/{chatId}/members`|Добавить участника|✔|

### Сообщения

|Метод|Путь|Описание|Auth|
|---|---|---|---|
|GET|`/chats/{chatId}/messages?limit=20&beforeMessageId=<guid>`|История с пагинацией|✔|

### Уведомления

|Метод|Путь|Описание|Auth|
|---|---|---|---|
|POST|`/notifications/subscribe`|Подписка на Web Push|✔|
|GET|`/notifications/vapid-public-key`|Публичный VAPID-ключ|—|

### Health-checks

|Путь|Назначение|
|---|---|
|`/health/live`|Liveness (просто «жив»)|
|`/health/ready`|Readiness (Postgres, Redis, RabbitMQ)|
|`/health`|Полная проверка с UI-ответом|

---
## SignalR хаб

Подключение: `/chatHub`, авторизация — JWT через `access_token` query-параметр.

|Сторона|Метод/Событие|Описание|
|---|---|---|
|Client → Server|`JoinChat(chatId)`|Подписаться на чат|
|Client → Server|`LeaveChat(chatId)`|Отписаться|
|Client → Server|`SendMessage(chatId, text)`|Отправить сообщение|
|Client → Server|`NotifyTyping(chatId)`|«Печатает…»|
|Client → Server|`MarkChatAsRead(chatId)`|Пометить прочитанным|
|Server → Client|`ReceiveMessage`|Новое сообщение|
|Server → Client|`UserStatusChanged`|Онлайн/офлайн|
|Server → Client|`UserIsTyping`|Индикатор набора|
|Server → Client|`MessagesMarkedAsRead`|Прочитано|
|Server → Client|`ChatInvitation`|Приглашение в чат|
|Server → Client|`ChatDeleted` / `UserLeave`|Управление чатом|

---
## Тесты

**73 теста**, все проходят локально и в CI.

|Проект|Что покрывает|
|---|---|
|`UnitTests`|Хендлеры (Moq) + валидаторы (FluentValidation.TestHelper)|
|`IntegrationTests`|Endpoints (WebApplicationFactory + Testcontainers Postgres) и ChatHub (SignalR Client)|

Особенности:

- **Testcontainers** поднимает PostgreSQL в Docker — не нужны внешние сервисы.
- **Respawn** сбрасывает БД после каждого теста — полная изоляция.
- **Реальные миграции** применяются через `MigrateAsync` — тесты валидируют схему.
- **SignalR-тесты** работают с настоящим `HubConnection` через `HttpMessageHandlerFactory`.

```bash
dotnet test
```
---
## CI/CD

**CI** (`.github/workflows/ci.yml`) — на каждый push/PR:

1. Поднимает Redis и RabbitMQ через `services`.
2. Собирает solution, прогоняет unit- и интеграционные тесты.

**CD** (`.github/workflows/cd.yml`) — после успешного CI на `master`:

1. Собирает Docker-образ.
2. Публикует в GHCR с тегами `master`, `latest`, `sha-<short>`, а также semver при push тега `v*`.

---
## Конфигурация

Все секреты — через **user-secrets** локально и **environment variables** в проде (`ConnectionStrings__X`, `JwtSettings__Secret`, `Vapid__PrivateKey`).

Структура `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "PostgresConnection": "",
    "RedisConnection": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "",
    "Issuer": "LightChatBackend",
    "Audience": "LightChatClient"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Vapid": {
    "Subject": "mailto:admin@lightchat.local",
    "PublicKey": "<public key>",
    "PrivateKey": ""
  }
}
```

---
## Автор

**Турковский Евгений** — [GitHub](https://github.com/gevgeni) · Email