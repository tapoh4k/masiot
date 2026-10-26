# MAS-IOT — Docker production stack

Единая точка входа для будущего деплоя на сервер: Postgres + RabbitMQ + Backend + Frontend (nginx).

## Требования

- Docker Engine 24+
- Docker Compose v2

## Быстрый старт (локальная проверка)

```bash
cd deploy
cp .env.example .env
# отредактируйте пароли и ENCRYPTION_* в .env

docker compose up -d --build
```

Проверка:

```bash
curl http://localhost/api/health
# UI: http://localhost/
# Логин: admin / admin123  (из schema.sql)
# RabbitMQ Management (только localhost): http://127.0.0.1:15672
```

Остановка:

```bash
docker compose down
# с удалением данных БД:
docker compose down -v
```

## Что внутри

| Сервис | Контейнер | Назначение |
|--------|-----------|------------|
| `postgres` | `masiot_postgres` | БД, init из `schema.sql` (только при первом создании volume) |
| `rabbitmq` | `masiot_rabbitmq` | брокер + Management plugin |
| `backend` | `masiot_backend` | ASP.NET Core API на `:3001` (внутри сети) |
| `frontend` | `masiot_frontend` | SPA + nginx proxy `/api` → backend |

Postgres и AMQP наружу **не** публикуются. Снаружи открыт только UI (`FRONTEND_PORT`, по умолчанию 80).

## Деплой на сервер (позже)

1. Скопировать репозиторий на сервер (`git clone` / `scp` / CI).
2. На сервере: `cd deploy && cp .env.example .env` и задать **сильные** секреты.
3. `docker compose up -d --build`
4. (Рекомендуется) поставить reverse-proxy с TLS (nginx/Caddy/Traefik) перед портом `FRONTEND_PORT`.

Обновление схемы БД на уже существующем volume: `schema.sql` идемпотентен — можно применить вручную:

```bash
docker compose exec -T postgres psql -U postgres -d agenti_db < ../masiot-factoryagent-backend-new-gen-dev/schema.sql
```

## Локальная разработка

Для daily-dev используйте корневые скрипты `start-dev.ps1` / `stop-dev.ps1` (Docker только для Postgres/RabbitMQ + `dotnet`/`npm` на хосте).  
Этот каталог `deploy/` — для prod-подобного полного стека.
