# Обзор API

## 1. Общая информация

- **Базовый URL:** `http://localhost:3001/api`
- **Формат:** JSON
- **Аутентификация:** Basic (логин/пароль через `POST /api/users/login`, возвращает JSON с `role`)
- **Документация Swagger:** `http://localhost:3001/swagger`
- **Postman-коллекция:** `postman_collection.json` (в корне проекта)

---

## 2. Группы endpoint'ов

### 2.1. Дерево (`/api/tree`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `GET` | `/api/tree` | Получить все узлы дерева (плоский список) |
| `POST` | `/api/tree` | Создать узел |
| `PUT` | `/api/tree/{id}` | Обновить узел (`name`, `icon`, `parent_id`, `type`) |
| `PUT` | `/api/tree/{id}/parent` | Сбросить `parent_id` в `null` (перемещение на корневой уровень) |
| `DELETE` | `/api/tree/{id}` | Удалить узел каскадно |

**Структура `TreeNode`:**
```json
{
  "id": "glavnaya",
  "name": "Главная",
  "icon": "🏠",
  "type": "root",
  "parent_id": null,
  "position": 0
}
```

---

### 2.2. Агенты (`/api/agents`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `GET` | `/api/agents` | Получить всех агентов с параметрами |
| `GET` | `/api/agents/{nodeId}` | Получить агентов по `nodeId` (строка) |
| `GET` | `/api/agents/okpd2/{code}` | Получить агентов по коду ОКПД-2 |
| `POST` | `/api/agents` | Создать агента (+ параметры, + авто-добавление в словарь) |
| `PUT` | `/api/agents/{id}` | Обновить агента (+ параметры, + авто-добавление в словарь) |
| `DELETE` | `/api/agents/{id}` | Удалить агента |

**Создание агента:**
```json
{
  "node_id": "node-123",
  "name": "Станок токарный",
  "type": "equipment",
  "quantity": 2,
  "queue_number": 1,
  "okpd2_code": "28.41.12.000",
  "parameters": [
    {"name": "Мощность", "value": "5000", "unit": "Вт", "description": "Мощность двигателя"}
  ]
}
```

---

### 2.3. Workflow (`/api/workflow`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `GET` | `/api/workflow` | Все запущенные workflow |
| `GET` | `/api/workflow/{id}` | Конкретный экземпляр |
| `GET` | `/api/workflow/agent/{agentId}` | Workflow по агенту |
| `GET` | `/api/workflow/status/{status}` | Workflow по статусу (`running`, `completed`, `error`) |
| `POST` | `/api/workflow/start` | Запустить новый workflow |
| `POST` | `/api/workflow/continue/{id}` | Продолжить workflow с указанного шага |
| `PATCH` | `/api/workflow/{id}/step` | Обновить текущий шаг |
| `PATCH` | `/api/workflow/{id}/status` | Обновить статус |
| `DELETE` | `/api/workflow/{id}` | Удалить запись |

**Запуск workflow:**
```json
{
  "agent_id": 42,
  "template_id": 5,
  "parameters": {"orderNumber": "ЗН-001"}
}
```

---

### 2.4. Шаблоны workflow (`/api/workflow-templates`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `GET` | `/api/workflow-templates` | Все шаблоны |
| `GET` | `/api/workflow-templates/{id}` | Шаблон по ID |
| `POST` | `/api/workflow-templates` | Создать шаблон |
| `PUT` | `/api/workflow-templates/{id}` | Обновить шаблон |
| `DELETE` | `/api/workflow-templates/{id}` | Удалить шаблон |

**Структура шаблона:**
```json
{
  "name": "Планирование заказа",
  "nodes": [...],
  "edges": [...]
}
```

---

### 2.5. Функции workflow (`/api/workflowfunctions`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `GET` | `/api/workflowfunctions` | Все функции (с фильтром по категориям) |
| `GET` | `/api/workflowfunctions/{id}` | Функция по ID |
| `POST` | `/api/workflowfunctions` | Создать функцию |
| `PUT` | `/api/workflowfunctions/{id}` | Обновить функцию |
| `DELETE` | `/api/workflowfunctions/{id}` | Удалить функцию |

---

### 2.6. Пользователи (`/api/users`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `POST` | `/api/users/login` | Аутентификация |
| `GET` | `/api/users` | Список пользователей |
| `GET` | `/api/users/{id}` | Пользователь по ID |
| `POST` | `/api/users` | Создать пользователя |
| `PUT` | `/api/users/{id}` | Обновить пользователя |
| `DELETE` | `/api/users/{id}` | Удалить пользователя |

**Логин:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

---

### 2.7. Словарь параметров (`/api/parameter-dictionary`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `GET` | `/api/parameter-dictionary` | Все параметры |
| `GET` | `/api/parameter-dictionary/{id}` | Параметр по ID |
| `POST` | `/api/parameter-dictionary` | Создать параметр |
| `PUT` | `/api/parameter-dictionary/{id}` | Обновить параметр |
| `DELETE` | `/api/parameter-dictionary/{id}` | Удалить параметр |

---

### 2.8. ОКПД-2 (`/api/okpd2-dictionary`, `/api/blank-okpd2`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `GET` | `/api/okpd2-dictionary` | Весь классификатор |
| `POST` | `/api/okpd2-dictionary` | Добавить запись |
| `GET` | `/api/blank-okpd2/find-by-name?name=Вал` | Поиск ОКПД-2 по названию |
| `GET` | `/api/blank-okpd2/all` | Все маппинги |
| `POST` | `/api/blank-okpd2/bulk-upsert` | Массовая вставка/обновление |

---

### 2.9. Техпроцессы (`/api/tech-process`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `POST` | `/api/tech-process/attach` | Загрузить XLSX (multipart/form-data) |
| `POST` | `/api/tech-process/start/{agentId}` | Запустить техпроцесс (отправка draft → RabbitMQ) |
| `GET` | `/api/tech-process/agent/{agentId}` | Получить техпроцесс агента |

---

### 2.10. Сообщения (`/api/messages`, `/api/agent-messages`)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `POST` | `/api/messages/work` | Отправить рабочее сообщение |
| `POST` | `/api/messages/agent-info` | Отправить info-сообщение |
| `POST` | `/api/messages/new-workflow` | Отправить сообщение о новом workflow |
| `POST` | `/api/agent-messages/{agentId}` | Отправить сообщение конкретному агенту |

---

### 2.11. Мониторинг (`/api/monitoring`)

| Метод | Endpoint | Заголовок | Описание |
|-------|----------|-----------|----------|
| `GET` | `/api/monitoring/statistics` | `X-API-Key` | Статистика по типам агентов |
| `GET` | `/api/monitoring/users` | `X-API-Key` | Данные пользователей |
| `POST` | `/api/monitoring/decrypt` | `X-API-Key` | Расшифровка пароля |

---

## 3. Модели данных (JSON)

### 3.1. Agent

```json
{
  "id": 42,
  "node_id": "node-1777564094239",
  "name": "Токарный станок",
  "type": "equipment",
  "status": 0,
  "quantity": 3,
  "queue_number": 1,
  "okpd2_code": "28.41.12.000",
  "agent_type": "resource",
  "parameters": [
    {
      "id": 100,
      "agent_id": 42,
      "name": "Мощность",
      "value": "5000",
      "unit": "Вт",
      "description": "Мощность двигателя"
    }
  ],
  "workflow_applied": false,
  "workflow_status": "not_ready",
  "applied_workflows": [],
  "created_at": "2026-04-30T18:48:14.287128",
  "updated_at": "2026-04-30T18:48:14.287128"
}
```

### 3.2. WorkflowTemplate

```json
{
  "id": 5,
  "name": "Планирование",
  "nodes": [
    {"id": "1", "type": "function", "data": {"label": "Подготовка ТП"}, "position": {"x": 0, "y": 0}}
  ],
  "edges": [
    {"id": "e1-2", "source": "1", "target": "2"}
  ]
}
```

---

## 4. Коды ответов

| Код | Значение | Когда возникает |
|-----|----------|-----------------|
| 200 | OK | Успешный GET/PUT/PATCH |
| 201 | Created | Успешный POST |
| 400 | Bad Request | Ошибка валидации ModelState |
| 401 | Unauthorized | Нет прав (неверный API-ключ или не авторизован) |
| 404 | Not Found | Ресурс не найден |
| 500 | Internal Server Error | Необработанное исключение (см. логи backend) |

---

## 5. Примеры интеграции

### 5.1. curl: Создание агента

```bash
curl -X POST http://localhost:3001/api/agents \
  -H "Content-Type: application/json" \
  -d '{
    "node_id": "node-123",
    "name": "Фрезерный станок",
    "type": "equipment",
    "quantity": 1,
    "okpd2_code": "28.41.13.000",
    "parameters": [{"name": "Мощность", "value": "7500", "unit": "Вт"}]
  }'
```

### 5.2. curl: Запуск workflow

```bash
curl -X POST http://localhost:3001/api/workflow/start \
  -H "Content-Type: application/json" \
  -d '{"agent_id": 42, "template_id": 5, "parameters": {"order": "ЗН-001"}}'
```

### 5.3. curl: Поиск ОКПД-2

```bash
curl "http://localhost:3001/api/blank-okpd2/find-by-name?name=Вал%20ступенчатый"
```
