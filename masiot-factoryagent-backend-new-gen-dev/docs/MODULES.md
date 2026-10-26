# Описание модулей

## 1. Backend-модули

### 1.1. Контроллеры (`Controllers/`)

Контроллеры реализуют REST API и служат точкой входа для HTTP-запросов. Каждый контроллер отвечает за отдельную предметную область.

| Модуль | Файл | Ключевые endpoint'ы | Назначение |
|--------|------|---------------------|------------|
| **Tree** | `TreeController.cs` | `GET /api/tree`, `POST /api/tree`, `PUT /api/tree/{id}`, `DELETE /api/tree/{id}`, `PUT /api/tree/{id}/parent` | Управление иерархическим деревом узлов (`tree_nodes`). Поддерживает сброс `parent_id` в `null` через отдельный endpoint. |
| **Agents** | `AgentsController.cs` | `GET /api/agents`, `GET /api/agents/{nodeId}`, `POST /api/agents`, `PUT /api/agents/{id}`, `DELETE /api/agents/{id}`, `GET /api/agents/okpd2/{code}` | CRUD агентов, поиск по `nodeId` и `okpd2_code`, автоматическое наполнение `blank_okpd2_mapping`, синхронизация параметров со словарём. |
| **Workflow** | `WorkflowController.cs` | `POST /api/workflow/start`, `POST /api/workflow/continue/{id}`, `GET /api/workflow/{id}`, `GET /api/workflow/agent/{agentId}` | Запуск и продолжение экземпляров workflow, управление шагами и статусами. |
| **Workflow Templates** | `WorkflowTemplatesController.cs` | `GET /api/workflow-templates`, `POST /api/workflow-templates`, `PUT /api/workflow-templates/{id}`, `DELETE /api/workflow-templates/{id}` | CRUD шаблонов workflow. Хранит графы React Flow в формате JSONB (`nodes`, `edges`). |
| **Workflow Functions** | `WorkflowFunctionsController.cs` | `GET /api/workflowfunctions`, `POST /api/workflowfunctions`, `PUT /api/workflowfunctions/{id}` | Управление реестром 55 исполняемых функций (метаданные для Reflection). |
| **Users** | `UsersController.cs` | `POST /api/users/login`, `GET /api/users`, `POST /api/users`, `PUT /api/users/{id}`, `DELETE /api/users/{id}` | Аутентификация через BCrypt, CRUD пользователей с ролями `admin`/`user`. |
| **Admin** | `AdminController.cs` | `POST /api/admin/migrate-passwords` | Ручной запуск миграции паролей на BCrypt (для обновления legacy-установок). |
| **Monitoring** | `MonitoringController.cs` | `GET /api/monitoring/statistics`, `GET /api/monitoring/users`, `POST /api/monitoring/decrypt` | Защищённые endpoint'ы статистики (требуют `X-API-Key`). |
| **Messages** | `MessagesController.cs` | `POST /api/messages/work`, `POST /api/messages/agent-info`, `POST /api/messages/new-workflow` | Отправка типизированных сообщений в RabbitMQ. |
| **Agent Messages** | `AgentMessagesController.cs` | `POST /api/agent-messages/{agentId}` | Отправка сообщений конкретному агенту через routing key `agent.{id}`. |
| **Tech Process** | `TechProcessController.cs` | `POST /api/tech-process/attach`, `POST /api/tech-process/start/{agentId}`, `GET /api/tech-process/agent/{agentId}` | Парсинг XLSX, создание агентов и папок ТП, управление `PendingMessage` (draft → sent). |
| **Parameter Dictionary** | `ParameterDictionaryController.cs` | `GET /api/parameter-dictionary`, `POST /api/parameter-dictionary`, `PUT /api/parameter-dictionary/{id}`, `DELETE /api/parameter-dictionary/{id}` | CRUD центрального справочника параметров. |
| **OKPD-2** | `Okpd2DictionaryController.cs` | `GET /api/okpd2-dictionary`, `POST /api/okpd2-dictionary` | CRUD полного классификатора ОКПД-2. |
| **Blank OKPD-2** | `BlankOkpd2Controller.cs` | `GET /api/blank-okpd2/find-by-name`, `POST /api/blank-okpd2/bulk-upsert`, `GET /api/blank-okpd2/all` | Поиск ОКПД-2 по названию заготовки, массовая вставка маппингов. |

---

### 1.2. Модели (`Models/`)

| Модель | Таблица | Связи | Описание |
|--------|---------|-------|----------|
| `TreeNode` | `tree_nodes` | Self-referencing (`ParentId`) | Узлы дерева: `root`, `branch`, `agent` |
| `Agent` | `agents` | `→ TreeNode`, `→ TechProcess`, `→ PendingMessages`, `→ Parameters` | Производственный ресурс (агент) |
| `AgentParameter` | `agent_parameters` | `→ Agent` | Конкретные значения параметров агента |
| `ParameterDictionary` | `parameter_dictionary` | — | Справочник возможных параметров (уникальное имя) |
| `WorkflowTemplate` | `workflow_templates` | — | Шаблоны workflow (`Nodes`, `Edges` JSONB) |
| `WorkflowFunction` | `workflow_functions` | — | Метаданные вызываемых функций |
| `WFList` | `wf_list` | `→ Agent` | Запущенные экземпляры workflow |
| `TechProcess` | `tech_processes` | `→ TechProcessOperation` | Импортированные техпроцессы |
| `TechProcessOperation` | `tech_process_operations` | `→ TechProcess` | Операции техпроцесса |
| `PendingMessage` | `pending_messages` | `→ Agent` | Черновики сообщений (`draft` / `sent` / `failed`) |
| `User` | `users` | — | Пользователи (AES + BCrypt) |
| `Timetable` | `timetable` | — | Расписание работ |
| `BlankOkpd2Mapping` | `blank_okpd2_mapping` | — | Маппинг названий заготовок на ОКПД-2 |
| `Okpd2Dictionary` | `okpd2_dictionary` | Self-referencing (`ParentCode`) | Иерархический классификатор ОКПД-2 |

---

### 1.3. Сервисы (`Services/`)

#### 1.3.1. Workflow Engine (`Services/Workflow/`)

| Сервис | Назначение |
|--------|-----------|
| `WorkflowExecutionService` | Ядро исполнения workflow: загрузка шаблона, валидация, конвертация графа, запуск шагов через `Task.Run` |
| `ParameterValidationService` | Проверка входных параметров функций на соответствие типам |
| `WorkflowFormatConverter` | Преобразование React Flow (`nodes` + `edges`) в топологически отсортированный список функций (алгоритм Кана) |
| `WorkflowFunctionWhitelist` | Статический whitelist разрешённых классов (`ExampleWorkflowFunctions`, `RabbitMQFunctions`, …) — защита от RCE |

#### 1.3.2. Workflow Functions (`Services/WorkflowFunctions/`)

| Класс | Функций | Назначение |
|-------|---------|------------|
| `RabbitMQFunctions` | 4 | Отправка сообщений агентам, broadcast по ОКПД-2 |
| `AgentInfoFunctions` | 5 | Чтение/обновление статуса и параметров агентов |
| `TimeTableFunctions` | 5 | CRUD расписания (`CreateScheduleEntry`, `GetScheduleByOrder`, …) |
| `WFListFunctions` | 6 | Управление записями workflow (`CreateWorkflow`, `CompleteWorkflow`, …) |
| `TechProcessFunctions` | 9 | Работа с техпроцессами (`GetOperationByNumber`, `CalculateTotalTime`, …) |
| `WorkflowControlFunctions` | 9 | Управление выполнением (`WorkFlowSleep`, `WorkFlowStop`, `WorkFlowContinue`, …) |
| `PlanningFunctions` | 8 | Полный цикл планирования (`TechprocessPrepare`, `PrepareMessagesToAgentAboutJob`, …) |
| `ExampleWorkflowFunctions` | 9 | Тестовые и вспомогательные функции (`SayHello`, `Add`, `Multiply`, `LogMessage`, …) |

#### 1.3.3. RabbitMQ (`Services/RabbitMQ/`)

| Сервис | Назначение |
|--------|-----------|
| `RabbitMQService` | Управление соединением, публикация сообщений в exchange `topic_logs` |
| `RabbitMQHostedService` | Фоновый `IHostedService` для прослушивания очереди `backend_queue` |
| `MessageProcessingService` | Диспетчеризация входящих сообщений по типам (`3-6-1-1`, `3-6-1-2`, остальные) |

#### 1.3.4. Прочие сервисы

| Сервис | Назначение |
|--------|-----------|
| `TechProcessXlsxParser` | Парсинг Excel-файлов через `ExcelDataReader`, поиск русскоязычных заголовков |
| `EncryptionService` | AES-256 CBC шифрование/расшифровка (пароли, чувствительные данные) |
| `PasswordHashService` | BCrypt-хеширование (work factor 12) |

---

### 1.4. Репозитории (`Repositories/`)

| Интерфейс | Реализация | Описание |
|-----------|------------|----------|
| `IWFListRepository` | `WFListRepository` | CRUD + поиск по `AgentId`, `Status`, обновление `Step`/`Parameters` |
| `ITimetableRepository` | `TimetableRepository` | CRUD + поиск по агенту, статусу, диапазону дат |
| `IWorkflowTemplateRepository` | `WorkflowTemplateRepository` | CRUD + поиск по имени |

---

### 1.5. Промежуточное ПО (`Middleware/`)

| Модуль | Назначение |
|--------|-----------|
| `GlobalExceptionHandlerMiddleware` | Перехват всех необработанных исключений, возврат JSON с `message`, `error`, `timestamp` и статусом 500 |

---

## 2. Frontend-модули

### 2.1. Компоненты (`src/components/`)

| Компонент | Строк | Назначение |
|-----------|-------|------------|
| `Sidebar.jsx` | ~307 | Навигационное дерево: поиск, drag-and-drop в режиме редактирования, добавление веток, счётчик агентов |
| `Header.jsx` | ~68 | Шапка: логотип, имя пользователя, роль, настройки, выход |
| `LoginPage.jsx` | ~76 | Форма авторизации, хранение `currentUser` в `localStorage` |
| `MainPage.jsx` | ~286 | Главная страница: drag-and-drop загрузка XLSX, статистика, история загрузок |
| `BranchPage.jsx` | ~541 | Карточка ветки: вкладки «Инфо», «Рабочий процесс», «Параметры», создание агентов с автопоиском ОКПД-2 |
| `AgentPage.jsx` | ~656 | Карточка агента: редактирование параметров, workflow, скачивание JSON, клонирование |
| `UsersPage.jsx` | ~203 | Управление пользователями (только `admin`): таблица, CRUD, модальное окно |
| `WorkflowTemplateEditor.jsx` | ~514 | Визуальный редактор workflow на React Flow: палитра функций, сохранение/загрузка шаблонов |
| `WorkflowMonitor.jsx` | ~265 | Мониторинг: автообновление каждые 3 сек, фильтры по статусам, продолжение после ошибок |
| `AgentCreationModal.jsx` | ~1290 | Массовое создание агентов из XLSX: парсинг листов, определение типа, автопоиск ОКПД-2 |
| `SettingsModal.jsx` | ~467 | Настройки: словарь параметров (CRUD с типами) + маппинг ОКПД-2 (только admin) |
| `AddParametersModal.jsx` | ~125 | Модалка выбора параметров из словаря для агента/ветки |
| `FunctionEditorModal.jsx` | ~270 | Создание/редактирование функции workflow (класс, метод, параметры, иконка) |
| `FunctionInfoModal.jsx` | ~93 | Просмотр информации о функции (двойной клик в редакторе) |
| `NodeConfigModal.jsx` | ~91 | Конфигурация узла React Flow (название, описание, JSON-конфиг) |

### 2.2. Кастомные узлы React Flow (`src/components/nodes/`)

| Узел | Тип | Визуал |
|------|-----|--------|
| `FunctionNode` | `function` | ⚙️ Зелёная рамка |
| `WFNode` | `wf` | 📦 Оранжевая рамка |
| `SubWFNode` | `subwf` | 🔗 Фиолетовая рамка |

### 2.3. API-слой (`src/services/` + `src/api/`)

| Файл | Клиент | Описание |
|------|--------|----------|
| `services/api.js` | `fetch` | Центральный API: `treeAPI`, `agentAPI`, `workflowTemplateAPI`, `workflowAPI`, `messagesAPI`, `parameterDictionaryAPI`, `blankOkpd2API`, `fileUploadAPI`, `statisticsAPI`, `workflowFunctionsAPI` |
| `api/userAPI.js` | `axios` | Аутентификация и управление пользователями (наследие ранней версии) |

---

## 3. Инфраструктурные модули

### 3.1. Docker

| Файл | Назначение |
|------|-----------|
| `docker-compose.yml` | Оркестрация 4 сервисов: postgres, rabbitmq, backend, frontend |
| `Backend/Dockerfile` | Многоэтапная сборка .NET 8 (sdk → aspnet), порт 3001 |
| `Dockerfile.frontend` | Многоэтапная сборка Node → Nginx alpine, SPA-роутинг |
| `nginx.conf` | Проксирование `/api` на backend, `try_files` для React Router, gzip, security headers |

### 3.2. CI/CD

| Файл | Назначение |
|------|-----------|
| `.github/workflows/` | GitHub Actions (пайплайны сборки/тестирования) |
| `.gitlab-ci.yml` | GitLab CI (сборка Docker-образа, деплой через Docker Swarm) |

### 3.3. Скрипты и миграции

| Файл/Папка | Назначение |
|------------|-----------|
| `schema.sql` | Полная схема БД v3.4: 13+ таблиц, триггеры, индексы, начальные данные |
| `Backend/Migrations/` | EF Core миграции (Code-First) |
| `Backend/sql/` | Ручные SQL-скрипты для исправления данных ОКПД-2 |
| `Backend/Scripts/` | C#-скрипты миграции паролей, генерации хешей |
| `scripts/` | Python/bash скрипты массового назначения ОКПД-2 |
