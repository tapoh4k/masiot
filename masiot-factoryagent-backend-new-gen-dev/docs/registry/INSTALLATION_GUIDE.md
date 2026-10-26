# Инструкция установки

**Продукт:** MAS-IOT  
**Версия:** 1.0.0  
**Платформы:** Windows 10/11, Linux (Ubuntu 20.04+), Docker

---

## Системные требования

| Компонент | Минимум | Рекомендуется |
|-----------|---------|---------------|
| CPU | 2 ядра | 4 ядра |
| RAM | 4 GB | 8 GB |
| Диск | 10 GB | 20 GB SSD |
| ОС | Windows 10 / Ubuntu 20.04 | Windows 11 / Ubuntu 22.04 |

## Необходимое ПО

- **PostgreSQL** 14+
- **.NET SDK** 8.0+
- **Node.js** 18+
- **RabbitMQ** 3.12+ (опционально, для обмена сообщениями)

## Быстрая установка

### 1. База данных

```bash
# Создать БД
psql -U postgres -c "CREATE DATABASE agenti_db;"

# Выполнить схему
psql -U postgres -d agenti_db -f schema.sql
```

### 2. Backend

```bash
cd Backend
cp appsettings.Example.json appsettings.json
# Отредактировать appsettings.json (пароли PostgreSQL и RabbitMQ)
dotnet restore
dotnet run
```

### 3. Frontend

```bash
npm install
npm run dev
```

### 4. Проверка

- Frontend: http://localhost:5173
- Backend API: http://localhost:3001/api/health
- Swagger (только в Development): http://localhost:3001/swagger

## Установка через Docker

```bash
docker-compose up -d
```

## Первоначальная настройка

1. Войти под администратором: `admin` / `admin123`
2. Создать пользователей через меню "Пользователи"
3. Настроить словарь ОКПД-2 (при необходимости)
4. Импортировать техпроцессы из Excel
