# Инструкция по установке

## 1. Системные требования

| Компонент | Минимум | Рекомендуется |
|-----------|---------|---------------|
| ОС | Windows 10 / Ubuntu 20.04 | Windows Server 2022 / Ubuntu 22.04 |
| CPU | 2 ядра | 4 ядра |
| RAM | 4 ГБ | 8 ГБ |
| Диск | 20 ГБ SSD | 50 ГБ SSD |
| .NET | SDK 8.0 | SDK 8.0 |
| Node.js | 18 LTS | 20 LTS |
| PostgreSQL | 12 | 16 |
| RabbitMQ | 3.8 | 3.12+ |

---

## 2. Установка зависимостей

### 2.1. PostgreSQL

**Windows:**
1. Скачайте установщик с [postgresql.org](https://www.postgresql.org/download/windows/).
2. Установите версию 16 с параметрами по умолчанию.
3. Запомните пароль суперпользователя `postgres`.
4. Убедитесь, что служба `postgresql-x64-16` запущена.

**Ubuntu:**
```bash
sudo apt update
sudo apt install postgresql-16
sudo systemctl enable postgresql
sudo systemctl start postgresql
```

### 2.2. RabbitMQ

**Windows:**
1. Установите Erlang OTP.
2. Установите RabbitMQ с [rabbitmq.com](https://www.rabbitmq.com/download.html).
3. Включите плагин Management:
   ```cmd
   rabbitmq-plugins enable rabbitmq_management
   ```
4. Веб-интерфейс будет доступен по адресу `http://localhost:15672` (логин/пароль: `guest`/`guest`).

**Docker (рекомендуется для разработки):**
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.12-management-alpine
```

### 2.3. .NET 8 SDK

Скачайте и установите с [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0).

Проверьте установку:
```bash
dotnet --version
```

### 2.4. Node.js

Скачайте LTS-версию с [nodejs.org](https://nodejs.org/).

Проверьте установку:
```bash
node --version
npm --version
```

---

## 3. Настройка базы данных

### 3.1. Создание БД

Подключитесь к PostgreSQL:
```bash
psql -U postgres -h localhost
```

Выполните:
```sql
CREATE DATABASE agenti_db WITH ENCODING = 'UTF8' LC_COLLATE = 'ru_RU.UTF-8' LC_CTYPE = 'ru_RU.UTF-8';
```

### 3.2. Инициализация схемы

Из корня проекта выполните:
```bash
psql -U postgres -h localhost -d agenti_db -f schema.sql
```

Файл `schema.sql` создаст все таблицы, индексы, триггеры и начальные данные (включая 55 workflow-функций и справочник ОКПД-2).

### 3.3. Настройка пароля

Убедитесь, что пароль пользователя `postgres` совпадает с указанным в `Backend/appsettings.json` (по умолчанию `123`).

```sql
ALTER USER postgres WITH PASSWORD '123';
```

---

## 4. Сборка и запуск Backend

### 4.1. Конфигурация

Откройте `Backend/appsettings.json` и проверьте строку подключения:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=agenti_db;Username=postgres;Password=123;Encoding=UTF8"
}
```

При необходимости измените параметры RabbitMQ и ключи шифрования (для production).

### 4.2. Сборка

```bash
cd Backend
dotnet restore
dotnet build
```

### 4.3. Запуск

```bash
dotnet run --urls "http://localhost:3001"
```

После запуска Swagger UI будет доступен по адресу: `http://localhost:3001/swagger`

---

## 5. Сборка и запуск Frontend

### 5.1. Установка зависимостей

Из корня проекта:
```bash
npm install
```

### 5.2. Конфигурация

Создайте файл `.env` в корне (опционально):
```env
VITE_API_URL=http://localhost:3001/api
```

Если `.env` отсутствует, frontend использует `http://localhost:3001/api` по умолчанию.

### 5.3. Запуск в режиме разработки

```bash
npm run dev
```

Приложение откроется по адресу `http://localhost:5173`.

### 5.4. Production-сборка

```bash
npm run build
```

Результат сборки появится в папке `dist/`.

---

## 6. Развёртывание через Docker

### 6.1. Быстрый старт

В корне проекта выполните:
```bash
docker-compose up -d
```

Это поднимет 4 контейнера:
- `postgres:16-alpine` (порт 5432)
- `rabbitmq:3.12-management-alpine` (порты 5672, 15672)
- `backend` (порт 3001)
- `frontend` (порт 5173, через Nginx)

### 6.2. Проверка

- Frontend: `http://localhost:5173`
- Backend API: `http://localhost:3001`
- Swagger: `http://localhost:3001/swagger`
- RabbitMQ Management: `http://localhost:15672`

---

## 7. Первоначальная настройка

### 7.1. Создание администратора

После первого запуска backend в БД автоматически создаётся пользователь:
- **Логин:** `admin`
- **Пароль:** `admin123`

**Важно:** сразу после первого входа измените пароль администратора через интерфейс управления пользователями.

### 7.2. Проверка API

Импортируйте Postman-коллекцию из файла `postman_collection.json` и выполните тестовые запросы.

---

## 8. Устранение типовых проблем

### 8.1. Ошибка подключения к PostgreSQL

**Симптом:** Backend возвращает `500` на все запросы.

**Решение:**
```bash
# Проверьте, запущен ли PostgreSQL
pg_isready -h localhost -p 5432

# Проверьте пароль
psql -U postgres -h localhost -d agenti_db -c "SELECT 1"
```

### 8.2. RabbitMQ недоступен

**Симптом:** В логах `BrokerUnreachableException`.

**Решение:**
```bash
# Windows
sc start rabbitmq

# Linux
sudo systemctl start rabbitmq-server
```

### 8.3. CORS-ошибки во фронтенде

**Симптом:** В консоли браузера ошибки `CORS policy`.

**Решение:** Проверьте, что в `Backend/Program.cs` включена политика `AllowFrontend` и она разрешает origin фронтенда.

---

## 9. Обновление системы

1. Сделайте резервную копию БД:
   ```bash
   pg_dump -U postgres -d agenti_db > backup_$(date +%Y%m%d).sql
   ```
2. Остановите контейнеры / службы.
3. Получите новую версию кода (`git pull` или распакуйте архив).
4. Примените миграции EF Core:
   ```bash
   cd Backend
dotnet ef database update
   ```
5. Пересоберите frontend:
   ```bash
   npm install
   npm run build
   ```
6. Запустите систему.
