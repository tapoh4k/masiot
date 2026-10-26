# Hosts и управление агентами

## Сущность Host

Место развёртывания агентов. Типы:

| Тип | Лимит агентов | Deploy |
|-----|---------------|--------|
| `cluster` | N (`max_agents`) | обычно `docker` |
| `single` | 1 | `docker` или `machine` |
| `specific` | N | `custom` (точка расширения) |

Агент **всегда** привязан к Host (`agents.host_id NOT NULL`). Дерево `tree_nodes` остаётся для UI-навигации.

## API

- `GET/POST /api/hosts`
- `GET/PUT/DELETE /api/hosts/{id}`
- `POST /api/hosts/{id}/commands/{command}` — публикация команд 1–13 в очередь Host

При создании Host генерируются RabbitMQ user/password, очередь и routing key `Host.{id}` (exchange `topic_logs`). Автозапуск Host не выполняется.

## Пайплайн создания агента

1. Валидация `host_id` и лимита Host  
2. Сохранение агента в БД  
3. `QueueDeclare(agent.{id})` + bindings: `agent.{id}` и коды профессий/ОКПД (точки → `_`)  
4. Настроечное сообщение `3-6-1-2`  
5. Команда Host `host.create_agent` (Cluster/Single — стандарт, Specific — `host.command.specific`)

## Команды Host

`host.create_agent`, `host.create_agents`, `host.start_agent`, `host.start_agents`, `host.start_all_agents`, `host.stop_agent`, `host.stop_agents`, `host.stop_all_agents`, `host.delete_agent`, `host.delete_agents`, `host.delete_all_agents`, `host.move_agent`, `host.move_agents`.

Runtime-исполнение на стороне Docker/машины Host — вне scope backend (только публикация сообщений).

## Локальный вход

Логин UI: `admin` / `admin123`.  
После обновления схемы: `.\start-dev.ps1` (накатывает `schema.sql`, включая Default Cluster).
