# Развёртывание на Linux без доступа к интернету

**Продукт:** MAS-IOT  
**Версия:** 1.0.0  

---

## Общая схема

```
┌─────────────────┐     USB/локальная сеть     ┌─────────────────────┐
│   Машина с      │ ───────────────────────────► │  Целевой сервер     │
│   интернетом    │   Перенос пакетов и образов  │  Linux (offline)    │
│   (подготовка)  │                              │                     │
└─────────────────┘                              └─────────────────────┘
```

## Что потребуется

| Компонент | Оффлайн-установка |
|-----------|-------------------|
| Docker Engine | `.rpm` / `.deb` пакеты |
| Docker Compose | бинарный файл |
| Docker-образы Backend | `docker save` → `.tar` |
| Docker-образы Frontend | `docker save` → `.tar` |
| Docker-образ PostgreSQL | `docker save` → `.tar` |
| Docker-образ RabbitMQ | `docker save` → `.tar` |
| Исходный код | `.zip` / `.tar.gz` |

## Подготовка пакета (на машине с интернетом)

### 1. Клонирование и сборка

```bash
git clone <repo> masiot
cd masiot
```

### 2. Сборка Docker-образов

```bash
docker build -t masiot-backend:latest ./Backend
docker build -t masiot-frontend:latest -f Dockerfile.frontend .
```

### 3. Сохранение образов

```bash
mkdir -p offline-packages
docker save -o offline-packages/masiot-backend.tar masiot-backend:latest
docker save -o offline-packages/masiot-frontend.tar masiot-frontend:latest
docker save -o offline-packages/postgres16.tar postgres:16-alpine
docker save -o offline-packages/rabbitmq.tar rabbitmq:3.12-management-alpine
```

### 4. Сохранение исходников

```bash
git archive --format=tar.gz -o offline-packages/masiot-source.tar.gz HEAD
```

### 5. Копирование на целевой сервер

```bash
scp -r offline-packages/ user@target-server:/opt/
```

## Установка на целевом сервере (offline)

### 1. Установка Docker

```bash
# CentOS/RHEL/Fedora
sudo rpm -i ./docker-engine-*.rpm

# Ubuntu/Debian
sudo dpkg -i ./docker-ce_*.deb
sudo apt-get install -f
```

### 2. Установка Docker Compose

```bash
sudo cp docker-compose-linux-x86_64 /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

### 3. Загрузка образов

```bash
cd /opt/offline-packages
sudo docker load -i masiot-backend.tar
sudo docker load -i masiot-frontend.tar
sudo docker load -i postgres16.tar
sudo docker load -i rabbitmq.tar
```

### 4. Развёртывание

```bash
cd /opt/masiot
docker-compose -f docker-compose.offline.yml up -d
```

## docker-compose.offline.yml

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: agenti_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./schema.sql:/docker-entrypoint-initdb.d/01-schema.sql
    ports:
      - "5432:5432"

  rabbitmq:
    image: rabbitmq:3.12-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"

  backend:
    image: masiot-backend:latest
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=agenti_db;Username=postgres;Password=${DB_PASSWORD}
    ports:
      - "3001:3001"
    depends_on:
      - postgres
      - rabbitmq

  frontend:
    image: masiot-frontend:latest
    ports:
      - "80:80"
    depends_on:
      - backend

volumes:
  pgdata:
```

## Переменные окружения

Создайте файл `.env`:

```bash
DB_PASSWORD=your_strong_password
MONITORING_API_KEY=your_monitoring_key
RABBITMQ_DEFAULT_USER=admin
RABBITMQ_DEFAULT_PASS=your_rabbit_password
```

## Проверка после развёртывания

```bash
# Статус контейнеров
docker-compose ps

# Логи backend
docker-compose logs -f backend

# Health-check
curl http://localhost:3001/api/health
```

## Обновление (offline)

1. Собрать новые образы на машине с интернетом
2. Сохранить `docker save`
3. Перенести на целевой сервер
4. `docker load`
5. `docker-compose up -d`
