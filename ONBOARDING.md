# Онбординг разработчика — как поднять MAS-IOT локально

Краткая инструкция: что поставить и как запустить проект с нуля (Windows).

## Что это за проект

| Папка | Что внутри |
|-------|------------|
| `masiot-factoryagent-backend-new-gen-dev` | API (ASP.NET Core 8), порт **3001** |
| `masiot-factoryagent-front-new-gen-dev` | UI (React + Vite), порт **5173** |
| `start-dev.ps1` / `stop-dev.ps1` | Один клик: Postgres + RabbitMQ + backend + frontend |
| `deploy/` | Production Docker-стек (не для повседневной разработки) |

Логин по умолчанию: **admin** / **admin123**.

---

## 1. Что установить (обязательно)

1. **Git**
2. **[.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)** — проверь: `dotnet --version` → начинается с `8.`
3. **[Node.js LTS 20+](https://nodejs.org/)** — проверь: `node -v`, `npm -v`
4. **Docker Desktop** — нужен для Postgres и RabbitMQ  
   - Скачать: https://www.docker.com/products/docker-desktop/  
   - После установки: перезагрузка / вход в систему, запустить Docker Desktop  
   - Дождись статуса **Docker is running** (китовский значок в трее)  
   - Проверь: `docker version` и `docker compose version`

Без Docker проект **не поднимется** через `start-dev.ps1`. Вариант без Docker — в конце файла (сложнее).

---

## 2. Получить код

```powershell
# клон / обновление репозитория — как у вас принято в команде
cd C:\path\to\MASIOT_2.0
```

Структура должна выглядеть так:

```
MASIOT_2.0\
  start-dev.ps1
  stop-dev.ps1
  masiot-factoryagent-backend-new-gen-dev\
  masiot-factoryagent-front-new-gen-dev\
```

---

## 3. Запуск (рекомендуемый способ)

1. Запусти **Docker Desktop** и дождись, пока он станет Ready.
2. В PowerShell из корня `MASIOT_2.0`:

```powershell
.\start-dev.ps1
```

Скрипт сам:

- поднимет контейнеры Postgres (`localhost:5433`) и RabbitMQ (`5672`, UI `15672`);
- накатит `schema.sql` (таблицы, seed, логин admin);
- откроет два окна: backend и frontend.

Открой в браузере: http://localhost:5173  
Логин: `admin` / `admin123`.

Проверка API: http://localhost:3001/api/health  
Swagger: http://localhost:3001/swagger

Остановка инфраструктуры:

```powershell
.\stop-dev.ps1
```

Чистая БД с нуля:

```powershell
.\start-dev.ps1 -ResetDb
```

---

## 4. Порты (чтобы ничего не конфликтовало)

| Сервис | Порт |
|--------|------|
| Frontend (Vite) | 5173 |
| Backend (API) | 3001 |
| Postgres (Docker) | **5433** ← не 5432 |
| RabbitMQ AMQP | 5672 |
| RabbitMQ Management UI | 15672 (guest/guest) |

Если порт занят — закрой старый процесс или смени порт в настройках.

---

## 5. Если что-то не стартует

| Симптом | Что сделать |
|---------|-------------|
| `Docker не найден` | Установи Docker Desktop, перезапусти терминал, проверь `docker version` |
| Docker висит / WSL error | В Docker Desktop: Settings → Resources / WSL integration; перезапусти Docker |
| Backend: ошибка подключения к БД / `28P01` | Postgres должен быть на **5433**, пароль `postgres`. Перезапусти `.\start-dev.ps1` |
| Backend: RabbitMQ connection refused | Контейнер `agenti_rabbitmq` не поднят — снова `.\start-dev.ps1` |
| Frontend пустой / CORS | Backend должен слушать `0.0.0.0`/localhost:3001; открой UI как `http://localhost:5173` |
| Первый `npm install` долго | Нормально; нужен интернет |
| Нет логина admin | Накати схему: `.\start-dev.ps1` или `-ResetDb` |

Ручной перезапуск только приложений (если Docker уже крутится):

```powershell
# окно 1 — backend
cd masiot-factoryagent-backend-new-gen-dev
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run

# окно 2 — frontend
cd masiot-factoryagent-front-new-gen-dev
npm install   # один раз
npm run dev
```

Конфиг БД/RabbitMQ для Development уже в  
`masiot-factoryagent-backend-new-gen-dev/appsettings.Development.json`  
(Host=localhost, Port=5433, user/pass postgres).

---

## 6. Без Docker (только если Docker поставить нельзя)

Нужно поставить вручную:

1. **PostgreSQL 14+** (служба на машине)
2. **RabbitMQ 3.12+** с плагином management (опционально)

### Postgres

```powershell
# создать БД (пример через psql)
psql -U postgres -c "CREATE DATABASE agenti_db;"
psql -U postgres -d agenti_db -f masiot-factoryagent-backend-new-gen-dev\schema.sql
```

В `appsettings.Development.json` выставь **свой** порт/пароль, например:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=agenti_db;Username=postgres;Password=ТВОЙ_ПАРОЛЬ;Encoding=UTF8"
```

### RabbitMQ

Сервис должен слушать `localhost:5672`, пользователь `guest` / `guest`  
(или поправь секцию `RabbitMQ` в `appsettings.Development.json`).

### Запуск приложений

Те же две команды `dotnet run` и `npm run dev` из раздела 5.

`start-dev.ps1` без Docker **не работает**.

---

## 7. Куда смотреть дальше

- Hosts (привязка агентов к хостам): `masiot-factoryagent-backend-new-gen-dev/docs/HOSTS.md`
- Редактор workflow: UI → раздел шаблонов workflow (`/workflow-templates`)
- Production deploy: `deploy/README.md`

Если после установки Docker и `.\start-dev.ps1` логин не открывается — напиши в чат команды и текст ошибки из окна backend.
