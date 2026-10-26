# Release Notes

**Продукт:** MAS-IOT  
**Версия:** 1.0.0  
**Дата выпуска:** 2026-05-30

---

## Новые возможности

- Полноценный workflow-движок с 55 встроенными функциями
- Визуальный редактор workflow на React Flow
- Обмен сообщениями между агентами через RabbitMQ
- Импорт технологических процессов из Excel
- Иерархический классификатор ОКПД-2
- Модуль расписания (TimeTable)
- Двухуровневая система безопасности паролей (BCrypt + AES-256)
- Сквозное шифрование сообщений RabbitMQ
- Многоязычный интерфейс (русский)

## Исправления безопасности

- Удалены debug endpoint'ы (`/api/users/generate-hash`, `/api/admin/migrate-passwords`)
- Удалены endpoint'ы раскрытия паролей из мониторинга
- Swagger доступен только в режиме Development
- Добавлено сквозное шифрование RabbitMQ-сообщений

## Технологический стек

- Backend: .NET 8, EF Core, PostgreSQL, RabbitMQ
- Frontend: React 18, Vite, React Flow
- Безопасность: BCrypt, AES-256-CBC

## Известные ограничения

- Требуется PostgreSQL 14+
- RabbitMQ необходим только для обмена сообщениями (без него workflow работает в offline-режиме)
- Оптимизировано для Chrome/Firefox/Edge
