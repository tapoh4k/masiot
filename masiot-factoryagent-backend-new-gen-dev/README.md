# MAS-IOT Backend

ASP.NET Core 8 API для системы управления многоагентной производственной средой.

## Технологии

- ASP.NET Core 8
- Entity Framework Core + Npgsql
- PostgreSQL 14+
- RabbitMQ 3.12+
- BCrypt + AES-256
- Serilog

## Локальный запуск (рекомендуется)

Новому разработчику (что поставить, Docker, первый запуск): см. [`../ONBOARDING.md`](../ONBOARDING.md).

Из корня `MASIOT_2.0`:

```powershell
.\start-dev.ps1
```

Скрипт поднимет Postgres+RabbitMQ в Docker, накатит актуальный `schema.sql`, запустит backend и frontend.
Логин: `admin` / `admin123`. Чистая БД: `.\start-dev.ps1 -ResetDb`.

## Установка вручную

```bash
dotnet restore
dotnet run
```

## Docker

Сборка только API:

```bash
docker build -t masiot-backend .
```

### Production (полный стек)

Из корня репозитория:

```bash
cd deploy
cp .env.example .env
docker compose up -d --build
```

Подробности: [`../deploy/README.md`](../deploy/README.md).

Dev-infra (Postgres/RabbitMQ для `start-dev.ps1`): этот `docker-compose.yml`.

## Конфигурация

Скопировать `appsettings.Example.json` → `appsettings.json` и указать:
- ConnectionStrings:DefaultConnection (PostgreSQL)
- RabbitMQ:Host, Username, Password
- Encryption:Key, IV

## Hosts

CRUD хостов и привязка агентов: см. [`docs/HOSTS.md`](docs/HOSTS.md).  
UI: раздел **Hosts** на фронте (`/hosts`). API: `/api/hosts`.

## Документация

См. папку `docs/` — полный комплект документации для реестра ПО.
