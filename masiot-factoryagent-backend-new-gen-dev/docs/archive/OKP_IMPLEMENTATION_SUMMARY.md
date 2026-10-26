# Резюме изменений - Интеграция ОКП API

## Выполненные изменения

### Backend (C# .NET 8)

#### Новые файлы:
1. **Backend/Services/OKP/IOkpApiService.cs** - интерфейс сервиса ОКП API
2. **Backend/Services/OKP/OkpApiService.cs** - реализация сервиса с кэшированием
3. **Backend/Services/BackgroundServices/OkpCacheRefreshService.cs** - фоновый сервис обновления кэша
4. **Backend/Models/OkpCacheEntry.cs** - модель для кэша ОКП
5. **Backend/Controllers/OkpApiController.cs** - REST API контроллер

#### Изменённые файлы:
1. **Backend/Data/ApplicationDbContext.cs**:
   - Добавлен DbSet<OkpCacheEntry> OkpCache
   - Добавлена конфигурация индексов для okp_cache
   - Добавлено обновление timestamps для OkpCacheEntry

2. **Backend/Program.cs**:
   - Зарегистрирован HttpClient для OkpApi
   - Зарегистрирован IOkpApiService
   - Зарегистрирован OkpCacheRefreshService как HostedService

3. **Backend/appsettings.Example.json**:
   - Добавлена секция OkpApi с BaseUrl и TimeoutSeconds

#### Миграция БД:
- **migrations/add_okp_cache_table.sql** - создание таблицы okp_cache с индексами

### Frontend (React 18)

#### Изменённые файлы:

1. **src/services/api.js**:
   - Добавлен okpAPI с методами:
     - searchByName(name)
     - getByCode(code)
     - refreshCache()
     - getCacheStats()

2. **src/components/BranchPage.jsx**:
   - Изменён импорт: okpAPI вместо okpDictionaryAPI
   - Добавлено состояние searchingOkp
   - Добавлена функция searchOkpByName() для автопоиска
   - Добавлен useEffect для автоматического поиска ОКП при вводе названия (задержка 500мс)
   - Изменён UI поля ОКП:
     - Показывает "Поиск ОКП..." во время поиска
     - Показывает ошибку если ОКП не найден
     - Read-only input если найден 1 результат
     - Select dropdown если найдено несколько
     - Placeholder если название не введено

3. **src/components/SettingsModal.jsx**:
   - Изменён импорт: okpAPI вместо okpDictionaryAPI
   - Удалены состояния для ручного создания ОКП
   - Добавлены состояния: okpCacheStats, refreshingCache
   - Добавлены функции: loadOkpCacheStats(), handleRefreshCache()
   - Изменён UI:
     - Вкладка "Настройка ОКП" удалена для обычных пользователей
     - Для админов: вкладка "Кэш ОКП (Администратор)" со статистикой
     - Кнопка "Обновить кэш вручную" для админов
   - Добавлен проп currentUser

4. **src/components/Header.jsx**:
   - Добавлена передача currentUser в SettingsModal

5. **src/components/AgentCreationModal.jsx**:
   - Добавлен импорт okpAPI
   - Изменена функция parseXLSX:
     - Добавлен автопоиск ОКП через API если название заполнено
     - Сохранение найденного okpCode в объекте агента
   - Изменена функция handleCreateAgentsFromAllSheets:
     - Использование agent.okpCode если доступен

### Документация

1. **OKP_API_INTEGRATION_GUIDE.md** - полное руководство по интеграции
2. **MIGRATION_OKP_CACHE.md** - инструкция по миграции БД

## Логика работы

### Создание агента (BranchPage):
1. Пользователь вводит название оборудования
2. Через 500мс после окончания ввода отправляется запрос к API
3. Backend ищет в кэше, если не найдено - запрашивает внешний API
4. Если найден 1 ОКП - автоматически выбирается и поле блокируется
5. Если найдено несколько - пользователь выбирает из списка
6. Если не найдено - показывается предупреждение

### Импорт из XLSX (AgentCreationModal):
1. При парсинге XLSX для каждого агента проверяется наличие ОКП в колонке
2. Если ОКП пустой, но есть название - автоматически ищется через API
3. Найденный ОКП сохраняется в agent.okpCode
4. При создании используется agent.okpCode или значение из колонки

### Кэширование:
1. Все запросы к API кэшируются в таблице okp_cache
2. Время жизни кэша: 24 часа
3. Автоматическое обновление: раз в сутки (OkpCacheRefreshService)
4. Ручное обновление: через админ-панель

### Для администраторов:
1. Настройки → Кэш ОКП (Администратор)
2. Просмотр статистики: количество записей, последнее обновление
3. Кнопка "Обновить кэш вручную"

## API Endpoints

### Backend REST API:
- `GET /api/okp/search?name={name}` - поиск ОКП по названию
- `GET /api/okp/code/{code}` - получение ОКП по коду
- `POST /api/okp/refresh-cache` - обновление кэша (админ)
- `GET /api/okp/cache-stats` - статистика кэша

## Требования к внешнему API

Backend ожидает следующий формат ответов от внешнего API ОКП:

### Поиск по названию: GET {BaseUrl}/search?name={name}
```json
[
  {
    "code": "28.41.22.130",
    "name": "Станки токарные с ЧПУ",
    "description": "Описание из классификатора"
  }
]
```

### Получение по коду: GET {BaseUrl}/code/{code}
```json
{
  "code": "28.41.22.130",
  "name": "Станки токарные с ЧПУ",
  "description": "Описание из классификатора"
}
```

## Шаги для запуска

1. **Выполнить миграцию БД:**
   ```bash
   psql -U postgres -d agenti_db -f migrations/add_okp_cache_table.sql
   ```

2. **Настроить appsettings.json:**
   ```json
   {
     "OkpApi": {
       "BaseUrl": "https://api.okp.example.ru/v1",
       "TimeoutSeconds": 30
     }
   }
   ```

3. **Перезапустить Backend:**
   ```bash
   cd Backend
   dotnet restore
   dotnet run
   ```

4. **Перезапустить Frontend:**
   ```bash
   npm install
   npm run dev
   ```

## Изменения в поведении

### Что изменилось для пользователей:
- ❌ Больше нельзя вручную создавать ОКП через настройки
- ✅ ОКП автоматически подбирается при вводе названия оборудования
- ✅ Поле ОКП становится read-only после автоподбора
- ✅ При импорте XLSX ОКП автоматически ищется для агентов без кода

### Что изменилось для администраторов:
- ✅ Новая вкладка "Кэш ОКП" в настройках
- ✅ Просмотр статистики кэша
- ✅ Ручное обновление кэша
- ❌ Удалена возможность ручного создания ОКП

## Обратная совместимость

- Старые записи в таблице `okp_dictionary` НЕ удаляются
- Они просто не отображаются в UI
- Агенты с существующими ОКП продолжают работать
- При необходимости можно вернуться к старой системе

## Тестирование

### Проверка Backend:
```bash
# Проверка здоровья API
curl http://localhost:3001/api/health

# Поиск ОКП
curl "http://localhost:3001/api/okp/search?name=Токарный%20станок"

# Статистика кэша
curl http://localhost:3001/api/okp/cache-stats
```

### Проверка Frontend:
1. Открыть http://localhost:5173
2. Войти в систему
3. Перейти в любую папку
4. Начать вводить название оборудования в поле "Имя агента"
5. Проверить автоматический поиск ОКП
6. Открыть Настройки → проверить вкладки (для админа должна быть "Кэш ОКП")

## Известные ограничения

1. **Требуется настройка URL API** - без реального API система будет использовать только кэш
2. **Первый запуск** - кэш пустой, нужно дождаться первого обновления или обновить вручную
3. **Офлайн режим** - если API недоступен, используются данные из кэша (могут быть устаревшими)

## Поддержка

При возникновении проблем:
1. Проверить логи Backend: `Backend/logs/backend-*.log`
2. Проверить консоль браузера (F12)
3. Проверить таблицу кэша: `SELECT * FROM okp_cache LIMIT 10;`
4. Проверить конфигурацию: `Backend/appsettings.json`
