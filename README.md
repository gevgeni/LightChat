# LightChat – мессенджер с поддержкой реального времени

**LightChat** — это полнофункциональный мессенджер, построенный на современном стеке .NET. Проект демонстрирует Clean Architecture, CQRS с MediatR, асинхронное взаимодействие через SignalR и работу с PostgreSQL + Redis.

---
## Основные возможности

- ✅ Регистрация и аутентификация пользователей (JWT)
- ✅ Создание групповых и личных (direct) чатов
- ✅ Отправка и получение сообщений в реальном времени через SignalR
- ✅ История сообщений с пагинацией
- ✅ Статус «онлайн/офлайн» пользователей (Redis)
- ✅ Индикатор набора текста (typing indicator)
- ✅ Отметка о прочтении сообщений (двойная галочка)
- ✅ Приглашение участников в групповые чаты
- ✅ Уведомления о новых сообщениях и приглашениях
- ✅ Минималистичный веб-клиент (HTML + Tailwind + SignalR)
- ✅ **Полное тестовое покрытие**: unit-тесты + интеграционные тесты с Testcontainers

---
## Технологический стек

### Backend

- **.NET 9**
- **[ASP.NET](https://ASP.NET) Core** (Minimal API, WebAPI)
- **Entity Framework Core** (PostgreSQL)
- **MediatR** (CQRS)
- **SignalR** (WebSockets)
- **JWT** (аутентификация)
- **FluentValidation** (валидация)
- **Serilog** (логирование)
- **xUnit + Moq + FluentAssertions** (юнит-тесты)

### Инфраструктура

- **PostgreSQL 16** – основная БД
- **Redis 7** – хранение статусов пользователей и активных соединений
- **Docker / Docker Compose** – контейнеризация окружения
- **BCrypt** – хеширование паролей

### Клиент (Web)

- HTML / CSS (Tailwind)
- SignalR JavaScript Client
- Адаптивный интерфейс

---
## Структура проекта
```text
LightChat/
├── Core/                     # Доменная логика, сущности, интерфейсы
│   ├── Entities/             # Chat, User, Message, ChatMember
│   ├── Features/             # CQRS: команды, запросы, хендлеры
│   │   ├── Chats/            # Создание, добавление участников, получение чатов
│   │   ├── Messages/         # История сообщений
│   │   └── Users/            # Регистрация, аутентификация, список пользователей
│   ├── Interfaces/           # Абстракции (IUserRepository, IPasswordHasher, ...)
│   └── Repositories/         # Интерфейсы репозиториев
│
├── Infrastructure/           # Реализации репозиториев, контекст БД, миграции
│   ├── Persistence/          # ApplicationDbContext + миграции
│   ├── Repositories/         # EfChatRepository, EfMessageRepository, EfUserRepository
│   ├── Security/             # BCryptPasswordHasher, JwtTokenGenerator
│   └── Services/             # UserStatusManager (Redis)
│
├── Web/                      # Хост-проект (стартовый)
│   ├── Hubs/                 # ChatHub (SignalR)
│   ├── Middlewares/          # CustomExceptionHandler (различает типы ошибок)
│   ├── Requests/             # DTO для Minimal API
│   ├── Extensions/           # AuthenticationSetup
│   ├── wwwroot/              # index.html (клиент)
│   └── Program.cs            # Входная точка, конфигурация DI, эндпоинты
│
├── UnitTests/                # Юнит-тесты для хендлеров и валидаторов
│   ├── Handlers/
│   │   ├── Chats/
│   │   ├── Messages/
│   │   └── Users/
│   └── Validators/
│
├── IntegrationTests/         # Интеграционные тесты (Testcontainers + SignalR)
│   ├── Endpoints/            # API-тесты (Auth, Chats, Messages, Users)
│   └── Hubs/                 # SignalR-тесты (Join, Send, Typing, Read)
│
└── docker-compose.yml        # Поднятие PostgreSQL и Redis
```
---
## Запуск проекта (локально)

### 1. Требования

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (или отдельные PostgreSQL и Redis)

### 2. Клонирование репозитория

```bash
git clone https://github.com/gevgeni/LightChat.git
cd LightChat
```
### 3. Запуск инфраструктуры (PostgreSQL + Redis)

```bash
docker-compose up -d
```
Это поднимет контейнеры с PostgreSQL (порт 5432) и Redis (порт 6379).

### 4. Применение миграций

Миграции применяются **автоматически** при запуске приложения (код в `Program.cs`). Если хотите выполнить вручную:

```bash
dotnet ef database update --project Infrastructure --startup-project Web
```

### 5. Запуск веб-приложения

```bash
cd Web
dotnet run
```
Приложение будет доступно по адресам:

- HTTP: `http://localhost:5041`
- HTTPS: `https://localhost:7114`

### 6. Открыть клиент

Перейдите в браузере по адресу `https://localhost:7114/index.html` (или `http://localhost:5041/index.html`).

---
## Настройка JWT

В `appsettings.json` уже есть тестовый ключ. Для продакшена **обязательно смените** `Secret` и настройте `Issuer`/`Audience`.

```json

"JwtSettings": {
  "Secret": "ваш-супер-секретный-ключ-не-менее-32-символов",
  "Issuer": "LightChatBackend",
  "Audience": "LightChatClient"
}
```
---

## API Эндпоинты (Minimal API)

|Метод|Путь|Описание|Авторизация|
|---|---|---|---|
|POST|`/api/users`|Регистрация пользователя|Нет|
|POST|`/auth/login`|Получение JWT-токена|Нет|
|GET|`/chats`|Получить все чаты пользователя|Да|
|POST|`/chats`|Создать групповой чат|Да|
|POST|`/chats/direct`|Создать личный чат|Да|
|GET|`/chats/{chatId}/members`|Получить участников чата|Да|
|POST|`/chats/{chatId}/members`|Добавить участника в чат|Да|
|GET|`/chats/{chatId}/messages`|История сообщений (с пагинацией)|Да|
|GET|`/users`|Список всех пользователей (кроме себя)|Да|

Все защищённые эндпоинты требуют передачи токена в заголовке:

```text
Authorization: Bearer <ваш_токен>
```
---
## Тестирование

Юнит-тесты находятся в проекте `UnitTests` и покрывают:

- Все хендлеры (регистрация, авторизация, получение чатов, сообщений, добавление участников)
- Валидаторы (FluentValidation)

Запуск тестов:

```bash
dotnet test
```
---
## Веб-клиент

Клиент (`wwwroot/index.html`) написан на чистом JavaScript с использованием SignalR. Он поддерживает:

- Авторизацию и регистрацию
- Отображение списка чатов
- Открытие чата и загрузку истории сообщений (с пагинацией при скролле вверх)
- Отправку сообщений
- Индикатор набора текста
- Статус «онлайн»
- Отметку о прочтении (двойная галочка)
- Приглашение пользователей в групповые чаты
- Создание личных чатов

---
## Планы по развитию (Roadmap)

- Добавить кэширование списка участников чата в Redis
- Поддержка отправки файлов (изображения, документы)
- Добавить Swagger/OpenAPI документацию
- Реализовать API для выхода из чата и удаления сообщений

---
## Связь с автором
- GitHub: https://github.com/gevgeni
- Email: evge1599@gmail.com