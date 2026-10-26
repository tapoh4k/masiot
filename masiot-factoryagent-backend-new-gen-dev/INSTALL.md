# Factory Agent System - Полная инструкция

## 📋 Содержание

1. [Быстрый старт](#быстрый-старт) - для опытных разработчиков
2. [Подробная установка](#подробная-установка) - пошаговая инструкция
3. [Устранение проблем](#устранение-проблем) - решение типовых ошибок
4. [Структура проекта](#структура-проекта)
5. [API документация](#api-документация)

---

## 🚀 Быстрый старт

Для опытных разработчиков:

```bash
# 1. Установите зависимости
# PostgreSQL 14+, .NET 8 SDK, Node.js 18+, RabbitMQ (опционально)

# 2. Создайте БД
psql -U postgres -c "CREATE DATABASE agenti_db;"
psql -U postgres -d agenti_db -f schema.sql

# 3. Настройте Backend
cd Backend
cp appsettings.Example.json appsettings.json
# Отредактируйте appsettings.json - укажите пароли PostgreSQL и RabbitMQ

# 4. Запустите
dotnet restore && dotnet run  # Backend на :3001

# В новом терминале:
npm install && npm run dev     # Frontend на :5173
```

**Готово!** Откройте http://localhost:5173

**Примечание:** RabbitMQ нужен только для обмена сообщениями между агентами. Без него система работает, но кнопка Send будет неактивна.

---

## 📖 Подробная установка

### Шаг 1: Установка зависимостей

#### PostgreSQL 14+

**Windows:**
1. Скачайте: https://www.postgresql.org/download/windows/
2. Запустите установщик
3. **Запомните пароль** для пользователя postgres!
4. Порт: 5432 (по умолчанию)

**Проверка:**
```bash
psql --version
Get-Service -Name postgresql*  # Должен быть Running
```

#### .NET 8 SDK

**Windows:**
1. Скачайте: https://dotnet.microsoft.com/download/dotnet/8.0
2. Запустите установщик
3. Перезапустите терминал

**Проверка:**
```bash
dotnet --version  # Должно быть 8.0.x
```

#### Node.js 18+

**Windows:**
1. Скачайте: https://nodejs.org/
2. Запустите установщик

**Проверка:**
```bash
node --version  # Должно быть 18.x+
npm --version
```

#### RabbitMQ (опционально)

**Для чего нужен:** Обмен сообщениями между агентами. Без RabbitMQ система работает, но функция отправки сообщений будет недоступна.

**Windows:**
1. Установите Erlang: https://www.erlang.org/downloads
2. Скачайте RabbitMQ: https://www.rabbitmq.com/download.html
3. Запустите установщик RabbitMQ
4. RabbitMQ запустится автоматически как служба Windows

**Проверка:**
```bash
# Проверка службы
Get-Service -Name RabbitMQ

# Management UI (опционально)
rabbitmq-plugins enable rabbitmq_management
# Откройте http://localhost:15672 (guest/guest)
```

**Linux/Mac:**
```bash
# Ubuntu/Debian
sudo apt-get install rabbitmq-server
sudo systemctl start rabbitmq-server

# Mac
brew install rabbitmq
brew services start rabbitmq
```

---

### Шаг 2: Создание базы данных

```bash
# Подключитесь к PostgreSQL
psql -U postgres
```

Введите пароль, затем:

```sql
CREATE DATABASE agenti_db;
\q
```

**Выполните SQL-скрипт:**

```bash
cd c:\project\agenti
psql -U postgres -d agenti_db -f schema.sql
```

**Вы должны увидеть:**
```
✓ База данных создана успешно!
Создано таблиц: 9
```

**Проверка:**
```bash
psql -U postgres -d agenti_db -c "\dt"
```

Должно быть **9 таблиц:**
- tree_nodes
- agents
- agent_parameters
- file_uploads
- statistics
- parameter_dictionary
- okp_dictionary
- workflow_templates
- users

---

### Шаг 3: Настройка Backend

**Создайте файл конфигурации:**

```bash
cd Backend
copy appsettings.Example.json appsettings.json  # Windows
# или
cp appsettings.Example.json appsettings.json    # Linux/Mac
```

**Откройте `Backend/appsettings.json` и замените пароль:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=agenti_db;Username=postgres;Password=ВАШ_ПАРОЛЬ"
  },
  "Port": 3001
}
```

**Запустите Backend:**

```bash
cd Backend
dotnet restore
dotnet run
```

**Должно появиться:**
```
[INF] Starting Factory Agent Backend on port 3001
[INF] Now listening on: http://localhost:3001
```

**Проверка:** Откройте http://localhost:3001/api/tree в браузере

---

### Шаг 4: Запуск Frontend

**В новом терминале:**

```bash
cd c:\project\agenti
npm install
npm run dev
```

**Должно появиться:**
```
VITE v5.x.x  ready in xxx ms
➜  Local:   http://localhost:5173/
```

**Откройте:** http://localhost:5173

---

## ✅ Проверка работоспособности

### Тест 1: Создание папки
1. Нажмите "Создать папку"
2. Введите название
3. Папка появится в дереве слева

### Тест 2: Создание агентов из XLSX
1. Нажмите "Создать агентов из XLSX"
2. Загрузите Excel файл
3. Нажмите "Создать агентов с текущей страницы"
4. **Все агенты должны создаться** (не только первый!)

### Тест 3: Workflow Templates
1. Откройте "Workflow Templates"
2. Создайте новый шаблон
3. Редактор должен открыться без ошибок

**Если все 3 теста прошли - всё работает! 🎉**

---

## 🔧 Устранение проблем

### ❌ Агенты не создаются (только 1 из 50)

**Причина:** UNIQUE constraint на node_id

**Решение:** Пересоздайте БД с актуальным schema.sql:

```bash
# Удалите старую БД
psql -U postgres -c "DROP DATABASE agenti_db;"
psql -U postgres -c "CREATE DATABASE agenti_db;"

# Создайте новую с правильной схемой
psql -U postgres -d agenti_db -f schema.sql
```

Новый schema.sql **автоматически удаляет** UNIQUE constraint.

---

### ❌ Backend не запускается

**Проверьте:**

1. **PostgreSQL запущен:**
   ```bash
   Get-Service -Name postgresql*  # Windows
   ```

2. **Пароль правильный** в `Backend/appsettings.json`

3. **База данных создана:**
   ```bash
   psql -U postgres -l | findstr agenti_db
   ```

4. **Порт 3001 свободен:**
   ```bash
   netstat -ano | findstr :3001
   ```

**Логи:** Проверьте `Backend/logs/backend-YYYYMMDD.log`

---

### ❌ CORS ошибки

**Симптомы:**
```
Access to fetch at 'http://localhost:3001/api/...' from origin 'http://localhost:XXXX'
has been blocked by CORS policy
```

**Решение:**

1. Остановите Frontend (Ctrl+C)
2. Остановите Backend (Ctrl+C)
3. Перезапустите Backend: `cd Backend && dotnet run`
4. Перезапустите Frontend: `npm run dev`
5. Frontend должен запуститься на **порту 5173**

---

### ❌ Workflow Templates не работают

**Причина:** Таблица отсутствует (старая версия schema.sql)

**Решение:** Используйте актуальный schema.sql (версия 3.0), который автоматически создаёт все таблицы.

---

### ❌ Ошибка "Backend.exe is being used by another process"

**Причина:** Backend уже запущен

**Решение:**

```powershell
# Найдите процесс
Get-Process -Name Backend

# Завершите его
Stop-Process -Name Backend -Force

# Запустите заново
cd Backend
dotnet run
```

---

## 📁 Структура проекта

```
agenti/
├── Backend/                    # C# ASP.NET Core 8
│   ├── Controllers/           # API контроллеры
│   ├── Models/               # Модели данных
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Data/                 # DbContext
│   ├── Middleware/           # Middleware
│   ├── logs/                 # Логи (создаётся автоматически)
│   ├── appsettings.json      # Конфигурация (создать из Example!)
│   ├── appsettings.Example.json  # Шаблон конфигурации
│   └── Program.cs            # Точка входа
│
├── src/                       # React Frontend
│   ├── components/           # React компоненты
│   │   ├── AgentCreationModal.jsx  # Создание агентов из XLSX
│   │   ├── AgentPage.jsx          # Страница агента
│   │   ├── BranchPage.jsx         # Страница папки
│   │   └── ...
│   ├── services/
│   │   └── api.js            # API клиент
│   ├── App.jsx
│   └── main.jsx
│
├── schema.sql                 # SQL-скрипт БД (всё включено!)
├── package.json              # Frontend зависимости
├── vite.config.js            # Vite конфигурация
├── .gitignore                # Git исключения
└── INSTALL.md                # Этот файл
```

---

## 🌐 API Документация

### Tree (Дерево узлов)
- `GET /api/tree` - получить все узлы
- `POST /api/tree` - создать узел (папку)
- `PUT /api/tree/{id}` - обновить узел
- `DELETE /api/tree/{id}` - удалить узел (каскадное удаление)

### Agents (Агенты)
- `GET /api/agents` - получить всех агентов
- `GET /api/agents/{nodeId}` - получить агентов по nodeId
- `POST /api/agents` - создать агента
- `PUT /api/agents/{id}` - обновить агента
- `DELETE /api/agents/{id}` - удалить агента

### File Uploads
- `GET /api/file-uploads` - история загрузок
- `POST /api/file-uploads` - создать запись о загрузке

### Statistics
- `GET /api/statistics` - получить статистику

### Parameter Dictionary
- `GET /api/parameter-dictionary` - все параметры
- `POST /api/parameter-dictionary` - создать параметр
- `PUT /api/parameter-dictionary/{id}` - обновить
- `DELETE /api/parameter-dictionary/{id}` - удалить

### Workflow Templates
- `GET /api/workflow-templates` - все шаблоны workflow
- `GET /api/workflow-templates/{id}` - получить шаблон
- `POST /api/workflow-templates` - создать шаблон
- `PUT /api/workflow-templates/{id}` - обновить шаблон
- `DELETE /api/workflow-templates/{id}` - удалить шаблон

### Workflow Execution (требует RabbitMQ)
- `POST /api/workflow/start` - запустить workflow
  ```json
  {
    "wf_type": "workflow_name",
    "agent_id": "agent_node_id",
    "parameters": {},
    "initiator": "system"
  }
  ```
- `POST /api/workflow/continue/{id}` - продолжить workflow с шага
- `GET /api/workflow/{id}` - получить workflow по ID
- `GET /api/workflow/agent/{agentId}` - все workflows агента
- `GET /api/workflow/status/{status}` - workflows по статусу
- `GET /api/workflow` - все workflows
- `PATCH /api/workflow/{id}/step` - обновить шаг
- `PATCH /api/workflow/{id}/status` - обновить статус
- `DELETE /api/workflow/{id}` - удалить workflow

### Messages (требует RabbitMQ)
- `POST /api/messages/work` - отправить рабочее сообщение
  ```json
  {
    "message_type": "3-6-1-3",
    "sender_id": "agent1",
    "receiver_id": "agent2",
    "parameter": {}
  }
  ```
- `POST /api/messages/agent-info` - отправить информацию об агенте
- `POST /api/messages/new-workflow` - отправить новый workflow

---

## 🔄 Система Workflow

### Как работает Workflow система

Система workflow позволяет агентам выполнять последовательности задач и обмениваться сообщениями через RabbitMQ.

**Основные компоненты:**

1. **Workflow Templates** - шаблоны workflow, создаваемые в визуальном редакторе
2. **Workflow Execution** - выполнение workflow на агентах
3. **RabbitMQ Messages** - обмен сообщениями между агентами
4. **WF List** - список активных workflow с отслеживанием шагов

### Использование Workflow

#### 1. Создание Workflow Template

1. Откройте "Workflow Templates" в меню
2. Нажмите "Создать новый workflow"
3. Используйте визуальный редактор для создания узлов:
   - **Function** - обычная функция
   - **WF** - workflow функция
   - **SUB-WF** - под-workflow
4. Соедините узлы стрелками для определения последовательности
5. Сохраните шаблон

#### 2. Назначение Workflow агенту

1. Откройте страницу агента
2. Перейдите на вкладку "Настройка Workflow"
3. Выберите нужные workflow из списка (checkbox)
4. Нажмите "Отправить в RabbitMQ"
5. Workflow запустятся на агенте

#### 3. Отслеживание выполнения

Используйте API для мониторинга:

```bash
# Получить все workflows агента
GET /api/workflow/agent/{agentId}

# Получить статус конкретного workflow
GET /api/workflow/{id}

# Статусы:
# 0 - В ожидании
# 1 - Выполняется
# 2 - Завершен
# 3 - Ошибка
```

### Типы сообщений RabbitMQ

- **3-6-1-1** - Новое определение workflow
- **3-6-1-2** - Обновление информации об агенте
- **Другие** - Рабочие сообщения, запускающие workflow

### Архитектура

```
Frontend (React)
    ↓ HTTP API
Backend (ASP.NET Core)
    ↓ RabbitMQ
Agents (Workflow Execution)
    ↓ Database
PostgreSQL (State Storage)
```

**Поток выполнения:**

1. Пользователь выбирает workflow для агента
2. Frontend отправляет POST /api/workflow/start
3. Backend создает запись в wf_list
4. WorkflowExecutionService выполняет функции по порядку
5. Каждый шаг сохраняется в БД
6. При ошибке workflow можно продолжить с последнего шага

---

### OKP Dictionary
- `GET /api/okp-dictionary` - все ОКП
- `POST /api/okp-dictionary` - создать ОКП
- `PUT /api/okp-dictionary/{id}` - обновить
- `DELETE /api/okp-dictionary/{id}` - удалить

### Workflow Templates
- `GET /api/workflow-templates` - все шаблоны
- `POST /api/workflow-templates` - создать шаблон
- `PUT /api/workflow-templates/{id}` - обновить
- `DELETE /api/workflow-templates/{id}` - удалить

---

## 💡 Полезные команды

### PostgreSQL

```bash
# Подключиться к БД
psql -U postgres -d agenti_db

# Список таблиц
\dt

# Описание таблицы
\d agents

# Количество агентов
SELECT COUNT(*) FROM agents;

# Выйти
\q
```

### Backend

```bash
# Восстановить зависимости
dotnet restore

# Запустить
dotnet run

# Собрать
dotnet build

# Очистить
dotnet clean
```

### Frontend

```bash
# Установить зависимости
npm install

# Запустить dev сервер
npm run dev

# Собрать для production
npm run build
```

---

## 📞 Поддержка

Если возникли проблемы:

1. Проверьте раздел [Устранение проблем](#устранение-проблем)
2. Проверьте логи Backend: `Backend/logs/`
3. Проверьте консоль браузера (F12)
4. Убедитесь что используете актуальный `schema.sql` (версия 3.0)

---

## 🎯 Требования

- **ОС:** Windows 10/11, Linux, macOS
- **RAM:** 4GB минимум
- **Диск:** 2GB свободного места
- **PostgreSQL:** 14.0+
- **.NET SDK:** 8.0+
- **Node.js:** 18.0+

---

**Версия:** 3.0 (финальная)
**Дата:** 27.02.2026
**Статус:** Готово к production ✅
