# 🚀 Быстрый старт Workflow системы

## Что уже готово

✅ Backend с RabbitMQ интеграцией
✅ Workflow execution engine
✅ 8 тестовых функций
✅ 3 готовых примера workflow
✅ UI для мониторинга
✅ API endpoints
✅ **🎨 Визуальный редактор workflow с drag-and-drop**
✅ **💾 Сохранение шаблонов в PostgreSQL**

## Запуск за 5 минут

### Шаг 1: Запуск Backend

```bash
cd Backend
dotnet run
```

Backend запустится на `http://localhost:3001`

### Шаг 2: Запуск Frontend

```bash
npm start
```

Frontend запустится на `http://localhost:3000`

### Шаг 3: Создайте workflow визуально (НОВОЕ! 🎨)

**Больше не нужно писать SQL!** Используйте визуальный редактор:

1. Откройте `http://localhost:3000`
2. Войдите в систему
3. Нажмите **🎨 Редактор шаблонов** в левом меню
4. Перетащите функции из левой панели на холст
5. Соедините узлы стрелками (кликните на край узла и протяните)
6. Нажмите "💾 Сохранить", введите название и описание
7. Готово! Workflow сохранен в базу данных

**Альтернатива:** Загрузить готовые примеры через SQL

```bash
psql -U postgres -d factory_agent -f test-workflows.sql
```

Это создаст 3 готовых примера:
- **Тест калькулятор** - складывает и умножает числа
- **Приветствие** - простое приветствие с логированием
- **Обработка детали** - симуляция производственного процесса

### Шаг 4: Тестирование через UI

1. Откройте `http://localhost:3000`
2. Перейдите на страницу любого агента
3. В разделе "Workflows" выберите "Тест калькулятор"
4. Нажмите кнопку "Send to RabbitMQ"
5. Откройте консоль Backend - увидите выполнение функций

### Шаг 5: Мониторинг выполнения

Добавьте в `src/App.js` новый роут для WorkflowMonitor:

```javascript
import WorkflowMonitor from './components/WorkflowMonitor';

// В <Routes>
<Route path="/workflows" element={<WorkflowMonitor />} />
```

Затем откройте `http://localhost:3000/workflows` для просмотра всех запущенных workflow.

## Быстрый тест через PowerShell

```powershell
.\test-workflow-quick.ps1
```

Этот скрипт:
- Проверит здоровье бэкенда
- Запустит workflow "Приветствие"
- Запустит workflow "Тест калькулятор"
- Покажет результаты выполнения

## Быстрый тест через API (curl)

### Запуск workflow "Приветствие"

```bash
curl -X POST http://localhost:3001/api/workflow/start \
  -H "Content-Type: application/json" \
  -d '{
    "wf_type": "Приветствие",
    "agent_id": "agent-001",
    "parameters": {
      "name": "Иван"
    },
    "initiator": "manual-test"
  }'
```

Ответ:
```json
{
  "wfId": 1,
  "message": "Workflow started successfully"
}
```

### Проверка результата

```bash
curl http://localhost:3001/api/workflow/1
```

Ответ:
```json
{
  "id": 1,
  "step": 2,
  "status": 1,
  "parameters": {
    "name": "Иван",
    "greeting": "Привет, Иван!",
    "timestamp": "2026-03-01 12:34:56"
  },
  "agentId": "agent-001",
  "wfType": "Приветствие"
}
```

### Запуск workflow "Тест калькулятор"

```bash
curl -X POST http://localhost:3001/api/workflow/start \
  -H "Content-Type: application/json" \
  -d '{
    "wf_type": "Тест калькулятор",
    "agent_id": "agent-001",
    "parameters": {
      "a": 10,
      "b": 5,
      "multiplier": 3
    },
    "initiator": "manual-test"
  }'
```

Результат: `(10 + 5) * 3 = 45`

## Что происходит внутри

1. **POST /api/workflow/start** создает запись в `wf_list`
2. **WorkflowExecutionService** загружает workflow из `workflow_templates`
3. Через **Reflection** находит классы и методы функций
4. Выполняет функции последовательно, передавая параметры
5. Сохраняет результаты каждого шага в `parameters`
6. Обновляет `step` и `status` после каждой функции

## Логи выполнения

В консоли Backend вы увидите:

```
[12:34:56 INF] Executing Add: 10 + 5
Результат: 10 + 5 = 15
[12:34:56 INF] Executing Multiply: 15 * 3
Результат: 15 * 3 = 45
[12:34:56 INF] LogMessage: 45 at 2026-03-01 12:34:56
[2026-03-01 12:34:56] 45
```

## Создание своих функций

1. Создайте класс в `Backend/Services/WorkflowFunctions/`
2. Зарегистрируйте в `Program.cs`:
   ```csharp
   builder.Services.AddScoped<YourFunctionClass>();
   ```
3. Создайте workflow в БД с указанием вашего класса:
   ```json
   {
     "functionclass": "Backend.Services.WorkflowFunctions.YourFunctionClass",
     "name": "YourMethod",
     "needParam": ["param1", "param2"],
     "paramReturn": "result"
   }
   ```

## Troubleshooting

### Backend не запускается
- Проверьте PostgreSQL: `psql -U postgres -l`
- Проверьте строку подключения в `appsettings.json`

### Workflow не выполняется
- Проверьте логи Backend
- Убедитесь, что workflow существует в БД: `SELECT * FROM workflow_templates;`
- Проверьте, что функции зарегистрированы в `Program.cs`

### 500 ошибка при запуске workflow
- Проверьте, что все параметры переданы
- Проверьте типы параметров (int, string, etc.)
- Проверьте имя класса в `functionclass` (должно быть полное с namespace)

## Следующие шаги

1. ✅ Протестируйте готовые примеры
2. 📝 Создайте свои функции для вашего производства
3. 🔧 Настройте RabbitMQ для межагентного взаимодействия
4. 🎨 Добавьте визуальный редактор workflow (как n8n)
5. 📊 Расширьте мониторинг с графиками и статистикой

## Полная документация

- **WORKFLOW_GUIDE.md** - подробное руководство по workflow
- **MASIOT_ANALYSIS.md** - анализ системы прошлых разработчиков
- **INSTALL.md** - полная инструкция по установке
