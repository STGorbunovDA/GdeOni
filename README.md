# GdeOni

Сервис для каталогизации мест захоронений: пользователь у могилы создаёт
карточку умершего с GPS-координатами, может загружать фото / документы /
воспоминания и подписываться на отслеживание (напоминания о годовщинах).

## Структура

- [backend/](backend/) — .NET 8, Clean Architecture (Domain / Application
  / Infrastructure / API). Полный план и история правок — в
  [backend/docs/PlanFull.txt](backend/docs/PlanFull.txt).
- [frontend/mobile/](frontend/mobile/) — .NET MAUI Android-приложение
  (net10.0-android). На момент текущего коммита — каркас, фичевая
  работа описана в плане как E1-E25.
- [frontend/web/](frontend/web/) — web frontend (F-блок плана). Ещё не
  начат.
- [.github/workflows/](.github/workflows/) — GitHub Actions CI/CD
  (backend-ci, backend-release, mobile-ci, mobile-release).

## Quick start (backend)

Все команды — из `backend/`. Требуется .NET 8 SDK + Docker.

```bash
cd backend

# 1. Создать локальный appsettings.json (gitignored)
cp src/GdeOni.API/appsettings.example.json src/GdeOni.API/appsettings.json
# Открыть и заполнить placeholder'ы: ConnectionStrings, Seed.SuperAdmin,
# Jwt.SecretKey (≥32 байт), Minio (если меняли пароли в docker-compose).

# 2. Поднять PostgreSQL (5434), MinIO (9001), Seq (8081)
docker compose up -d

# 3. Применить миграции
dotnet ef database update \
    --project src/GdeOni.Infrastructure \
    --startup-project src/GdeOni.API

# 4. Запустить API
dotnet run --project src/GdeOni.API/GdeOni.API.csproj

# Swagger: http://localhost:5226/swagger
# Seq logs: http://localhost:8081
```

## Тесты

```bash
cd backend
dotnet test backend.sln
```

449 тестов на момент коммита:
- Domain: 146 (агрегаты, value objects, доменные методы)
- Application: 153 (use cases с моками)
- Infrastructure: 44 (repository + Testcontainers PostgreSQL)
- Integration: 106 (`WebApplicationFactory<Program>` + Testcontainers
  PostgreSQL + MinIO)

Infrastructure и Integration требуют Docker.

## Configuration

Полный шаблон конфигурации с пояснениями к каждой секции —
[backend/src/GdeOni.API/appsettings.example.json](backend/src/GdeOni.API/appsettings.example.json).
Файл `appsettings.json` и `appsettings.Development.json` в `.gitignore` —
не коммитьте секреты.

Главные секции и их назначение:

| Секция | Что задаёт |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection. |
| `Seed:SuperAdmin` | Первый SuperAdmin, создаётся идемпотентно при старте. |
| `Jwt` | JWT issuer/audience/secret (≥32 байт, ≥16 уникальных символов). |
| `BCrypt:WorkFactor` | Стоимость хеширования паролей. |
| `Minio` | Endpoint, ключи, PublicBaseUrl для presigned-URL. |
| `AppVersion` | D17: версии клиента для `/api/app/version`. |
| `FeatureFlags` | D17: SubscriptionEnabled / GracePeriodDaysAfterExpiry. |
| `Subscription` | D16: цены, длительность, ReturnUrl. |
| `YooKassa` | D16: ShopId / SecretKey платёжного провайдера. |
| `Legal` | D19: версии Privacy / Terms (бамп при обновлении документов). |
| `Sentry` | D21: DSN для crash reporting (no-op без DSN). |
| `Cors:AllowedOrigins` | Whitelist origins для web/mobile. В Production — обязательна. |
| `Hosting` | KnownProxies / KnownNetworks за reverse-proxy. |
| `RateLimiting:Auth` | Лимит на /auth/* эндпоинты. |
| `RefreshTokensCleanup` | Background-job очистки старых refresh-токенов. |

## Production deploy

См. [backend/docs/deploy.md](backend/docs/deploy.md) — переменные
окружения, presigned URL, MinIO nginx, docker-compose.

CI/CD:
- `backend-v*` тег → Docker image в GHCR (`ghcr.io/<owner>/gdeoni-api`).
- `mobile-v*` тег → подписанный APK как GitHub Release (нужны
  4 секрета: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`,
  `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`).

## Архитектурные соглашения

См. [CLAUDE.md](CLAUDE.md) — Clean Architecture, Result-обвязка,
use case structure (Model / UseCase / Validation), Errors-конвенции,
snake_case в БД, no-op guards для доменных мутаций.

## Лицензия / контакты

Privacy Policy и Terms of Use — [backend/docs/legal/](backend/docs/legal/).
На момент текущего коммита — шаблоны под юр-вычитку перед публичным
релизом.
