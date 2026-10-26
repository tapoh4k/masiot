# Пакет документации для реестра отечественного ПО

**Продукт:** MAS-IOT — Система управления многоагентной производственной средой  
**Версия:** 1.0.0  
**Дата:** 2026-05-30  

---

## Состав пакета

| № | Документ | Описание |
|---|----------|----------|
| 1 | [FUNCTIONAL_SPECIFICATION.md](FUNCTIONAL_SPECIFICATION.md) | Описание функциональных характеристик |
| 2 | [INSTALLATION_GUIDE.md](INSTALLATION_GUIDE.md) | Инструкция установки |
| 3 | [OPERATION_MANUAL.md](OPERATION_MANUAL.md) | Инструкция эксплуатации |
| 4 | [ADMIN_GUIDE.md](ADMIN_GUIDE.md) | Руководство администратора |
| 5 | [USER_GUIDE.md](USER_GUIDE.md) | Руководство пользователя |
| 6 | [ARCHITECTURE.md](ARCHITECTURE.md) | Описание архитектуры |
| 7 | [RELEASE_NOTES.md](RELEASE_NOTES.md) | Список изменений (Release Notes) |
| 8 | [MODULE_DESCRIPTION.md](MODULE_DESCRIPTION.md) | Описание модулей |
| 9 | [RABBITMQ_ENCRYPTION_GUIDE.md](RABBITMQ_ENCRYPTION_GUIDE.md) | Шифрование сообщений RabbitMQ |
| 10 | [OFFLINE_LINUX_DEPLOYMENT.md](OFFLINE_LINUX_DEPLOYMENT.md) | Развёртывание на Linux без интернета |
| 11 | [SECURITY_HARDENING.md](SECURITY_HARDENING.md) | Итоги аудита безопасности и устранение уязвимостей |

---

## Краткие сведения о продукте

MAS-IOT — это веб-платформа для управления производственными агентами (оборудование, персонал, технологические процессы) с использованием многоагентной архитектуры, workflow-движка и интеграции с RabbitMQ.

**Ключевые возможности:**
- Иерархическое дерево агентов (папки и агенты)
- Workflow-шаблоны с визуальным редактором
- Обмен сообщениями между агентами через RabbitMQ
- Импорт технологических процессов из Excel
- Классификатор ОКПД-2
- Расписание работ и планирование
- Двухуровневая система безопасности паролей (BCrypt + AES-256)

**Технологический стек:**
- Backend: ASP.NET Core 8, Entity Framework Core, PostgreSQL, RabbitMQ
- Frontend: React 18, Vite, React Flow
