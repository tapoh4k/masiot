# Описание модулей

**Продукт:** MAS-IOT  
**Версия:** 1.0.0

---

## Backend модули

### Controllers (API слой)

| Модуль | Путь | Назначение |
|--------|------|------------|
| UsersController | `api/users` | Аутентификация, управление пользователями |
| TreeController | `api/tree` | CRUD операции с иерархическим деревом |
| AgentsController | `api/agents` | Управление агентами, импорт из Excel |
| WorkflowController | `api/workflow` | Запуск и управление workflow-процессами |
| WorkflowTemplatesController | `api/workflow-templates` | Шаблоны workflow |
| WorkflowFunctionsController | `api/workflowfunctions` | Каталог функций workflow |
| MessagesController | `api/messages` | Отправка сообщений через RabbitMQ |
| TechProcessController | `api/tech-process` | Импорт и управление техпроцессами |
| BlankOkpd2Controller | `api/blank-okpd2` | Маппинг заготовок на ОКПД-2 |
| Okpd2DictionaryController | `api/okpd2-dictionary` | Справочник ОКПД-2 |
| ParameterDictionaryController | `api/parameter-dictionary` | Словарь параметров |
| MonitoringController | `api/monitoring` | Системный мониторинг (API Key) |

### Services (Бизнес-логика)

| Модуль | Назначение |
|--------|------------|
| EncryptionService | AES-256 шифрование/дешифрование |
| PasswordHashService | BCrypt хеширование паролей |
| RabbitMQService | Публикация и потребление сообщений |
| RabbitMQMessageEncryptionService | Сквозное шифрование RabbitMQ |
| MessageProcessingService | Обработка входящих сообщений |
| WorkflowExecutionService | Исполнитель workflow-процессов |
| WorkflowFunctionWhitelist | Белый список разрешённых функций |

### Data (Доступ к данным)

| Модуль | Назначение |
|--------|------------|
| ApplicationDbContext | EF Core DbContext, маппинг всех сущностей |

## Frontend модули

| Модуль | Назначение |
|--------|------------|
| App.jsx | Корневой компонент, роутинг |
| Sidebar.jsx | Боковая панель с деревом |
| MainPage.jsx | Главная страница |
| AgentPage.jsx | Страница агента |
| BranchPage.jsx | Страница папки |
| WorkflowTemplateEditor.jsx | Визуальный редактор workflow |
| FunctionEditorModal.jsx | Редактор функций |
| AgentCreationModal.jsx | Импорт агентов из Excel |
| LoginPage.jsx | Страница входа |
| UsersPage.jsx | Управление пользователями |

## Database модули

| Таблица | Назначение |
|---------|------------|
| tree_nodes | Иерархическое дерево |
| agents | Производственные агенты |
| agent_parameters | Параметры агентов |
| wf_list | Выполняемые workflow |
| workflow_functions | Каталог функций |
| workflow_templates | Шаблоны workflow |
| okpd2_dictionary | Классификатор ОКПД-2 |
| blank_okpd2_mapping | Маппинг заготовок |
| tech_processes | Технологические процессы |
| timetable | Расписание работ |
| users | Учётные записи |
| statistics | Статистика системы |
| file_uploads | История загрузок |
