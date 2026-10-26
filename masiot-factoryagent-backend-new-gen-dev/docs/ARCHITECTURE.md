# Описание архитектуры

## 1. Общая архитектурная схема

```
┌─────────────────────────────────────────────────────────────┐
│                        Клиент                                │
│              (React 18 + Vite + React Flow)                 │
│                     localhost:5173                          │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP/REST
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                      Nginx (Docker)                          │
│         (SPA-роутинг, gzip, proxy /api → backend)           │
└──────────────────────┬──────────────────────────────────────┘
                       │
         ┌─────────────┴─────────────┐
         │                           │
         ▼                           ▼
┌──────────────┐           ┌──────────────────────┐
│   Frontend   │           │       Backend        │
│   (React)    │◄─────────►│  ASP.NET Core 8 API  │
│              │  fetch    │     localhost:3001   │
└──────────────┘           └──────────┬───────────┘
                                      │
                       ┌──────────────┼──────────────┐
                       │              │              │
                       ▼              ▼              ▼
                ┌──────────┐  ┌──────────┐  ┌──────────────┐
                │PostgreSQL│  │ RabbitMQ │  │   Serilog    │
                │  :5432   │  │  :5672   │  │  (лог-файлы) │
                └──────────┘  └──────────┘  └──────────────┘
```

---

## 2. Backend (ASP.NET Core 8)

### 2.1. Многослойная архитектура

```
┌────────────────────────────────────────┐
│            Presentation Layer          │
│  Controllers + DTOs + Middleware       │
│  (REST API, валидация, обработка       │
│   исключений, Swagger)                 │
├────────────────────────────────────────┤
│           Business Logic Layer         │
│  Services (Workflow Engine, RabbitMQ,  │
│   Security, TechProcess Parser)        │
├────────────────────────────────────────┤
│           Data Access Layer            │
│  Repositories + EF Core + PostgreSQL   │
└────────────────────────────────────────┘
```

### 2.2. Контроллеры (API Endpoints)

| Контроллер | Назначение |
|------------|-----------|
| `TreeController` | Иерархическое дерево узлов (`tree_nodes`) |
| `AgentsController` | Агенты, параметры, ОКПД-2, импорт XLSX |
| `WorkflowController` | Запуск, продолжение, управление экземплярами workflow |
| `WorkflowTemplatesController` | CRUD шаблонов workflow (React Flow JSON) |
| `WorkflowFunctionsController` | Реестр 55 исполняемых функций |
| `UsersController` | Аутентификация (BCrypt), CRUD пользователей |
| `MonitoringController` | Защищённая статистика (API Key) |
| `MessagesController` / `AgentMessagesController` | Отправка сообщений в RabbitMQ |
| `TechProcessController` | Парсинг XLSX, управление техпроцессами |
| `ParameterDictionaryController` | Справочник параметров |
| `Okpd2DictionaryController` / `BlankOkpd2Controller` | Классификатор ОКПД-2 |

### 2.3. Сервисы

#### Workflow Engine
- **`WorkflowExecutionService`** — загружает шаблон, валидирует входные параметры, конвертирует React Flow → линейная последовательность функций, исполняет шаги через Reflection.
- **`WorkflowFormatConverter`** — топологическая сортировка узлов (алгоритм Кана) для определения порядка выполнения.
- **`WorkflowFunctionWhitelist`** — жёсткий белый список разрешённых классов, предотвращающий RCE.
- **`ParameterValidationService`** — проверка типов входных параметров функций.

#### RabbitMQ
- **`RabbitMQService`** — публикация/подписка через exchange `topic_logs` (тип `topic`).
- **`RabbitMQHostedService`** — фоновый HostedService для прослушивания очереди `backend_queue`.
- **`MessageProcessingService`** — обработка входящих сообщений (типы `3-6-1-1`, `3-6-1-2`, `work`).

#### Безопасность
- **`EncryptionService`** — AES-256 CBC для обратимого шифрования паролей (для просмотра админом).
- **`PasswordHashService`** — BCrypt с work factor 12 для безопасной аутентификации.

### 2.4. База данных (PostgreSQL)

**Основные сущности:**

```
tree_nodes (id, name, type, parent_id, position)
  └── agents (id, node_id, name, type, okpd2_code, status, ...)
        └── agent_parameters (id, agent_id, name, value, unit, description)

workflow_templates (id, name, nodes JSONB, edges JSONB)
workflow_functions (id, function_class, function_name, params JSONB, ...)
wf_list (id, agent_id, step, status, parameters JSONB, wf_functions JSONB)

tech_processes (id, code, name, ...)
  └── tech_process_operations (id, tech_process_id, number, type, ...)

parameter_dictionary (id, name UNIQUE, description, type, unit)
okpd2_dictionary (id, code UNIQUE, parent_code, level, name)
blank_okpd2_mapping (id, keywords, okpd2_code, description)

users (id, username UNIQUE, password AES, password_hash BCrypt, role, full_name)
```

---

## 3. Frontend (React 18 + Vite)

### 3.1. Архитектура компонентов

```
App.jsx (Router, глобальное состояние дерева, авторизация)
├── Sidebar.jsx (навигационное дерево, drag-and-drop, поиск)
├── Header.jsx (шапка, пользователь, выход)
├── MainPage.jsx (главная страница, загрузка XLSX, статистика)
├── BranchPage.jsx (карточка папки/ветки: параметры, агенты, workflow)
├── AgentPage.jsx (карточка агента: параметры, workflow, клонирование)
├── LoginPage.jsx (форма авторизации)
├── UsersPage.jsx (управление пользователями, только admin)
├── WorkflowTemplateEditor.jsx (React Flow редактор шаблонов)
├── WorkflowMonitor.jsx (мониторинг запущенных workflow)
├── SettingsModal.jsx (словарь параметров + ОКПД-2 маппинг)
├── AgentCreationModal.jsx (массовое создание агентов из XLSX)
└── nodes/ (FunctionNode, WFNode, SubWFNode — кастомные узлы React Flow)
```

### 3.2. Слои API

- **`services/api.js`** — центральный HTTP-клиент на `fetch` (~500 строк), покрывает все endpoint'ы backend.
- **`api/userAPI.js`** — отдельный axios-клиент для аутентификации (наследие ранней версии).

### 3.3. Управление состоянием

- Глобальное состояние дерева (`treeNodes`) хранится в `App.jsx` и передаётся через props.
- Авторизация (`currentUser`) — `localStorage`.
- Раскрытые узлы дерева — `localStorage` (`expandedNodes`).
- Активные вкладки страниц — `localStorage`.

---

## 4. Workflow Engine

### 4.1. Жизненный цикл workflow

```
1. Пользователь создаёт шаблон в визуальном редакторе (React Flow)
        │
        ▼
2. Шаблон сохраняется в workflow_templates (nodes + edges в JSONB)
        │
        ▼
3. Запуск через POST /api/workflow/start
   → Создаётся запись в wf_list (status = running)
        │
        ▼
4. WorkflowExecutionService конвертирует граф в линейную
   последовательность функций (топологическая сортировка)
        │
        ▼
5. Каждая функция вызывается через Reflection по whitelist
   с Dependency Injection
        │
        ▼
6. После каждого шага обновляются step, parameters, status в wf_list
        │
        ▼
7. При ошибке → status = error, возможен Continue с произвольного шага
```

### 4.2. Категории функций

| Категория | Примеры |
|-----------|---------|
| `RabbitMQFunctions` | Отправка сообщений агентам / по ОКПД-2 |
| `AgentInfoFunctions` | Чтение/обновление статуса и параметров агентов |
| `TimeTableFunctions` | CRUD расписания работ |
| `WFListFunctions` | Управление экземплярами workflow |
| `TechProcessFunctions` | Чтение операций, расчёт времени |
| `WorkflowControlFunctions` | Sleep, Stop, Continue, условные переходы |
| `PlanningFunctions` | WF1-WF5.2: полный цикл планирования производства |
| `ExampleWorkflowFunctions` | Тестовые: Add, SayHello, LogMessage |

---

## 5. Интеграционные потоки

### 5.1. Импорт техпроцессов (XLSX)

```
XLSX-файл
    │
    ▼
TechProcessXlsxParser (ExcelDataReader)
    │
    ├──► Создание папки «ТП» в дереве
    ├──► Создание агента типа tech_process
    ├──► Сохранение TechProcess + TechProcessOperation в БД
    └──► Создание PendingMessage (draft)
                │
                ▼
        POST /api/tech-process/start/{agentId}
                │
                ▼
        Отправка сообщения в RabbitMQ (agent.{id})
```

### 5.2. Агентный цикл планирования

```
WF1: TechprocessPrepare
    │
    ▼
WF2: PrepareMessagesToAgentAboutJob → Broadcast по ОКПД-2
    │
    ▼
WF2.1: AddTimetableEvent (сбор ответов от ресурсов)
    │
    ▼
WF3: PrepareMessageToSPR (отправка в систему планирования ресурсов)
    │
    ▼
WF4: ProcessingSPRResponse (обработка решения)
    │
    ▼
WF5: GetOperationTimeEnd → WF5.1: NextOperationExist?
    │
    ▼
WF5.2: UpdateOperationTimeStart → возврат к WF2
```

---

## 6. Масштабирование и отказоустойчивость

### 6.1. Горизонтальное масштабирование

- Backend — stateless, можно запустить несколько инстансов за load balancer.
- PostgreSQL — репликация (streaming replication).
- RabbitMQ — clustering.
- Frontend — статические файлы, раздаются через CDN или Nginx.

### 6.2. Резервное копирование

```bash
# БД
pg_dump -U postgres -d agenti_db > backup.sql

# Логи
Backend/logs/backend-*.log
```

### 6.3. Мониторинг

- Serilog пишет структурированные логи в `Backend/logs/`.
- RabbitMQ Management UI — метрики очередей.
- `MonitoringController` — защищённый API для внешних систем мониторинга.
