# NumismaticClub (ПиКПО)

Микросервисный проект «Нумизматический клуб» — лабораторные работы по дисциплине **«Проектирование и конструирование программного обеспечения»**.

## Стек

- **.NET 10** (ASP.NET Core)
- **MongoDB** — хранилище данных
- **Redis** — кэширование
- **Apache Kafka** — асинхронное подтверждение монет
- **JWT** — авторизация
- **Ocelot** — API Gateway
- **OpenTelemetry + Prometheus** — метрики
- **Swagger** — документация REST API

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Visual Studio 2022+ / JetBrains Rider / VS Code

## Быстрый старт

### 1. Клонировать репозиторий

```cmd
git clone <URL_РЕПОЗИТОРИЯ>
cd NumismaticClub
```

### 2. Поднять инфраструктуру

```cmd
docker-compose up -d
```

Поднимутся 4 контейнера: MongoDB (27017), Redis (6379), Kafka (9092), Zookeeper (2181).

### 3. Создать Kafka-топики (только при первом запуске)

```cmd
docker exec irisps-kafka kafka-topics --bootstrap-server 127.0.0.1:9092 --create --topic irisps-coin-confirmation-requests --partitions 1 --replication-factor 1

docker exec irisps-kafka kafka-topics --bootstrap-server 127.0.0.1:9092 --create --topic irisps-coin-confirmation-responses --partitions 1 --replication-factor 1
```

### 4. Запустить сервисы

В **трёх отдельных окнах** cmd:

**Окно 1 — UserConfirmationService:**
```cmd
cd UserConfirmationService
dotnet run --launch-profile http
```

**Окно 2 — NumismaticClub:**
```cmd
cd NumismaticClub
dotnet run --launch-profile http
```

**Окно 3 — ApiGateway:**
```cmd
cd ApiGateway
dotnet run --launch-profile http
```

### 5. Открыть Swagger

- http://localhost:5182/swagger  — UserConfirmationService
- http://localhost:5210/swagger — NumismaticClub

### 6. Авторизоваться

1. `POST /api/Auth/login` на `5184` с телом:
   ```json
   { "username": "admin", "password": "admin123" }
   ```
2. Скопировать `token` из ответа.
3. Нажать **Authorize** в Swagger `5184` и `5212`, вставить токен.

Пользователь `admin` создаётся автоматически при первом запуске UserConfirmationService.

## Порты

| Сервис | HTTP | HTTPS |
|---|---|---|
| UserConfirmationService | 5182 | 7140 |
| NumismaticClub | 5210 | 7230 |
| ApiGateway | 5194 | 5000 |

## Инфраструктура

| Сервис | Порт |
|---|---|
| MongoDB | 27017 |
| Redis | 6379 |
| Kafka | 9092 |
| Zookeeper | 2181 |

## Kafka-топики

- `irisps-coin-confirmation-requests`
- `irisps-coin-confirmation-responses`

## Структура решения

- `NumismaticClub` — основной микросервис (монеты, MongoDB, Kafka, Redis, JWT)
- `UserConfirmationService` — сервис пользователей (JWT, MongoDB, Kafka)
- `ApiGateway` — Ocelot API Gateway
- `NumismaticClub.Tests` — консольные нагрузочные тесты
- `NumismaticClub.JwtTests` — консольные JWT-тесты

## Полезные команды Docker

```cmd
# Запустить инфраструктуру
docker-compose up -d

# Остановить (данные сохранятся)
docker-compose stop

# Полностью удалить (данные потеряются!)
docker-compose down -v

# Перезапустить один контейнер
docker-compose restart kafka

# Логи контейнера
docker-compose logs kafka --tail 50
```

## Git-flow

- Ветка `main` защищена, прямые пуши запрещены.
- Работаем через feature-ветки: `feature/название-задачи`.
- Изменения — через Pull Request.