# Описание архитектуры

**Продукт:** MAS-IOT  
**Версия:** 1.0.0

---

## Общая архитектура

```
┌─────────────────┐      HTTP API       ┌──────────────────┐
│   Browser       │ ◄─────────────────► │  ASP.NET Core 8  │
│  (React + Vite) │                     │     Backend      │
└─────────────────┘                     └────────┬─────────┘
                                                  │
                       ┌──────────────────────────┼──────────┐
                       │                          │          │
                       ▼                          ▼          ▼
                ┌─────────────┐          ┌─────────────┐ ┌─────────┐
                │  PostgreSQL │          │  RabbitMQ   │ │ Serilog │
                │    14+      │          │   3.12+     │ │  Logs   │
                └─────────────┘          └─────────────┘ └─────────┘
```

## Компоненты системы

### Frontend (React 18 + Vite)

- **SPA** (Single Page Application)
- **React Flow** — визуальный редактор workflow
- **Axios / Fetch** — HTTP-клиент для API
- **Vite** — сборщик и dev-сервер

### Backend (ASP.NET Core 8)

- **Контроллеры** — REST API endpoints
- **Entity Framework Core** + **Npgsql** — работа с PostgreSQL
- **RabbitMQ.Client** — обмен сообщениями
- **BCrypt.Net-Next** — хеширование паролей
- **AES-256-CBC** — шифрование паролей и RabbitMQ-сообщений
- **Serilog** — структурированное логирование

### База данных (PostgreSQL)

- 13+ таблиц
- JSONB для гибких структур (workflow, параметры)
- Иерархические связи (tree_nodes, okpd2_dictionary)

### Очередь сообщений (RabbitMQ)

- Topic exchange: `topic_logs`
- Routing keys: `agent.{id}`, `okpd2.{code}`
- Сквозное AES-шифрование сообщений

## Уровни приложения

1. **Presentation** — Controllers, DTOs, Validation
2. **Business Logic** — Services, Workflow Engine, RabbitMQ Integration
3. **Data Access** — EF Core DbContext, Repositories
4. **External** — PostgreSQL, RabbitMQ, File System

## Security Layer

- Двухуровневая система паролей: BCrypt (аутентификация) + AES-256 (хранение)
- CORS с ограничением localhost
- Rate Limiting на login
- API Key для мониторинга
