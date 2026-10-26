# MAS-IOT Frontend

React SPA для системы управления многоагентной производственной средой.

## Технологии

- React 18
- Vite
- React Flow (визуальный редактор workflow)
- Axios

## Локальный запуск (рекомендуется)

Новому разработчику (что поставить, Docker, первый запуск): см. [`../ONBOARDING.md`](../ONBOARDING.md).

Из корня `MASIOT_2.0`:

```powershell
.\start-dev.ps1
```

Откроется http://localhost:5173 — логин `admin` / `admin123`.

## Установка вручную

```bash
npm install
npm run dev
```

## Сборка

```bash
npm run build
```

## Docker

Сборка только UI:

```bash
docker build -f Dockerfile.frontend --build-arg VITE_API_URL=/api -t masiot-frontend .
```

### Production (полный стек)

```bash
cd ../deploy
cp .env.example .env
docker compose up -d --build
```

UI: `http://localhost/` (nginx проксирует `/api` на backend).  
Подробности: [`../deploy/README.md`](../deploy/README.md).

## Hosts

Раздел **Hosts** в сайдбаре — карточки хостов (тип, число агентов, статус).  
При создании агента (ветка / XLSX) обязателен выбор Host.

## Конфигурация

- Dev: `http://localhost:3001/api` (по умолчанию в `api.js`)
- Prod Docker: `VITE_API_URL=/api` (same-origin через nginx)
