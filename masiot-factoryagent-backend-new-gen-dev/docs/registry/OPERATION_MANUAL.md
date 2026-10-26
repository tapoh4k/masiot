# Инструкция эксплуатации

**Продукт:** MAS-IOT  
**Версия:** 1.0.0

---

## Запуск и остановка системы

### Локальный запуск

```bash
# Backend
cd Backend
dotnet run

# Frontend (в отдельном терминале)
npm run dev
```

### Docker

```bash
# Запуск
docker-compose up -d

# Остановка
docker-compose down

# Просмотр логов
docker-compose logs -f backend
```

## Резервное копирование

### PostgreSQL

```bash
pg_dump -U postgres -d agenti_db > backup_$(date +%Y%m%d).sql
```

### Восстановление

```bash
psql -U postgres -d agenti_db -f backup_20260101.sql
```

## Проверка работоспособности

- Health-check: `GET http://localhost:3001/api/health`
- Логи Backend: `Backend/logs/backend-YYYYMMdd.log`
- Логи Frontend: консоль браузера (F12)

## Обновление версии

1. Сделать резервную копию БД
2. Остановить сервисы
3. Обновить код (`git pull` или заменить файлы)
4. Пересоздать БД с `schema.sql` (если есть миграции)
5. Перезапустить Backend и Frontend

## Мониторинг

- API статистики: `GET /api/monitoring/statistics` (требуется `X-API-Key`)
- Логи хранятся 30 дней (ротация по дням)
- RabbitMQ Management UI: http://localhost:15672 (guest/guest)
