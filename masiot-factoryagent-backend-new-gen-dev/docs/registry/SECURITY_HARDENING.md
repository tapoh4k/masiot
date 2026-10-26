# Итоги аудита безопасности и устранение уязвимостей

**Продукт:** MAS-IOT  
**Версия:** 1.0.0  
**Дата аудита:** 2026-05-18

---

## Выполненные мероприятия

### 1. Удалены debug- и тестовые endpoint'ы

| Удалённый компонент | Причина |
|---------------------|---------|
| `GET /api/users/generate-hash` | Возвращал хеш пароля в открытом виде |
| `POST /api/admin/migrate-passwords` | Позволял любому пользователю перехешировать все пароли |
| `GET /api/monitoring/users` | Возвращал зашифрованные пароли всех пользователей |
| `GET /api/monitoring/full-data` | Возвращал пароли + данные пользователей |
| `POST /api/monitoring/decrypt-password` | Oracle для дешифровки любого зашифрованного пароля |

### 2. Swagger закрыт для Production

- Swagger UI и Swagger JSON доступны **только** при `ASPNETCORE_ENVIRONMENT=Development`
- В Production (`Production`) Swagger недоступен

### 3. Добавлено сквозное шифрование RabbitMQ

- Реализован `RabbitMQMessageEncryptionService`
- Алгоритм: AES-256-CBC
- Формат: `ENC:[base64_payload]`
- Обратная совместимость: система принимает как зашифрованные, так и незашифрованные сообщения
- Управление через `appsettings.json`: `"RabbitMQ:EncryptMessages": true/false`

### 4. Сохранённые механизмы безопасности

- **BCrypt** — хеширование паролей (work factor: 12)
- **AES-256-CBC** — шифрование паролей для просмотра администратором
- **Rate Limiting** — защита endpoint'а логина от brute-force
- **CORS** — ограничение origin (localhost только для разработки)
- **API Key** для мониторинга (constant-time comparison)

## Рекомендации для production

1. Перевести CORS с `localhost` на конкретные домены
2. Настроить HTTPS/TLS для Backend и Frontend
3. Ограничить доступ к PostgreSQL и RabbitMQ firewall'ом
4. Регулярно менять `Encryption:Key` и `MonitoringApiKey`
5. Включить логирование всех аутентификационных событий
