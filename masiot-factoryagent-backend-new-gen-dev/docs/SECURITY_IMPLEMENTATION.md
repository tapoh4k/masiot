# Security Implementation Documentation

## Обзор системы безопасности

Реализована двухуровневая система безопасности для паролей пользователей:

### 1. Хеширование (BCrypt) - для аутентификации
- **Назначение:** Безопасная проверка пароля при входе
- **Алгоритм:** BCrypt с work factor 12
- **Поле в БД:** `password_hash`
- **Необратимо:** Невозможно восстановить оригинальный пароль

### 2. Шифрование (AES-256) - для просмотра админом
- **Назначение:** Возможность просмотра паролей администратором
- **Алгоритм:** AES-256 в режиме CBC
- **Поле в БД:** `password`
- **Обратимо:** Можно расшифровать с помощью ключа

---

## Структура базы данных

### Таблица users
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(255) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,        -- Зашифрованный пароль (AES-256)
    password_hash VARCHAR(255) NOT NULL,   -- Хеш пароля (BCrypt)
    role VARCHAR(50) DEFAULT 'user',
    full_name VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## API Endpoints

### Для основного приложения

#### 1. Вход в систему
```http
POST /api/users/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password123"
}
```

**Ответ:**
```json
{
  "id": 1,
  "username": "admin",
  "role": "admin",
  "fullName": "Администратор",
  "createdAt": "2026-03-07T00:00:00Z",
  "updatedAt": "2026-03-07T00:00:00Z"
}
```

#### 2. Получить всех пользователей (для админа)
```http
GET /api/users
```

**Ответ:**
```json
[
  {
    "id": 1,
    "username": "admin",
    "password": "password123",  // Расшифрованный пароль
    "role": "admin",
    "fullName": "Администратор",
    "createdAt": "2026-03-07T00:00:00Z",
    "updatedAt": "2026-03-07T00:00:00Z"
  }
]
```

#### 3. Создать пользователя
```http
POST /api/users
Content-Type: application/json

{
  "username": "newuser",
  "password": "securepass123",
  "role": "user",
  "fullName": "Новый Пользователь"
}
```

---

### Для системы мониторинга

**Все запросы требуют API ключ в заголовке:**
```http
X-API-Key: monitoring_secret_key_12345678
```

#### 1. Получить статистику
```http
GET /api/monitoring/statistics
X-API-Key: monitoring_secret_key_12345678
```

**Ответ:**
```json
{
  "timestamp": "2026-03-07T01:00:00Z",
  "agents": {
    "total": 414,
    "tech_process": 238,
    "equipment": 50,
    "instrument": 50,
    "personnel": 25,
    "blank": 51
  },
  "users_count": 5,
  "tree_nodes_count": 12
}
```

#### 2. Получить данные пользователей
```http
GET /api/monitoring/users
X-API-Key: monitoring_secret_key_12345678
```

**Ответ:**
```json
{
  "timestamp": "2026-03-07T01:00:00Z",
  "users": [
    {
      "id": 1,
      "username": "admin",
      "encrypted_password": "U2FsdGVkX1...",  // Зашифрованный пароль
      "role": "admin",
      "full_name": "Администратор",
      "created_at": "2026-03-07T00:00:00Z",
      "updated_at": "2026-03-07T00:00:00Z"
    }
  ]
}
```

#### 3. Получить полные данные
```http
GET /api/monitoring/full-data
X-API-Key: monitoring_secret_key_12345678
```

**Ответ:** Комбинация статистики, пользователей и последних агентов

#### 4. Расшифровать пароль
```http
POST /api/monitoring/decrypt-password
X-API-Key: monitoring_secret_key_12345678
Content-Type: application/json

{
  "encryptedPassword": "U2FsdGVkX1..."
}
```

**Ответ:**
```json
{
  "decrypted_password": "password123"
}
```

---

## Конфигурация

### appsettings.json
```json
{
  "Encryption": {
    "Key": "MySecretEncryptionKey123456789",  // 32 символа для AES-256
    "IV": "MySecretIV123456"                   // 16 символов для IV
  },
  "MonitoringApiKey": "monitoring_secret_key_12345678"
}
```

**⚠️ ВАЖНО:** В production эти ключи должны храниться в безопасном месте:
- Azure Key Vault
- AWS Secrets Manager
- Environment Variables
- Encrypted configuration files

---

## Миграция существующих пользователей

Для обновления существующих пользователей на новую систему безопасности:

### Вариант 1: Через API endpoint (рекомендуется)
```http
POST /api/users/migrate-passwords
X-API-Key: admin_secret_key
```

### Вариант 2: Через скрипт
```bash
cd Backend
dotnet run --project MigratePasswords
```

---

## Безопасность ключей

### Для разработки
Ключи хранятся в `appsettings.json`

### Для production

#### 1. Environment Variables
```bash
export Encryption__Key="your-production-key-32-chars"
export Encryption__IV="your-prod-iv-16"
export MonitoringApiKey="your-monitoring-api-key"
```

#### 2. User Secrets (для разработки)
```bash
dotnet user-secrets set "Encryption:Key" "your-secret-key"
dotnet user-secrets set "Encryption:IV" "your-secret-iv"
dotnet user-secrets set "MonitoringApiKey" "your-api-key"
```

#### 3. Azure Key Vault
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-vault.vault.azure.net/"),
    new DefaultAzureCredential()
);
```

---

## Примеры использования

### Пример 1: Создание пользователя из системы мониторинга

```javascript
// В системе мониторинга
const response = await fetch('http://localhost:3001/api/users', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    username: 'operator1',
    password: 'secure123',
    role: 'user',
    fullName: 'Оператор 1'
  })
});

const user = await response.json();
console.log('Создан пользователь:', user);
```

### Пример 2: Получение данных для мониторинга

```javascript
// В системе мониторинга
const response = await fetch('http://localhost:3001/api/monitoring/full-data', {
  headers: {
    'X-API-Key': 'monitoring_secret_key_12345678'
  }
});

const data = await response.json();
console.log('Статистика:', data.statistics);
console.log('Пользователи:', data.users);
console.log('Агенты:', data.recent_agents);
```

### Пример 3: Расшифровка пароля в системе мониторинга

```javascript
// В системе мониторинга
const encryptedPassword = "U2FsdGVkX1...";

const response = await fetch('http://localhost:3001/api/monitoring/decrypt-password', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': 'monitoring_secret_key_12345678'
  },
  body: JSON.stringify({
    encryptedPassword: encryptedPassword
  })
});

const result = await response.json();
console.log('Расшифрованный пароль:', result.decrypted_password);
```

---

## Тестирование

### Тест 1: Создание пользователя
```bash
curl -X POST http://localhost:3001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"test123","role":"user","fullName":"Test User"}'
```

### Тест 2: Вход в систему
```bash
curl -X POST http://localhost:3001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"test123"}'
```

### Тест 3: Получение статистики для мониторинга
```bash
curl http://localhost:3001/api/monitoring/statistics \
  -H "X-API-Key: monitoring_secret_key_12345678"
```

### Тест 4: Получение пользователей для мониторинга
```bash
curl http://localhost:3001/api/monitoring/users \
  -H "X-API-Key: monitoring_secret_key_12345678"
```

---

## Рекомендации по безопасности

1. **Смените ключи шифрования** в production
2. **Используйте HTTPS** для всех запросов
3. **Ротация ключей** - меняйте ключи периодически
4. **Ограничьте доступ** к API мониторинга по IP
5. **Логируйте доступ** к чувствительным данным
6. **Используйте rate limiting** для защиты от брутфорса
7. **Храните ключи безопасно** - не коммитьте в Git

---

## Troubleshooting

### Ошибка: "Invalid API key"
- Проверьте заголовок `X-API-Key`
- Убедитесь, что ключ совпадает с `MonitoringApiKey` в конфигурации

### Ошибка: "Неверный логин или пароль"
- Проверьте, что пользователь мигрирован на новую систему
- Убедитесь, что `password_hash` не пустой в БД

### Ошибка при расшифровке
- Проверьте, что ключи шифрования не изменились
- Убедитесь, что пароль был зашифрован с теми же ключами
