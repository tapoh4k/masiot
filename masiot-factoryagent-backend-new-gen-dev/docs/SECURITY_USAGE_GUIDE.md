# Руководство по использованию системы безопасности

## ✅ Что реализовано

### 1. Двухуровневая защита паролей
- **BCrypt хеширование** - для безопасной аутентификации (необратимо)
- **AES-256 шифрование** - для просмотра паролей админом (обратимо)

### 2. API для системы мониторинга
- Защищен API ключом
- Возвращает зашифрованные пароли
- Endpoint для расшифровки паролей

### 3. Миграция существующих пользователей
- Все старые пользователи автоматически мигрированы
- Пароли зашифрованы и захешированы

---

## 🔑 Ключи безопасности

### В файле appsettings.json:
```json
{
  "Encryption": {
    "Key": "MySecretEncryptionKey123456789",
    "IV": "MySecretIV123456"
  },
  "MonitoringApiKey": "monitoring_secret_key_12345678"
}
```

**⚠️ ВАЖНО:** Перед деплоем в production смените эти ключи на свои!

---

## 📋 Как использовать в основном приложении

### Создание пользователя (админ)
```javascript
const response = await fetch('http://localhost:3001/api/users', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'operator1',
    password: 'secure123',
    role: 'user',
    fullName: 'Оператор 1'
  })
});
```

### Вход в систему
```javascript
const response = await fetch('http://localhost:3001/api/users/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'operator1',
    password: 'secure123'
  })
});
```

### Просмотр всех пользователей (админ видит пароли)
```javascript
const response = await fetch('http://localhost:3001/api/users');
const users = await response.json();
// users[0].password будет расшифрованным паролем
```

---

## 🖥️ Как использовать в системе мониторинга

### 1. Получить статистику
```javascript
const response = await fetch('http://localhost:3001/api/monitoring/statistics', {
  headers: {
    'X-API-Key': 'monitoring_secret_key_12345678'
  }
});

const data = await response.json();
console.log('Всего агентов:', data.agents.total);
console.log('Пользователей:', data.users_count);
```

**Ответ:**
```json
{
  "timestamp": "2026-03-07T02:00:00Z",
  "agents": {
    "total": 410,
    "tech_process": 234,
    "equipment": 50,
    "instrument": 50,
    "personnel": 25,
    "blank": 51
  },
  "users_count": 4,
  "tree_nodes_count": 59
}
```

### 2. Получить данные пользователей (с зашифрованными паролями)
```javascript
const response = await fetch('http://localhost:3001/api/monitoring/users', {
  headers: {
    'X-API-Key': 'monitoring_secret_key_12345678'
  }
});

const data = await response.json();
console.log('Пользователи:', data.users);
```

**Ответ:**
```json
{
  "timestamp": "2026-03-07T02:00:00Z",
  "users": [
    {
      "id": 4,
      "username": "admin",
      "encrypted_password": "yJ8NCgdQIF0H2yzlDAV3kw==",
      "role": "admin",
      "full_name": "Администратор",
      "created_at": "2026-03-02T21:50:40.801392",
      "updated_at": "2026-03-07T02:01:19.633749"
    }
  ]
}
```

### 3. Расшифровать пароль (если нужно показать в мониторинге)
```javascript
const encryptedPassword = "yJ8NCgdQIF0H2yzlDAV3kw==";

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
console.log('Пароль:', result.decrypted_password); // "admin"
```

### 4. Получить все данные сразу
```javascript
const response = await fetch('http://localhost:3001/api/monitoring/full-data', {
  headers: {
    'X-API-Key': 'monitoring_secret_key_12345678'
  }
});

const data = await response.json();
console.log('Статистика:', data.statistics);
console.log('Пользователи:', data.users);
console.log('Последние агенты:', data.recent_agents);
```

---

## 🧪 Тестирование

### Тест 1: Создать пользователя
```bash
curl -X POST http://localhost:3001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"test123","role":"user","fullName":"Test User"}'
```

### Тест 2: Войти в систему
```bash
curl -X POST http://localhost:3001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"test123"}'
```

### Тест 3: Получить статистику для мониторинга
```bash
curl http://localhost:3001/api/monitoring/statistics \
  -H "X-API-Key: monitoring_secret_key_12345678"
```

### Тест 4: Получить пользователей для мониторинга
```bash
curl http://localhost:3001/api/monitoring/users \
  -H "X-API-Key: monitoring_secret_key_12345678"
```

### Тест 5: Расшифровать пароль
```bash
curl -X POST http://localhost:3001/api/monitoring/decrypt-password \
  -H "Content-Type: application/json" \
  -H "X-API-Key: monitoring_secret_key_12345678" \
  -d '{"encryptedPassword":"yJ8NCgdQIF0H2yzlDAV3kw=="}'
```

---

## 🔒 Безопасность в Production

### 1. Смените ключи шифрования
Сгенерируйте новые случайные ключи:
```bash
# Для Key (32 символа)
openssl rand -base64 24

# Для IV (16 символов)
openssl rand -base64 12
```

### 2. Используйте переменные окружения
```bash
export Encryption__Key="ваш-новый-ключ-32-символа"
export Encryption__IV="ваш-новый-iv-16"
export MonitoringApiKey="ваш-новый-api-ключ"
```

### 3. Настройте HTTPS
Все запросы должны идти через HTTPS, особенно к API мониторинга.

### 4. Ограничьте доступ по IP
Настройте firewall, чтобы только сервер мониторинга мог обращаться к `/api/monitoring/*`

---

## 📊 Пример интеграции с системой мониторинга

### React компонент для отображения пользователей
```javascript
import React, { useState, useEffect } from 'react';

function UsersMonitor() {
  const [users, setUsers] = useState([]);
  const [decryptedPasswords, setDecryptedPasswords] = useState({});

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    const response = await fetch('http://localhost:3001/api/monitoring/users', {
      headers: {
        'X-API-Key': 'monitoring_secret_key_12345678'
      }
    });
    const data = await response.json();
    setUsers(data.users);
  };

  const decryptPassword = async (encryptedPassword, userId) => {
    const response = await fetch('http://localhost:3001/api/monitoring/decrypt-password', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': 'monitoring_secret_key_12345678'
      },
      body: JSON.stringify({ encryptedPassword })
    });
    const result = await response.json();
    setDecryptedPasswords(prev => ({
      ...prev,
      [userId]: result.decrypted_password
    }));
  };

  return (
    <div>
      <h2>Пользователи системы</h2>
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Логин</th>
            <th>Пароль</th>
            <th>Роль</th>
            <th>Полное имя</th>
          </tr>
        </thead>
        <tbody>
          {users.map(user => (
            <tr key={user.id}>
              <td>{user.id}</td>
              <td>{user.username}</td>
              <td>
                {decryptedPasswords[user.id] || '••••••'}
                <button onClick={() => decryptPassword(user.encrypted_password, user.id)}>
                  Показать
                </button>
              </td>
              <td>{user.role}</td>
              <td>{user.full_name}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default UsersMonitor;
```

---

## ✅ Проверка работоспособности

Все тесты пройдены успешно:
- ✅ Создание нового пользователя с шифрованием
- ✅ Вход в систему с BCrypt аутентификацией
- ✅ Миграция существующих пользователей (3 пользователя)
- ✅ API мониторинга возвращает зашифрованные пароли
- ✅ Расшифровка паролей работает корректно
- ✅ Защита API ключом работает

---

## 📞 Поддержка

Если возникнут вопросы:
1. Проверьте логи backend: `d:\agenti\Backend\backend.log`
2. Убедитесь, что API ключ правильный
3. Проверьте, что ключи шифрования не изменились
4. Посмотрите документацию: `docs/SECURITY_IMPLEMENTATION.md`
