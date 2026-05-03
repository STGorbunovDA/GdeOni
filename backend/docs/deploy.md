# Production deploy

Этот документ описывает развёртывание GdeOni backend в production.
Затрагивает только инфраструктурные шаги, которые отличаются от
локальной разработки.

## Состав

- API (`GdeOni.API`) — HTTP, ASP.NET Core 8.
- PostgreSQL.
- MinIO (S3-совместимое хранилище медиа).
- Seq (опционально, для логов).
- nginx — TLS termination + reverse proxy.

## Переменные окружения

Создаются на хосте (`.env` рядом с `docker-compose.yml`) и подставляются
в `docker-compose.yml` через `${VAR}` или передаются прямо в `appsettings.Production.json`.

| Переменная | Назначение |
|---|---|
| `POSTGRES_PASSWORD` | Пароль PostgreSQL. |
| `MINIO_ROOT_USER` | Боевой access key MinIO (не `minioadmin`). |
| `MINIO_ROOT_PASSWORD` | Боевой secret key MinIO. |
| `MINIO_SERVER_URL` | Публичный URL MinIO (https://files.example.com). MinIO подставит его в presigned URL вместо внутреннего `minio:9000`. |
| `MINIO_BROWSER_REDIRECT_URL` | Публичный URL консоли MinIO (опционально). |

## docker-compose: production-режим

В `backend/docker-compose.yml` есть закомментированный блок `environment:`
для сервиса `minio`. На production:

1. Создать `.env` рядом с compose-файлом.
2. Раскомментировать блок `environment:` под `minio`.
3. Подставить `MINIO_SERVER_URL` на свой публичный домен.
4. Опционально — поднять PostgreSQL и Seq за nginx, открыть наружу
   только порты, которые реально нужны.

## Presigned URL и публичный домен

Backend генерирует **presigned URL** для документов
(`MediaKind.Document`) — клиент скачивает файл напрямую из MinIO.

В коде ([MinioFileStorage.cs](../src/GdeOni.Infrastructure/Storage/MinioFileStorage.cs))
создаются два `IMinioClient`:

- **internal** — для `Upload`/`Delete`/`GetObject`. Endpoint = `Minio:Endpoint`
  (внутри сети, например `minio:9000`).
- **presigned** — только для `PresignedGetObjectAsync`. Endpoint =
  host из `Minio:PublicBaseUrl`. Если `PublicBaseUrl` не задан, оба
  клиента совпадают (dev-режим).

Дополнительно на стороне MinIO задаётся `MINIO_SERVER_URL` — это
страховка, чтобы даже при ошибочной конфигурации backend сервер
сам подменил host в подписи.

**Двойная защита:** работает любой из двух механизмов.

## nginx: пример конфига

```nginx
# /etc/nginx/sites-available/gdeoni
server {
    listen 443 ssl http2;
    server_name api.gdeoni.example.com;

    ssl_certificate     /etc/letsencrypt/live/api.gdeoni.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.gdeoni.example.com/privkey.pem;

    client_max_body_size 50m;  # под лимит /media upload

    location / {
        proxy_pass         http://127.0.0.1:8080;  # API
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}

server {
    listen 443 ssl http2;
    server_name files.gdeoni.example.com;

    ssl_certificate     /etc/letsencrypt/live/files.gdeoni.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/files.gdeoni.example.com/privkey.pem;

    client_max_body_size 50m;

    # MinIO S3 API
    location / {
        proxy_pass         http://127.0.0.1:9000;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_connect_timeout 300;
        proxy_http_version 1.1;
        chunked_transfer_encoding off;
    }
}

# HTTP -> HTTPS redirect
server {
    listen 80;
    server_name api.gdeoni.example.com files.gdeoni.example.com;
    return 301 https://$host$request_uri;
}
```

## appsettings.Production.json

```json
{
  "Minio": {
    "Endpoint": "minio:9000",
    "AccessKey": "<MINIO_ROOT_USER>",
    "SecretKey": "<MINIO_ROOT_PASSWORD>",
    "UseSsl": false,
    "PublicBaseUrl": "https://files.gdeoni.example.com",
    "Buckets": {
      "DeceasedPhotos": "deceased-photos",
      "GravePhotos": "grave-photos",
      "DeceasedDocuments": "deceased-documents"
    }
  }
}
```

`UseSsl: false` — это про связь backend↔MinIO внутри docker-сети,
по HTTP. TLS terminating делает nginx снаружи.

## Проверка

После деплоя:

1. `curl -I https://api.gdeoni.example.com/swagger/index.html` → 200.
2. Загрузить фото через `POST /api/deceased-records/{id}/media` → 200.
3. `GET /api/deceased-records/{id}/media/{mediaId}` для документа →
   в ответе поле `url` начинается с `https://files.gdeoni.example.com/`,
   а не с `http://minio:9000/`.
4. Открыть этот URL в инкогнито-вкладке браузера → файл скачивается.
