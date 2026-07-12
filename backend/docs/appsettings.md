# Configuration: appsettings.json

Полное описание всех секций в [appsettings.example.json](../src/GdeOni.API/appsettings.example.json).

## Как пользоваться

1. Скопировать template в локальный конфиг (он gitignored):
   ```bash
   cp backend/src/GdeOni.API/appsettings.example.json \
      backend/src/GdeOni.API/appsettings.json
   ```
2. Заменить значения `CHANGE_ME` на свои.
3. Для local-dev большинство секций оставить как есть — у всех безопасные дефолты в коде.
4. Для production КРИТИЧНО заменить:
   - `Jwt.SecretKey`
   - `Seed.SuperAdmin.Password`
   - `Minio` креды
   - `Cors.AllowedOrigins`
   - `Sentry.Dsn` (если используется Sentry)
   - `YooKassa.SecretKey` (если используется YooKassa)

**Важно:** реальные секреты НИКОГДА не попадают в `appsettings.example.json` — он коммитится в git. Все секреты — только в локальном `appsettings.json` (gitignored) или в env vars / secrets manager.

---

## ConnectionStrings

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5434;Database=gde_oni;Username=postgres;Password=CHANGE_ME;"
}
```

Строка подключения к PostgreSQL. Порт `5434` — маппинг из `docker-compose.yml` (контейнер слушает 5432 внутри, наружу проброшен 5434), чтобы не конфликтовать с локально установленным Postgres.

- **Для dev:** оставить как есть, поднять контейнер `docker compose up -d`.
- **Для prod:** указать боевой хост, сильный пароль, при необходимости `SSL=Require`.

---

## Seed

```json
"Seed": {
  "SuperAdmin": { "Email": "...", "Password": "...", "FullName": "...", "UserName": "superadmin" }
}
```

При первом запуске на пустой БД создаётся **один** пользователь с ролью SuperAdmin. Идемпотентно: при повторном запуске не дублируется.

Это **единственный** способ получить SuperAdmin — публичный `POST /api/users` (Register) отвергает попытки создать админа.

Пароль хранится в БД как BCrypt-hash. Plain-text нужен только при первом старте — потом этот файл можно "забыть". Для смены пароля админа после первого старта — `POST /api/auth/login` → `PUT /api/users/{id}/password`.

---

## Jwt

```json
"Jwt": {
  "Issuer": "GdeOni",
  "Audience": "GdeOniClient",
  "SecretKey": "CHANGE_ME_GENERATE_WITH_openssl_rand_base64_32",
  "AccessTokenLifetimeMinutes": 15,
  "RefreshTokenLifetimeDays": 14,
  "SecurityStampCacheTtlSeconds": 30
}
```

- **`Issuer` / `Audience`** — стандартные JWT-поля, проверяются при валидации. Изменение приведёт к тому, что ранее выпущенные токены перестанут работать.
- **`SecretKey`** — критический секрет для подписи токенов (HMAC-SHA256):
  - ≥ 32 байт (256 бит) — есть fail-fast проверка на старте.
  - ≥ 16 уникальных символов (защита от мусора типа `aaa...`).
  - Сгенерировать: `openssl rand -base64 32`.
  - В проде хранить в env var `Jwt__SecretKey` или в secrets manager.
- **`AccessTokenLifetimeMinutes`** — TTL access-токена. 15 минут — стандарт: достаточно мало, чтобы скомпрометированный токен быстро устарел; достаточно много, чтобы клиент не дёргал `/refresh` каждую секунду.
- **`RefreshTokenLifetimeDays`** — TTL refresh-токена. 14 дней — типовое. При каждом `/auth/refresh` старый refresh ротируется (replay-detection D7.32): если кто-то его украл и использовал, replay будет пойман и все активные сессии юзера отозваны.
- **`SecurityStampCacheTtlSeconds`** — TTL кеша SecurityStamp. При смене email/пароля/роли `User.SecurityStamp` инкрементируется — старые токены становятся невалидны. Без кеша `SELECT security_stamp` идёт на КАЖДОМ запросе. 30 сек — компромисс: ChangeEmail/ChangePassword/ChangeRole сбрасывают кеш сразу через `ISecurityStampInvalidator` (окно = 0). TTL актуален только для прямых SQL-мутаций минуя use case + для multi-instance деплоя (кеш локальный, не распределённый).

---

## BCrypt

```json
"BCrypt": { "WorkFactor": 11 }
```

Стоимость хеширования паролей. Каждый +1 удваивает время:

| WorkFactor | Время | Когда |
|---|---|---|
| 10 | ~100ms | Дефолт BCrypt.Net. Быстро для тестов. |
| 11 | ~200ms | Минимум для прода. |
| 12 | ~400ms | **Рекомендация для прода.** Баланс безопасности и UX. |
| 13 | ~800ms | Только для high-security систем. |

Меньше = быстрее login, но дешевле brute-force на украденные хеши. Больше = безопаснее, но юзер ждёт лишние секунды на каждом логине.

---

## Minio

```json
"Minio": {
  "Endpoint": "localhost:9000",
  "AccessKey": "minioadmin",
  "SecretKey": "CHANGE_ME_FOR_PRODUCTION",
  "UseSsl": false,
  "PublicBaseUrl": "http://localhost:9000"
}
```

- **`Endpoint`** — внутренний адрес MinIO для бэка (Upload/Delete/HEAD). В docker-compose контейнер называется `minio:9000`. На локальном хосте через docker port mapping — `localhost:9000`.
- **`AccessKey` / `SecretKey`** — учётные данные. `minioadmin/minioadmin` — dev-дефолт из docker-compose. **Для прода обязательно поменять** через `.env` (`MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD`).
- **`UseSsl`** — для контейнерной сети `false` (TLS терминирует nginx снаружи).
- **`PublicBaseUrl`** — отдельный URL для presigned-ссылок на **документы**. Бэк генерирует presigned URL, клиент скачивает напрямую из MinIO. Этот URL должен быть доступен **извне** (с мобильника, из браузера), а не только из контейнерной сети.

Примеры `PublicBaseUrl`:
- На localhost dev: `http://localhost:9000`
- На Android-эмуляторе: `http://10.0.2.2:9000` (special-IP для адресации хоста с эмулятора)
- На реальном устройстве в WiFi: `http://192.168.x.x:9000` (IP компа)
- В проде: `https://files.gdeoni.ru` + nginx с `MINIO_SERVER_URL`

---

## AppVersion (D17)

```json
"AppVersion": {
  "MinSupportedVersion": "1.0.0",
  "LatestVersion": "1.0.0",
  "DownloadUrl": "https://gdeoni.ru/download",
  "ForceUpdateMessage": null
}
```

Бэк отдаёт значения через `GET /api/app/version` (`AllowAnonymous`). Клиент при старте сравнивает свою версию:

| Условие | Поведение |
|---|---|
| `currentVersion < MinSupportedVersion` | `BlockingUpdatePage` — обязательное обновление, без "пропустить". |
| `MinSupportedVersion ≤ current < LatestVersion` | Soft-баннер "доступна новая версия". |
| `current ≥ LatestVersion` | Ничего не показывать. |

- **`DownloadUrl`** — куда вести юзера за APK. Раз не публикуемся в Play Store, это страница на gdeoni.ru с инструкцией "разрешите unknown sources".
- **`ForceUpdateMessage`** — опциональный текст для `BlockingUpdatePage`. `null` = клиент использует свой дефолт. Кейс: `"Критическое обновление безопасности — установите для продолжения"`.

**Практика:**
- При каждом релизе бампать `LatestVersion`.
- `MinSupportedVersion` поднимать **только** при breaking-change бэка (поменялся контракт `/auth/login`, удалён эндпоинт).

---

## FeatureFlags (D17)

```json
"FeatureFlags": {
  "SubscriptionEnabled": false,
  "GracePeriodDaysAfterExpiry": 0
}
```

- **`SubscriptionEnabled`** — главный switch монетизации:
  - `false` → open-beta: все юзеры имеют доступ ко всему без подписки. Удобно для альфы.
  - `true` → требуется `HasActiveSubscription`, иначе 403 + `subscription.required`. Whitelist (доступ без подписки): `/me`, `/me/subscription`, `/auth/*`, `/api/legal/*`, `/api/app/*`, `/api/payments/yookassa/webhook`. SuperAdmin/Admin освобождены от подписки.

- **`GracePeriodDaysAfterExpiry`** — буфер после `ExpiresAtUtc` на случай:
  - задержки YooKassa webhook при автосписании;
  - временной недоступности платёжной системы;
  - юзер забыл оплатить — даём пару дней пройти без блокировки.

  Значения: `0` = жёстко по дате. `1-3` = рекомендация для прода.

Hot-reload: меняешь значение в JSON → `IOptionsMonitor` подхватит без рестарта приложения.

---

## Subscription (D16 + D23)

```json
"Subscription": {
  "MonthlyPriceRub": 49,
  "MonthlyDurationDays": 30,
  "TrialDurationDays": 30,
  "ProductDescription": "Подписка «Где Они» — 1 месяц",
  "MobileReturnUrl": "gdeoni://payment/return",
  "WebReturnUrl": "http://localhost:5173/payment/return",
  "PendingPaymentReuseMinutes": 10
}
```

- **`MonthlyPriceRub`** — цена месячной подписки в рублях. Решение 2026-05-14: 49 ₽. Сознательно низкая, чтобы убрать per-feature gating.
- **`MonthlyDurationDays`** — длительность одного платежа. 30 дней ≈ календарный месяц.
- **`TrialDurationDays`** — пробный период при регистрации (бесплатно). 30 дней всем новым юзерам. Записывается автоматически в `RegisterUserUseCase` через `User.StartTrial`.
- **`ProductDescription`** — описание товара в платёжном чеке. **Обязательно** для 54-ФЗ (Закон о ККТ): юзер должен видеть в чеке что именно он купил.
- **`MobileReturnUrl`** — куда YooKassa вернёт мобильного юзера после оплаты. Deep-link `gdeoni://payment/return` (E22.7): MAUI перехватывает и открывает `SubscriptionPage` с активным поллингом.
- **`WebReturnUrl`** — куда YooKassa вернёт веб-юзера после оплаты. Страница `/payment/return` React-приложения поллит `/api/users/me/subscription` до перехода в Active. В dev — `http://localhost:5173/payment/return`, в prod — публичный HTTPS-URL сайта. Client-side выбор URL зависит от поля `Platform` в теле `create-payment` (Mobile/Web); старые клиенты без поля считаются Mobile.
- **`PendingPaymentReuseMinutes`** (D23, default 10) — окно дедупликации платежей. Если юзер тапнул «Оформить подписку» N раз подряд, `CreatePaymentUseCase` в этот промежуток вернёт существующий `CheckoutUrl` вместо создания нового платежа в YooKassa. Это закрывает кейс «оплатил не последний платёж → webhook 404». Подбирать вровень с YooKassa confirmation_url-таймаутом (обычно 10 минут). Уменьшать только если у юзеров часто истекает payment-link до оплаты; увеличивать — если webhook у YooKassa приходит с большой задержкой.

---

## YooKassa (D16)

```json
"YooKassa": {
  "BaseUrl": "https://api.yookassa.ru",
  "ShopId": "TEST_SHOP_ID",
  "SecretKey": "test_CHANGE_ME"
}
```

- **`BaseUrl`** — API endpoint YooKassa. Дефолт `https://api.yookassa.ru` — production endpoint. Для прогона против sandbox / mock-сервера (WireMock) можно переопределить.
- **`ShopId`** — идентификатор магазина в YooKassa. **Не секрет.** Получаешь после регистрации магазина. Тестовый: `1359063` (раздаётся YooKassa).
- **`SecretKey`** — **критический секрет:**
  - НЕ коммитить в git.
  - Хранить только в локальном `appsettings.json` или env var `YooKassa__SecretKey`.
  - `test_*` — тестовый ключ. Платежи проходят, но реальные деньги не списываются.
  - `live_*` — боевой ключ. Активируется после подписания договора с YooKassa.

**Поведение:** если `SecretKey` или `ShopId` пусты → DI регистрирует `FakePaymentProvider` (все платежи всегда успешны, для integration-тестов и для open-beta без денег).

---

## Legal (D19, 152-ФЗ)

```json
"Legal": {
  "CurrentPrivacyPolicyVersion": 1,
  "CurrentTermsVersion": 1,
  "PrivacyPolicyUrl": "https://gdeoni.ru/legal/privacy",
  "TermsUrl": "https://gdeoni.ru/legal/terms"
}
```

- **`CurrentPrivacyPolicyVersion` / `CurrentTermsVersion`** — текущие версии документов на сервере. Хранятся в `User.PrivacyPolicyVersion` / `User.TermsVersion` при регистрации и при `POST /me/accept-legal`.

  Когда юзер регистрируется, в его записи фиксируется "на момент регистрации актуальной была версия N". Если потом версия изменилась — клиент через `/me` видит флаг `hasOutdatedLegalAcceptance` и показывает модалку "Документы обновились, прочитайте и подтвердите".

**Когда бампать:**
- После юр-вычитки и существенных изменений в тексте.
- Опечатки и косметику можно не бампать.
- Изменения в обработке ПД, цены, условиях возврата → **обязательно** бампать, иначе нарушение 152-ФЗ.

- **`PrivacyPolicyUrl` / `TermsUrl`** — публичные URL текстов документов. Сами тексты — в [docs/legal/](legal/) (на момент текущего коммита — шаблоны под юр-вычитку).

---

## Sentry (D21)

```json
"Sentry": {
  "Dsn": null,
  "Environment": "development",
  "Release": null,
  "TracesSampleRate": 0.0
}
```

- **`Dsn`** — Data Source Name из проекта на sentry.io (или self-hosted). Формат: `https://abc123@o123.ingest.sentry.io/456`.

  `null` = Sentry **не инициализируется** (no-op). Это нормально для:
  - local-dev (не засоряем общий dashboard);
  - integration-тестов (не отправляем фейковые ошибки);
  - первого запуска приложения до регистрации в Sentry.

- **`Environment`** — тег для фильтрации: `development` / `staging` / `production`. Помогает в Sentry-dashboard отделить прод-ошибки от dev-шума.

- **`Release`** — версия бэка для группировки. В CI обычно подставляется из тега (`backend-v1.2.3`). `null` = Sentry возьмёт assembly version.

- **`TracesSampleRate`** — семплинг трейсов (Sentry Performance):

| Значение | Поведение |
|---|---|
| `0.0` | Трейсы не отправляются, только ошибки. Дефолт. |
| `0.1` | 10% запросов идут как traces. Хороший баланс для прода. |
| `0.5` | 50%. Для дебага производительности. |
| `1.0` | Всё. Только для исследования. |

**`SendDefaultPii=false`** захардкожено в `SentryRegistration.cs` — Sentry не получает email/IP/cookies юзера. Только userId из `ClaimTypes.NameIdentifier`. Соответствие 152-ФЗ и собственной Privacy Policy.

---

## Cors

```json
"Cors": {
  "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
}
```

Whitelist origins для CORS preflight (OPTIONS-запросы). Браузер делает preflight перед POST/PUT/DELETE с auth — если origin не в whitelist, запрос даже не отправляется.

**В Development:** если секция отсутствует или `AllowedOrigins` пустой → silent fallback на стандартные dev-порты (`localhost:5173` Vite, `localhost:3000` CRA).

**В Production:** если секция отсутствует или пустая → сервер **не стартует** (`InvalidOperationException` на старте). Это сознательный fail-fast: молчаливый CORS-блок прод-фронта без логов на сервере — ад в дебаге.

**Когда менять:** появился web-фронт (F-блок), сменили домен, появился staging.

---

## Hosting

```json
"Hosting": {
  "KnownProxies": [],
  "KnownNetworks": []
}
```

Когда API стоит за reverse-proxy, `HttpContext.Connection.RemoteIpAddress` по умолчанию возвращает IP **прокси**, а не клиента. Это ломает:
- rate-limiting (D7.39) — лимит на IP прокси, а не на юзера;
- запись `CreatedFromIp` в refresh-токен (логи безопасности теряют смысл);
- request-логи в Seq.

**Решение:** `ForwardedHeadersMiddleware` читает `X-Forwarded-For`, но **только** от доверенных прокси. Whitelist обязателен — иначе любой клиент сможет подделать `X-Forwarded-For` и прикинуться чужим IP (security-уязвимость).

- **`KnownProxies`** — точные IP-адреса прокси. Пример: `["10.0.0.5", "172.20.0.1"]`.
- **`KnownNetworks`** — сети в CIDR. Удобно для k8s/docker, где IP меняются. Пример: `["10.0.0.0/8", "172.16.0.0/12"]`.

**Поведение:** если оба массива пустые → middleware **не подключается**. Loopback (127.0.0.1, ::1) ASP.NET доверяет всегда.

**Когда менять:**
- local-dev: пустые.
- prod за nginx на одном хосте: nginx обычно `127.0.0.1` → ничего не менять.
- prod за nginx на другом хосте / k8s / Cloudflare: заполнить.

---

## RateLimiting (D7.39)

```json
"RateLimiting": {
  "Auth": { "PermitLimit": 10, "WindowMinutes": 1, "SegmentsPerWindow": 6 }
}
```

Защита от brute-force на login / register / refresh / change-password / change-email. Привязывается к атрибуту `[EnableRateLimiting("auth")]` на контроллер-action'ах.

Sliding window:
- **`PermitLimit`** запросов разрешено за **`WindowMinutes`** минут с одного IP.
- **`SegmentsPerWindow`** делит окно на сегменты — чем больше, тем плавнее срабатывает лимит. Без сегментов лимит резко сбрасывается каждую минуту; с 6 сегментами — каждые 10 секунд "освобождается" 1/6 квоты.

**Текущие значения:** 10 попыток за 1 минуту с одного IP, окно разбито на 6 сегментов по 10 секунд. Хватит обычному юзеру (даже если 5 раз ошибся в пароле), остановит автоматический brute-force.

**Когда менять:**
- `PermitLimit` ниже (5) — если хочешь жёстче ловить bot'ов.
- `PermitLimit` выше (20-30) — если жалобы от юзеров на "слишком жёстко".

На клиенте: при 429 показывать "Слишком много попыток, подождите минуту".

---

## RefreshTokensCleanup

```json
"RefreshTokensCleanup": {
  "Enabled": true,
  "IntervalHours": 24,
  "RevokedRetentionDays": 30,
  "ExpiredRetentionDays": 7,
  "InitialDelayMinutes": 5
}
```

Background-service, который раз в `IntervalHours` чистит из таблицы `refresh_tokens` устаревшие записи. Без него таблица будет расти бесконечно.

- **`Enabled`** — глобальный switch. `false` → отключить (например, если делаешь cleanup через pg_cron).
- **`IntervalHours`** — раз в сколько часов запускать. 24 — раз в сутки, для большинства проектов нормально.
- **`RevokedRetentionDays`** — сколько хранить revoked-токены. 30 дней — для аудита и расследования инцидентов.
- **`ExpiredRetentionDays`** — сколько хранить expired-токены после `ExpiresAtUtc`. 7 дней — короткий запас на дебаг.
- **`InitialDelayMinutes`** — задержка перед первым прогоном после старта. 5 минут — чтобы при горячем рестарте не сразу нагружать БД.

**Когда менять:** очень большой проект → `IntervalHours=6`, retention ниже. Compliance "хранить аудит 90 дней" → бампнуть `RevokedRetentionDays`.

---

## Email (D37)

```json
"Email": {
  "Host": null,
  "Port": 587,
  "UseSsl": true,
  "UserName": null,
  "Password": null,
  "FromEmail": null,
  "FromName": "Где Они",
  "TimeoutSeconds": 30
}
```

SMTP-канал для исходящих писем (пока единственный сценарий — напоминания о годовщинах, см. `AnniversaryEmails`). Реализован на встроенном `System.Net.Mail.SmtpClient` — без внешних NuGet.

- **`Host`** — SMTP-сервер провайдера. `null` (или пустой) **выключает канал**: DI подставляет no-op отправитель, письма физически не уходят, приложение спокойно стартует без почтового сервера (dev / integration-тесты). Примеры: `smtp.yandex.ru`, `smtp.mail.ru`, `smtp.gmail.com`.
- **`Port`** — `587` (STARTTLS, типовой) / `465` (implicit SSL) / `25` (без шифрования, не рекомендуется).
- **`UseSsl`** — включает TLS (`EnableSsl`). Для 587/465 — `true`.
- **`UserName` / `Password`** — учётка SMTP. Обычно `UserName` = `FromEmail`. **`Password` — секрет**: только в локальном `appsettings.json` или env `Email__Password`. У Яндекса/Gmail это **пароль приложения**, а не пароль от аккаунта.
- **`FromEmail`** — адрес в поле From. **Обязателен** для включения канала (вместе с `Host`). Должен совпадать с почтовым ящиком, от имени которого разрешена отправка (иначе провайдер отобьёт).
- **`FromName`** — отображаемое имя отправителя.
- **`TimeoutSeconds`** — таймаут отправки одного письма.

**Прод:** держать пароль в env/секрет-менеджере; проверить, что провайдер разрешает SMTP-отправку с этого ящика (у бесплатных ящиков часто лимиты). Для больших объёмов лучше транзакционный провайдер (Unisender/SendGrid/Mailgun) — у всех есть SMTP, конфиг тот же.

---

## AnniversaryEmails (D37)

```json
"AnniversaryEmails": {
  "Enabled": false,
  "SendAtHourLocal": 9,
  "TimeZoneId": "Europe/Moscow",
  "MaxJitterSeconds": 120,
  "AppName": "Где Они",
  "AppUrl": null
}
```

Фоновый сервис: раз в сутки находит подписки с включёнными напоминаниями (`NotifyOnDeathAnniversary` / `NotifyOnBirthAnniversary` в отслеживании), у которых сегодня годовщина смерти/рождения умершего, и шлёт письмо через канал `Email`. Дедупликация — таблица `sent_anniversary_emails` (одно письмо на пользователя/умершего/тип/год).

- **`Enabled`** — главный switch. `false` (дефолт) → сервис не рассылает ничего. Включать **после** настройки секции `Email`. Если `Email` не сконфигурирован, а `Enabled=true` — прогон пропускается с warning, годовщины НЕ помечаются отправленными (уйдут, когда включат SMTP).
- **`SendAtHourLocal`** — час локального времени (0–23) для ежедневной рассылки. 9 — письмо ждёт человека к утру.
- **`TimeZoneId`** — часовой пояс для «сегодня» и часа отправки (IANA-id, на .NET 8 работает и на Windows). Нераспознанный id → fallback на UTC с warning.
- **`MaxJitterSeconds`** — случайный разброс старта (защита от одновременного старта нескольких реплик). `0` — без джиттера.
- **`AppName`** — подпись в письме.
- **`AppUrl`** — базовый URL приложения для кнопки-ссылки в письме (например, `https://gdeoni.ru`). `null` — письмо без ссылки.

**Замечание (пересмотр D18):** изначально (D18) годовщины планировались только локально на мобилке (WorkManager), сервер ничего не слал. D37 добавляет серверный email-канал — он **дополняет**, а не заменяет мобильные локальные уведомления. Web-пользователи получают напоминания только по email (у них нет мобильного клиента); мобильные могут получать и локальный push, и письмо.

**Миграция:** таблица `sent_anniversary_emails` добавляется миграцией `D37_AddSentAnniversaryEmails` — накатить `dotnet ef database update` перед стартом (иначе fail-fast на pending-миграции).

---

## Seq, Logging, Serilog

**Seq** — локальный log-агрегатор для dev. Поднимается через `docker compose up -d`:
- `5341` — HTTP-приём логов.
- `8081` — Web UI: открыть в браузере, видишь структурированные логи с поиском по полям.

В проде обычно используют ELK / Grafana Loki / Datadog.

**Logging** — базовая обвязка .NET logging. Serilog её override'ит, но эта секция тоже учитывается. `Default=Information` — стандарт. `Microsoft.AspNetCore=Warning` — гасим шумные "Request finished" логи фреймворка.

**Serilog** — структурированное логирование:
- **`Using`** — какие sink'и подгрузить (Console / Debug / Seq).
- **`MinimumLevel.Default=Information`** — глобальный уровень. `Override` — точечные снижения для ASP.NET внутренних логгеров.
- **`WriteTo`**:
  - `Console` — цветной output в терминал `dotnet run`.
  - `Debug` — Visual Studio Debug Output window.
  - `Seq` — отправка на `serverUrl`.
- **`Enrich`** — добавляет в каждое сообщение:
  - `FromLogContext` — scope-поля (TraceId, UserId).
  - `WithThreadId` — id потока.
  - `WithEnvironmentName` — `DOTNET_ENVIRONMENT`.
  - `WithMachineName` — hostname.
  - `WithEnvironmentUserName` — OS-юзер процесса.

---

## Что менять в prod

Минимально:

```jsonc
// 1. Боевая БД
"ConnectionStrings": { "DefaultConnection": "Host=prod-pg;..." }

// 2. Сильный пароль SuperAdmin
"Seed": { "SuperAdmin": { "Password": "<длинный_пароль>" } }

// 3. Сгенерированный JWT-ключ
"Jwt": { "SecretKey": "<openssl rand -base64 32>" }

// 4. Боевой BCrypt
"BCrypt": { "WorkFactor": 12 }

// 5. Боевой MinIO с SSL
"Minio": { "Endpoint": "files.gdeoni.ru", "UseSsl": true, ... }

// 6. Боевой YooKassa
"YooKassa": { "SecretKey": "live_..." }

// 7. Включить подписку
"FeatureFlags": { "SubscriptionEnabled": true, "GracePeriodDaysAfterExpiry": 2 }

// 8. Sentry DSN
"Sentry": { "Dsn": "https://...", "Environment": "production", "TracesSampleRate": 0.1 }

// 9. Прод-CORS
"Cors": { "AllowedOrigins": ["https://gdeoni.ru"] }

// 10. Если за nginx
"Hosting": { "KnownProxies": ["10.0.0.5"] }
```
