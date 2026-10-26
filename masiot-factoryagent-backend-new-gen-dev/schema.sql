-- ============================================
-- FACTORY AGENT SYSTEM - DATABASE SCHEMA
-- ============================================
-- Версия: 3.4 (добавлена старая структура очереди агентов)
-- Дата: 02.05.2026
--
-- ВАЖНО: Этот файл содержит ВСЁ необходимое для создания БД
-- Включает все исправления и миграции
-- Безопасен для повторного выполнения
--
-- Использование:
-- psql -U postgres -d agenti_db -f schema.sql
-- ============================================

-- Устанавливаем кодировку клиента UTF-8 для корректной работы с русским языком
SET client_encoding TO 'UTF8';

-- ============================================
-- ТАБЛИЦЫ
-- ============================================

-- Таблица для узлов дерева (папки и агенты)
CREATE TABLE IF NOT EXISTS tree_nodes (
  id VARCHAR(255) PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  icon VARCHAR(50),
  type VARCHAR(50) NOT NULL,
  parent_id VARCHAR(255),
  position INTEGER DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица для данных агентов
CREATE TABLE IF NOT EXISTS agents (
  id SERIAL PRIMARY KEY,
  queue TEXT,
  node_id VARCHAR(255) NOT NULL,
  name VARCHAR(255),
  type VARCHAR(50) DEFAULT 'other',
  profession VARCHAR(255),
  status INTEGER DEFAULT 0,
  quantity INTEGER DEFAULT 1,
  queue_number BIGINT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица для параметров агентов
CREATE TABLE IF NOT EXISTS agent_parameters (
  id SERIAL PRIMARY KEY,
  agent_id INTEGER NOT NULL,
  name VARCHAR(255) NOT NULL,
  unit VARCHAR(50),
  value VARCHAR(255),
  description TEXT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица для истории загрузок файлов
CREATE TABLE IF NOT EXISTS file_uploads (
  id SERIAL PRIMARY KEY,
  file_name VARCHAR(255) NOT NULL,
  uploaded_at TIMESTAMP NOT NULL,
  status VARCHAR(50) NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица для статистики (одна строка)
CREATE TABLE IF NOT EXISTS statistics (
  id INTEGER PRIMARY KEY DEFAULT 1,
  agents_count INTEGER DEFAULT 0,
  equipment_count INTEGER DEFAULT 0,
  processes_count INTEGER DEFAULT 0,
  instructions_count INTEGER DEFAULT 0,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT single_row CHECK (id = 1)
);

-- Таблица для словаря параметров
CREATE TABLE IF NOT EXISTS parameter_dictionary (
  id SERIAL PRIMARY KEY,
  name VARCHAR(255) NOT NULL UNIQUE,
  description TEXT,
  type VARCHAR(50) DEFAULT 'string',
  unit VARCHAR(50),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица для словаря ОКПД-2 (классификатор)
CREATE TABLE IF NOT EXISTS okpd2_dictionary (
  id SERIAL PRIMARY KEY,
  code VARCHAR(20) NOT NULL UNIQUE,
  name VARCHAR(500) NOT NULL,
  description TEXT,
  parent_code VARCHAR(20),
  level INTEGER DEFAULT 0,
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица маппинга названий на коды ОКПД-2 (для автоподстановки при создании агентов)
CREATE TABLE IF NOT EXISTS blank_okpd2_mapping (
  id SERIAL PRIMARY KEY,
  keywords VARCHAR(255) NOT NULL,
  okpd2_code VARCHAR(50) NOT NULL,
  description TEXT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_blank_okpd2_keywords ON blank_okpd2_mapping(keywords);
CREATE INDEX IF NOT EXISTS idx_blank_okpd2_code ON blank_okpd2_mapping(okpd2_code);
CREATE INDEX IF NOT EXISTS idx_blank_okpd2_code_keywords ON blank_okpd2_mapping(okpd2_code, keywords);

-- Таблица для шаблонов workflow
CREATE TABLE IF NOT EXISTS workflow_templates (
  id SERIAL PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  nodes JSONB NOT NULL DEFAULT '[]'::jsonb,
  edges JSONB NOT NULL DEFAULT '[]'::jsonb,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица для функций workflow
CREATE TABLE IF NOT EXISTS workflow_functions (
  id SERIAL PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  function_class VARCHAR(500) NOT NULL,
  function_name VARCHAR(255) NOT NULL,
  params JSONB NOT NULL DEFAULT '[]'::jsonb,
  returns VARCHAR(255),
  icon VARCHAR(10),
  type VARCHAR(50) NOT NULL DEFAULT 'function',
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Таблица для пользователей (авторизация)
CREATE TABLE IF NOT EXISTS users (
  id SERIAL PRIMARY KEY,
  username VARCHAR(255) NOT NULL UNIQUE,
  password VARCHAR(255) NOT NULL,
  role VARCHAR(50) NOT NULL DEFAULT 'user',
  full_name VARCHAR(255),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица для списка выполняемых workflow
CREATE TABLE IF NOT EXISTS wf_list (
  id SERIAL PRIMARY KEY,
  step INTEGER DEFAULT 0,
  time_start TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  status INTEGER DEFAULT 0,
  parameters JSONB DEFAULT '{}'::jsonb,
  agent_id VARCHAR(255) NOT NULL,
  wf_type VARCHAR(255) NOT NULL,
  wf_functions JSONB DEFAULT '[]'::jsonb,
  initiator VARCHAR(255),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- ТЕХПРОЦЕССЫ (TechProcess) и подготовленные сообщения (PendingMessage)
-- ============================================

-- Таблица техпроцессов
CREATE TABLE IF NOT EXISTS tech_processes (
  id SERIAL PRIMARY KEY,
  code VARCHAR(50) NOT NULL,
  name VARCHAR(255),

  source_kind VARCHAR(50) NOT NULL DEFAULT 'xlsx',
  source_file_name VARCHAR(255),
  source_sheet_name VARCHAR(255),
  source_checksum VARCHAR(100),
  imported_at TIMESTAMP,

  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Операции техпроцесса
CREATE TABLE IF NOT EXISTS tech_process_operations (
  id SERIAL PRIMARY KEY,
  tech_process_id INTEGER NOT NULL,

  op_no INTEGER,
  op_type VARCHAR(255),
  blank_code VARCHAR(255),
  setup_time_min DOUBLE PRECISION,
  process_time_min DOUBLE PRECISION,
  service_time_min DOUBLE PRECISION,
  equipment_code VARCHAR(255),
  tool_codes TEXT, -- строка (CSV/;), нормализуем на уровне приложения
  profession_code VARCHAR(255),
  description TEXT,
  extra TEXT,

  position INTEGER DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Подготовленные (draft) сообщения для последующей отправки в RabbitMQ
CREATE TABLE IF NOT EXISTS pending_messages (
  id SERIAL PRIMARY KEY,
  agent_id INTEGER NOT NULL,
  kind VARCHAR(50) NOT NULL DEFAULT 'work',
  workflow_type VARCHAR(255) NOT NULL,
  payload JSONB NOT NULL DEFAULT '{}'::jsonb,
  status VARCHAR(50) NOT NULL DEFAULT 'draft', -- draft|sent|failed
  error TEXT,
  sent_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Добавляем в agents привязку 1:1 к техпроцессу
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'tech_process_id'
    ) THEN
        ALTER TABLE agents ADD COLUMN tech_process_id INTEGER;
        RAISE NOTICE '✓ Колонка tech_process_id добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка tech_process_id уже существует';
    END IF;
END $$;

-- Внешние ключи для техпроцессов
ALTER TABLE tech_process_operations
DROP CONSTRAINT IF EXISTS fk_tp_ops_tp;
ALTER TABLE tech_process_operations
ADD CONSTRAINT fk_tp_ops_tp
FOREIGN KEY (tech_process_id) REFERENCES tech_processes(id)
ON DELETE CASCADE;

ALTER TABLE pending_messages
DROP CONSTRAINT IF EXISTS fk_pending_messages_agent;
ALTER TABLE pending_messages
ADD CONSTRAINT fk_pending_messages_agent
FOREIGN KEY (agent_id) REFERENCES agents(id)
ON DELETE CASCADE;

ALTER TABLE agents
DROP CONSTRAINT IF EXISTS fk_agents_tech_process;
ALTER TABLE agents
ADD CONSTRAINT fk_agents_tech_process
FOREIGN KEY (tech_process_id) REFERENCES tech_processes(id)
ON DELETE SET NULL;

-- Уникальность: один агент -> один ТП и один ТП -> один агент
-- (в PostgreSQL UNIQUE допускает несколько NULL, что нам подходит)
CREATE UNIQUE INDEX IF NOT EXISTS uq_agents_tech_process_id ON agents(tech_process_id);

-- Индексы
CREATE INDEX IF NOT EXISTS idx_tp_code ON tech_processes(code);
CREATE INDEX IF NOT EXISTS idx_tp_ops_tp_id ON tech_process_operations(tech_process_id);
CREATE INDEX IF NOT EXISTS idx_pending_messages_agent_id ON pending_messages(agent_id);
CREATE INDEX IF NOT EXISTS idx_pending_messages_status ON pending_messages(status);

-- Таблица для расписания работ агентов
CREATE TABLE IF NOT EXISTS timetable (
  id SERIAL PRIMARY KEY,
  order_number VARCHAR(255) NOT NULL,
  time_start TIMESTAMP NOT NULL,
  time_end TIMESTAMP NOT NULL,
  quantity DOUBLE PRECISION DEFAULT 0,
  location VARCHAR(255),
  status INTEGER DEFAULT 0,
  partner_id VARCHAR(255),
  wf_id INTEGER,
  parameters JSONB DEFAULT '{}'::jsonb,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- ИСПРАВЛЕНИЕ: Удаление UNIQUE constraint
-- ============================================
-- Это критически важно! Без этого нельзя создавать
-- несколько агентов в одной папке

DO $$
BEGIN
    -- Удаляем все возможные UNIQUE constraints на node_id
    EXECUTE (
        SELECT string_agg('ALTER TABLE agents DROP CONSTRAINT IF EXISTS ' || conname || ';', ' ')
        FROM pg_constraint
        WHERE conrelid = 'agents'::regclass
        AND contype = 'u'
        AND conkey = ARRAY[(SELECT attnum FROM pg_attribute WHERE attrelid = 'agents'::regclass AND attname = 'node_id')]
    );

    RAISE NOTICE '✓ UNIQUE constraints на agents.node_id удалены';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '✓ UNIQUE constraints отсутствуют';
END $$;

-- ============================================
-- ИСПРАВЛЕНИЕ: Добавление колонки type
-- ============================================
-- Добавляем колонку type если её нет (для старых БД)

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'type'
    ) THEN
        ALTER TABLE agents ADD COLUMN type VARCHAR(50) DEFAULT 'other';
        RAISE NOTICE '✓ Колонка type добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка type уже существует';
    END IF;
END $$;

-- ============================================
-- ИСПРАВЛЕНИЕ: Добавление колонок profession и status
-- ============================================
-- Добавляем колонки profession и status если их нет (для старых БД)

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'profession'
    ) THEN
        ALTER TABLE agents ADD COLUMN profession VARCHAR(255);
        RAISE NOTICE '✓ Колонка profession добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка profession уже существует';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'status'
    ) THEN
        ALTER TABLE agents ADD COLUMN status INTEGER DEFAULT 0;
        RAISE NOTICE '✓ Колонка status добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка status уже существует';
    END IF;
END $$;

-- ============================================
-- ИСПРАВЛЕНИЕ: Добавление колонок workflow статуса
-- ============================================
-- Добавляем колонки для отслеживания статуса workflow

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'workflow_applied'
    ) THEN
        ALTER TABLE agents ADD COLUMN workflow_applied BOOLEAN NOT NULL DEFAULT false;
        RAISE NOTICE '✓ Колонка workflow_applied добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка workflow_applied уже существует';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'workflow_status'
    ) THEN
        ALTER TABLE agents ADD COLUMN workflow_status VARCHAR(50) NOT NULL DEFAULT 'not_ready';
        RAISE NOTICE '✓ Колонка workflow_status добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка workflow_status уже существует';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'applied_workflows'
    ) THEN
        ALTER TABLE agents ADD COLUMN applied_workflows JSONB NOT NULL DEFAULT '[]'::jsonb;
        RAISE NOTICE '✓ Колонка applied_workflows добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка applied_workflows уже существует';
    END IF;
END $$;

-- ============================================================================
-- МИГРАЦИЯ: Добавление колонки password_hash в таблицу users
-- Дата: 2026-03-08
-- Описание: Добавляем колонку password_hash для хранения хешированных паролей
-- ============================================================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'users' AND column_name = 'password_hash'
    ) THEN
        ALTER TABLE users ADD COLUMN password_hash VARCHAR(255) NOT NULL DEFAULT '';
        RAISE NOTICE '✓ Колонка password_hash добавлена в таблицу users';
    ELSE
        RAISE NOTICE '✓ Колонка password_hash уже существует';
    END IF;
END $$;

-- ============================================================================
-- МИГРАЦИЯ: Добавление OKPD-2 полей в таблицу agents
-- Дата: 2026-03-21
-- Описание: Добавляем agent_type, okpd2_code, okpd2_codes для новой архитектуры
-- ============================================================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'agent_type'
    ) THEN
        ALTER TABLE agents ADD COLUMN agent_type VARCHAR(50) NOT NULL DEFAULT 'resource';
        RAISE NOTICE '✓ Колонка agent_type добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка agent_type уже существует';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'okpd2_code'
    ) THEN
        ALTER TABLE agents ADD COLUMN okpd2_code VARCHAR(255);
        RAISE NOTICE '✓ Колонка okpd2_code добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка okpd2_code уже существует';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'okpd2_codes'
    ) THEN
        ALTER TABLE agents ADD COLUMN okpd2_codes JSONB NOT NULL DEFAULT '[]'::jsonb;
        RAISE NOTICE '✓ Колонка okpd2_codes добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка okpd2_codes уже существует';
    END IF;
END $$;

-- ============================================
-- МИГРАЦИЯ: Добавление старой структуры очереди агента
-- ============================================
-- В старой версии проекта у агента было строковое поле Queue.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'queue'
    ) THEN
        ALTER TABLE agents ADD COLUMN queue TEXT;
        RAISE NOTICE '✓ Колонка queue добавлена в таблицу agents';
    ELSE
        RAISE NOTICE '✓ Колонка queue уже существует';
    END IF;
END $$;

-- ============================================================================
-- МИГРАЦИЯ: Пополнение справочника ОКПД-2 из существующих агентов
-- Дата: 2026-04-30
-- Описание: переносим валидные okpd2_code из agents в список ОКПД-2 в настройках
-- ============================================================================

INSERT INTO blank_okpd2_mapping (keywords, okpd2_code, description, created_at, updated_at)
SELECT DISTINCT
    LEFT(TRIM(a.name), 255) AS keywords,
    TRIM(split.code) AS okpd2_code,
    'Автоматически добавлено из существующего агента "' || TRIM(a.name) || '" (' || COALESCE(a.type, 'other') || ')' AS description,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM agents a
CROSS JOIN LATERAL regexp_split_to_table(COALESCE(a.okpd2_code, ''), E'\\s*[,;|\\n\\r]+\\s*') AS split(code)
WHERE a.name IS NOT NULL
  AND TRIM(a.name) <> ''
  AND TRIM(split.code) <> ''
  AND TRIM(split.code) <> '00.00.00.000'
  AND TRIM(split.code) ~ '^(\\d{2}\\.\\d{2}(\\.\\d{1,3}){1,2}|\\d{4,6})$'
  AND NOT EXISTS (
      SELECT 1
      FROM blank_okpd2_mapping existing
      WHERE existing.okpd2_code = TRIM(split.code)
        AND LOWER(existing.keywords) = LOWER(LEFT(TRIM(a.name), 255))
  );

-- ============================================================================
-- Базовые ключевые фразы для заготовок (GET /api/blank-okpd2/find-by-name).
-- Поиск идет по вхождению фразы в название заготовки, длинные фразы проверяются первыми,
-- поэтому в листе "Заготовки" название должно начинаться с "Круг стальной…", "Лист стальной…" и т.п.
-- ============================================================================

INSERT INTO blank_okpd2_mapping (keywords, okpd2_code, description)
SELECT seed.keywords, seed.okpd2_code, seed.description
FROM (VALUES
    ('круг стальной горячекатаный', '25.73.40.130', 'Круг стальной горячекатаный'),
    ('круг стальной', '25.73.40.130', 'Круг стальной'),
    ('труба стальная', '25.73.40.140', 'Труба стальная'),
    ('труба', '25.73.40.140', 'Труба'),
    ('шестигранник', '25.73.40.150', 'Шестигранник'),
    ('полоса стальная', '25.73.40.120', 'Полоса стальная'),
    ('лист стальной', '25.73.40.110', 'Лист стальной'),
    ('уголок', '25.73.40.150', 'Уголок'),
    ('швеллер', '25.73.40.150', 'Швеллер'),
    ('балка', '25.73.40.150', 'Балка'),
    ('фланец', '25.73.50.110', 'Фланец'),
    ('втулка', '25.73.90.190', 'Втулка'),
    ('прокат стальной', '25.73.40.190', 'Прокат стальной')
) AS seed(keywords, okpd2_code, description)
WHERE NOT EXISTS (
    SELECT 1 FROM blank_okpd2_mapping existing
    WHERE LOWER(existing.keywords) = seed.keywords
);

-- ============================================
-- ВНЕШНИЕ КЛЮЧИ С КАСКАДНЫМ УДАЛЕНИЕМ
-- ============================================

-- Удаляем старые constraints
ALTER TABLE tree_nodes DROP CONSTRAINT IF EXISTS fk_tree_nodes_parent;
ALTER TABLE tree_nodes DROP CONSTRAINT IF EXISTS tree_nodes_parent_id_fkey;
ALTER TABLE agents DROP CONSTRAINT IF EXISTS agents_node_id_fkey;
ALTER TABLE agents DROP CONSTRAINT IF EXISTS fk_agents_node;
ALTER TABLE agent_parameters DROP CONSTRAINT IF EXISTS agent_parameters_agent_id_fkey;
ALTER TABLE agent_parameters DROP CONSTRAINT IF EXISTS fk_agent_parameters_agent;

-- Добавляем правильные constraints с CASCADE
ALTER TABLE tree_nodes
ADD CONSTRAINT fk_tree_nodes_parent
FOREIGN KEY (parent_id) REFERENCES tree_nodes(id)
ON DELETE CASCADE;

ALTER TABLE agents
ADD CONSTRAINT fk_agents_node
FOREIGN KEY (node_id) REFERENCES tree_nodes(id)
ON DELETE CASCADE;

ALTER TABLE agent_parameters
ADD CONSTRAINT fk_agent_parameters_agent
FOREIGN KEY (agent_id) REFERENCES agents(id)
ON DELETE CASCADE;

-- ============================================
-- ИНДЕКСЫ ДЛЯ ОПТИМИЗАЦИИ
-- ============================================

CREATE INDEX IF NOT EXISTS idx_tree_nodes_parent ON tree_nodes(parent_id);
CREATE INDEX IF NOT EXISTS idx_agents_node ON agents(node_id);
CREATE INDEX IF NOT EXISTS idx_agents_agent_type ON agents(agent_type);
CREATE INDEX IF NOT EXISTS idx_agents_okpd2_code ON agents(okpd2_code);
CREATE INDEX IF NOT EXISTS idx_parameters_agent ON agent_parameters(agent_id);
CREATE INDEX IF NOT EXISTS idx_file_uploads_uploaded_at ON file_uploads(uploaded_at DESC);
CREATE INDEX IF NOT EXISTS idx_parameter_dictionary_name ON parameter_dictionary(name);
CREATE INDEX IF NOT EXISTS idx_okpd2_dictionary_code ON okpd2_dictionary(code);
CREATE INDEX IF NOT EXISTS idx_okpd2_dictionary_parent ON okpd2_dictionary(parent_code);
CREATE INDEX IF NOT EXISTS idx_workflow_templates_name ON workflow_templates(name);
CREATE INDEX IF NOT EXISTS idx_workflow_functions_name ON workflow_functions(name);
CREATE INDEX IF NOT EXISTS idx_workflow_functions_type ON workflow_functions(type);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- Индексы для колонок, добавленных миграциями
CREATE INDEX IF NOT EXISTS idx_agents_workflow_applied ON agents(workflow_applied);
CREATE INDEX IF NOT EXISTS idx_agents_workflow_status ON agents(workflow_status);
CREATE INDEX IF NOT EXISTS idx_users_password_hash ON users(password_hash);

-- ============================================
-- ФУНКЦИИ И ТРИГГЕРЫ
-- ============================================

-- Автоматическое удаление старых file_uploads (оставляем 5 последних)
CREATE OR REPLACE FUNCTION cleanup_old_file_uploads()
RETURNS TRIGGER AS $$
BEGIN
  DELETE FROM file_uploads
  WHERE id NOT IN (
    SELECT id FROM file_uploads
    ORDER BY uploaded_at DESC LIMIT 5
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_cleanup_file_uploads ON file_uploads;
CREATE TRIGGER trigger_cleanup_file_uploads
AFTER INSERT ON file_uploads
FOR EACH STATEMENT
EXECUTE FUNCTION cleanup_old_file_uploads();

-- Пересчет статистики
CREATE OR REPLACE FUNCTION recalculate_statistics()
RETURNS void AS $$
BEGIN
  UPDATE statistics SET
    agents_count = (SELECT COUNT(*) FROM agents),
    equipment_count = (SELECT COUNT(*) FROM tree_nodes WHERE type = 'equipment'),
    processes_count = (SELECT COUNT(*) FROM tree_nodes WHERE type = 'process'),
    instructions_count = (SELECT COUNT(*) FROM tree_nodes WHERE type = 'instruction'),
    updated_at = CURRENT_TIMESTAMP
  WHERE id = 1;
END;
$$ LANGUAGE plpgsql;

-- Обновление статистики при изменении агентов
CREATE OR REPLACE FUNCTION update_statistics_on_agent_change()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    UPDATE statistics SET agents_count = agents_count + 1, updated_at = CURRENT_TIMESTAMP WHERE id = 1;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE statistics SET agents_count = agents_count - 1, updated_at = CURRENT_TIMESTAMP WHERE id = 1;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Обновление статистики при изменении узлов дерева
CREATE OR REPLACE FUNCTION update_statistics_on_node_change()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.type = 'equipment' THEN
      UPDATE statistics SET equipment_count = equipment_count + 1, updated_at = CURRENT_TIMESTAMP WHERE id = 1;
    ELSIF NEW.type = 'process' THEN
      UPDATE statistics SET processes_count = processes_count + 1, updated_at = CURRENT_TIMESTAMP WHERE id = 1;
    ELSIF NEW.type = 'instruction' THEN
      UPDATE statistics SET instructions_count = instructions_count + 1, updated_at = CURRENT_TIMESTAMP WHERE id = 1;
    END IF;
  ELSIF TG_OP = 'DELETE' THEN
    IF OLD.type = 'equipment' THEN
      UPDATE statistics SET equipment_count = equipment_count - 1, updated_at = CURRENT_TIMESTAMP WHERE id = 1;
    ELSIF OLD.type = 'process' THEN
      UPDATE statistics SET processes_count = processes_count - 1, updated_at = CURRENT_TIMESTAMP WHERE id = 1;
    ELSIF OLD.type = 'instruction' THEN
      UPDATE statistics SET instructions_count = instructions_count - 1, updated_at = CURRENT_TIMESTAMP WHERE id = 1;
    END IF;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Триггеры
DROP TRIGGER IF EXISTS trigger_update_statistics_agents ON agents;
CREATE TRIGGER trigger_update_statistics_agents
AFTER INSERT OR DELETE ON agents
FOR EACH ROW
EXECUTE FUNCTION update_statistics_on_agent_change();

DROP TRIGGER IF EXISTS trigger_update_statistics_nodes ON tree_nodes;
CREATE TRIGGER trigger_update_statistics_nodes
AFTER INSERT OR DELETE ON tree_nodes
FOR EACH ROW
EXECUTE FUNCTION update_statistics_on_node_change();

-- ============================================
-- ИНИЦИАЛИЗАЦИЯ ДАННЫХ
-- ============================================

-- Начальная статистика
INSERT INTO statistics (id, agents_count, equipment_count, processes_count, instructions_count)
VALUES (1, 0, 0, 0, 0)
ON CONFLICT (id) DO NOTHING;

-- Пересчитываем статистику
SELECT recalculate_statistics();

-- Дефолтный администратор (пароль: admin123)
-- Удаляем и вставляем заново для правильной кодировки UTF-8
DELETE FROM users WHERE username = 'admin';
INSERT INTO users (username, password, password_hash, role, full_name)
VALUES ('admin', 'F5NleNjT6cxMVcsID8/aGg==', '$2a$12$ULJ/BBTnJbqbCQlhKvgr5ug2S8nEjwo8X6DItA5RgxkwvSdrkzuJ2', 'admin', 'Администратор');

-- Заполняем словарь параметров из существующих
INSERT INTO parameter_dictionary (name, description, type, unit)
SELECT DISTINCT ap.name, ap.description, 'string', ap.unit
FROM agent_parameters ap
WHERE NOT EXISTS (SELECT 1 FROM parameter_dictionary pd WHERE pd.name = ap.name)
ON CONFLICT (name) DO NOTHING;

-- ============================================
-- WORKFLOW FUNCTIONS REGISTRATION (46 функций; без Example)
-- ============================================
-- Регистрация всех workflow функций в базе данных
DELETE FROM workflow_functions;

-- RabbitMQ Functions (4)
INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type) VALUES
('Отправить агенту', 'Отправить сообщение конкретному агенту',
 'Backend.Services.WorkflowFunctions.RabbitMQFunctions', 'SendToAgent',
 '[{"name":"agentId","type":"string","description":"ID агента"},{"name":"messageType","type":"string","description":"Тип сообщения"},{"name":"payload","type":"string","description":"JSON payload"}]'::jsonb,
 'status', '📤', 'RabbitMQ'),
('Отправить по ОКПД-2', 'Отправить сообщение всем агентам с OKPD-2 кодом',
 'Backend.Services.WorkflowFunctions.RabbitMQFunctions', 'SendToOkpd2Code',
 '[{"name":"okpd2Code","type":"string","description":"ОКПД-2 код"},{"name":"messageType","type":"string","description":"Тип сообщения"},{"name":"payload","type":"string","description":"JSON payload"}]'::jsonb,
 'status', '📡', 'RabbitMQ'),
('Broadcast по ОКПД-2', 'Отправить сообщение нескольким OKPD-2 кодам',
 'Backend.Services.WorkflowFunctions.RabbitMQFunctions', 'BroadcastToOkpd2Codes',
 '[{"name":"okpd2Codes","type":"string","description":"JSON array кодов"},{"name":"messageType","type":"string","description":"Тип сообщения"},{"name":"payload","type":"string","description":"JSON payload"}]'::jsonb,
 'status', '📢', 'RabbitMQ'),
('Отправить сообщение', 'Отправить сообщение с произвольным routing key',
 'Backend.Services.WorkflowFunctions.RabbitMQFunctions', 'SendMessage',
 '[{"name":"routingKey","type":"string","description":"Routing key"},{"name":"messageType","type":"string","description":"Тип сообщения"},{"name":"payload","type":"string","description":"JSON payload"}]'::jsonb,
 'status', '✉️', 'RabbitMQ');

-- Agent Info Functions (5)
INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type) VALUES
('Статус агента', 'Получить статус агента',
 'Backend.Services.WorkflowFunctions.AgentInfoFunctions', 'GetAgentStatus',
 '[{"name":"agentId","type":"string","description":"ID агента"}]'::jsonb,
 'status', 'ℹ️', 'AgentInfo'),
('Параметры агента', 'Получить параметры агента',
 'Backend.Services.WorkflowFunctions.AgentInfoFunctions', 'GetAgentParameters',
 '[{"name":"agentId","type":"string","description":"ID агента"}]'::jsonb,
 'parameters', '⚙️', 'AgentInfo'),
('Информация об агенте', 'Получить полную информацию об агенте',
 'Backend.Services.WorkflowFunctions.AgentInfoFunctions', 'GetAgentInfo',
 '[{"name":"agentId","type":"string","description":"ID агента"}]'::jsonb,
 'info', '📋', 'AgentInfo'),
('Обновить статус', 'Обновить статус агента',
 'Backend.Services.WorkflowFunctions.AgentInfoFunctions', 'UpdateAgentStatus',
 '[{"name":"agentId","type":"string","description":"ID агента"},{"name":"status","type":"int","description":"Новый статус"}]'::jsonb,
 'success', '🔄', 'AgentInfo'),
('Агенты по ОКПД-2', 'Получить список агентов по OKPD-2 коду',
 'Backend.Services.WorkflowFunctions.AgentInfoFunctions', 'GetAgentsByOkpd2Code',
 '[{"name":"okpd2Code","type":"string","description":"ОКПД-2 код"}]'::jsonb,
 'agents', '🔍', 'AgentInfo');

-- TimeTable Functions (5)
INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type) VALUES
('Создать запись', 'Создать запись в расписании',
 'Backend.Services.WorkflowFunctions.TimeTableFunctions', 'CreateScheduleEntry',
 '[{"name":"orderNumber","type":"string"},{"name":"timeStart","type":"string"},{"name":"timeEnd","type":"string"},{"name":"quantity","type":"double"},{"name":"location","type":"string"},{"name":"partnerId","type":"string"}]'::jsonb,
 'timetableId', '📅', 'TimeTable'),
('Расписание заказа', 'Получить расписание по номеру заказа',
 'Backend.Services.WorkflowFunctions.TimeTableFunctions', 'GetScheduleByOrder',
 '[{"name":"orderNumber","type":"string"}]'::jsonb,
 'schedule', '📆', 'TimeTable'),
('Обновить статус', 'Обновить статус записи расписания',
 'Backend.Services.WorkflowFunctions.TimeTableFunctions', 'UpdateScheduleStatus',
 '[{"name":"timetableId","type":"int"},{"name":"status","type":"int"}]'::jsonb,
 'success', '✅', 'TimeTable'),
('Расписание партнера', 'Получить расписание по ID партнера',
 'Backend.Services.WorkflowFunctions.TimeTableFunctions', 'GetScheduleByPartner',
 '[{"name":"partnerId","type":"string"}]'::jsonb,
 'schedule', '👤', 'TimeTable'),
('Расписание периода', 'Получить расписание за период',
 'Backend.Services.WorkflowFunctions.TimeTableFunctions', 'GetScheduleByPeriod',
 '[{"name":"startDate","type":"string"},{"name":"endDate","type":"string"}]'::jsonb,
 'schedule', '📊', 'TimeTable');

-- WFList Functions (6)
INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type) VALUES
('Создать workflow', 'Создать новый workflow',
 'Backend.Services.WorkflowFunctions.WFListFunctions', 'CreateWorkflow',
 '[{"name":"agentId","type":"string"},{"name":"wfType","type":"string"},{"name":"parameters","type":"string"},{"name":"initiator","type":"string"}]'::jsonb,
 'wfId', '➕', 'Workflow'),
('Обновить шаг', 'Обновить текущий шаг workflow',
 'Backend.Services.WorkflowFunctions.WFListFunctions', 'UpdateWorkflowStep',
 '[{"name":"wfId","type":"int"},{"name":"step","type":"int"}]'::jsonb,
 'success', '👣', 'Workflow'),
('Обновить статус', 'Обновить статус workflow',
 'Backend.Services.WorkflowFunctions.WFListFunctions', 'UpdateWorkflowStatus',
 '[{"name":"wfId","type":"int"},{"name":"status","type":"int"}]'::jsonb,
 'success', '🔄', 'Workflow'),
('Завершить workflow', 'Завершить выполнение workflow',
 'Backend.Services.WorkflowFunctions.WFListFunctions', 'CompleteWorkflow',
 '[{"name":"wfId","type":"int"}]'::jsonb,
 'success', '✔️', 'Workflow'),
('Информация о workflow', 'Получить информацию о workflow',
 'Backend.Services.WorkflowFunctions.WFListFunctions', 'GetWorkflowInfo',
 '[{"name":"wfId","type":"int"}]'::jsonb,
 'info', 'ℹ️', 'Workflow'),
('Активные workflow', 'Получить активные workflow для агента',
 'Backend.Services.WorkflowFunctions.WFListFunctions', 'GetActiveWorkflowsForAgent',
 '[{"name":"agentId","type":"string"}]'::jsonb,
 'workflows', '📋', 'Workflow');

-- TechProcess Functions (9)
INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type) VALUES
('Получить техпроцесс', 'Получить техпроцесс по ID',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'GetTechProcess',
 '[{"name":"techProcessId","type":"int"}]'::jsonb,
 'techProcess', '🏭', 'TechProcess'),
('Операция по номеру', 'Получить операцию по номеру',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'GetOperationByNumber',
 '[{"name":"techProcessId","type":"int"},{"name":"opNo","type":"int"}]'::jsonb,
 'operation', '🔧', 'TechProcess'),
('Все операции', 'Получить все операции техпроцесса',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'GetAllOperations',
 '[{"name":"techProcessId","type":"int"}]'::jsonb,
 'operations', '📝', 'TechProcess'),
('Операции оборудования', 'Получить операции по коду оборудования',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'GetOperationsByEquipment',
 '[{"name":"techProcessId","type":"int"},{"name":"equipmentCode","type":"string"}]'::jsonb,
 'operations', '⚙️', 'TechProcess'),
('Общее время', 'Рассчитать общее время техпроцесса',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'CalculateTotalTime',
 '[{"name":"techProcessId","type":"int"}]'::jsonb,
 'totalMinutes', '⏱️', 'TechProcess'),
('Время операции', 'Получить время операции',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'GetOperationTime',
 '[{"name":"techProcessId","type":"int"},{"name":"opNo","type":"int"}]'::jsonb,
 'minutes', '⏲️', 'TechProcess'),
('Код оборудования', 'Получить код оборудования операции',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'GetEquipmentCode',
 '[{"name":"techProcessId","type":"int"},{"name":"opNo","type":"int"}]'::jsonb,
 'equipmentCode', '🔧', 'TechProcess'),
('Код профессии', 'Получить код профессии операции',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'GetProfessionCode',
 '[{"name":"techProcessId","type":"int"},{"name":"opNo","type":"int"}]'::jsonb,
 'professionCode', '👷', 'TechProcess'),
('Количество операций', 'Получить количество операций',
 'Backend.Services.WorkflowFunctions.TechProcessFunctions', 'GetOperationsCount',
 '[{"name":"techProcessId","type":"int"}]'::jsonb,
 'count', '🔢', 'TechProcess');

-- WorkflowControl Functions (9)
INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type) VALUES
('Пауза workflow', 'Приостановить выполнение workflow',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'WorkFlowSleep',
 '[{"name":"wfId","type":"int"},{"name":"delaySeconds","type":"int"}]'::jsonb,
 'status', '⏸️', 'WorkflowControl'),
('Остановить workflow', 'Остановить выполнение workflow',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'WorkFlowStop',
 '[{"name":"wfId","type":"int"}]'::jsonb,
 'status', '⏹️', 'WorkflowControl'),
('Продолжить workflow', 'Продолжить выполнение workflow',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'WorkFlowContinue',
 '[{"name":"wfId","type":"int"},{"name":"fromStep","type":"int"}]'::jsonb,
 'status', '▶️', 'WorkflowControl'),
('Условный переход', 'Выполнить workflow по условию',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'WorkFlowRunByOneCondition',
 '[{"name":"condition","type":"bool"},{"name":"firstRoute","type":"string"},{"name":"secondRoute","type":"string"},{"name":"parameters","type":"string"}]'::jsonb,
 'route', '🔀', 'WorkflowControl'),
('Отправить сообщение', 'Отправить сообщение',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'SendMessage',
 '[{"name":"messageType","type":"string"},{"name":"routingKey","type":"string"},{"name":"payload","type":"string"}]'::jsonb,
 'status', '📨', 'WorkflowControl'),
('Отправить готовые', 'Отправить несколько подготовленных сообщений',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'SendReadyMessages',
 '[{"name":"messages","type":"string"}]'::jsonb,
 'count', '📬', 'WorkflowControl'),
('Текущий шаг', 'Получить текущий шаг workflow',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'GetWorkflowStep',
 '[{"name":"wfId","type":"int"}]'::jsonb,
 'step', '👣', 'WorkflowControl'),
('Параметры workflow', 'Получить параметры workflow',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'GetWorkflowParameters',
 '[{"name":"wfId","type":"int"}]'::jsonb,
 'parameters', '⚙️', 'WorkflowControl'),
('Обновить параметры', 'Обновить параметры workflow',
 'Backend.Services.WorkflowFunctions.WorkflowControlFunctions', 'UpdateWorkflowParameters',
 '[{"name":"wfId","type":"int"},{"name":"parameters","type":"string"}]'::jsonb,
 'success', '💾', 'WorkflowControl');

-- Planning Functions (8)
INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type) VALUES
('Подготовить ТП', 'Подготовить техпроцесс и определить первую операцию',
 'Backend.Services.WorkflowFunctions.PlanningFunctions', 'TechprocessPrepare',
 '[{"name":"techProcessId","type":"int"}]'::jsonb,
 'nextOperation', '🏁', 'Planning'),
('Сообщения агентам', 'Подготовить сообщения ресурсам о работе',
 'Backend.Services.WorkflowFunctions.PlanningFunctions', 'PrepareMessagesToAgentAboutJob',
 '[{"name":"techProcessId","type":"int"},{"name":"opNo","type":"int"},{"name":"workMessageType","type":"string"}]'::jsonb,
 'messages', '📋', 'Planning'),
('Сообщение СПР', 'Подготовить сообщение для СПР',
 'Backend.Services.WorkflowFunctions.PlanningFunctions', 'PrepareMessageToSPR',
 '[{"name":"techProcessId","type":"int"},{"name":"opNo","type":"int"},{"name":"typePlan","type":"string"},{"name":"wfId","type":"int"}]'::jsonb,
 'message', '🎯', 'Planning'),
('Добавить событие', 'Добавить событие в расписание',
 'Backend.Services.WorkflowFunctions.PlanningFunctions', 'AddTimetableEvent',
 '[{"name":"orderNumber","type":"string"},{"name":"operationNumber","type":"int"},{"name":"timeStart","type":"string"},{"name":"length","type":"int"},{"name":"timeEnd","type":"string"},{"name":"quantity","type":"double"},{"name":"location","type":"string"},{"name":"candidateStatus","type":"int"},{"name":"partnerId","type":"string"},{"name":"wfId","type":"int"},{"name":"parameters","type":"string"}]'::jsonb,
 'timetableId', '➕', 'Planning'),
('Обработать ответ СПР', 'Обработать ответ СПР',
 'Backend.Services.WorkflowFunctions.PlanningFunctions', 'ProcessingSPRResponse',
 '[{"name":"wfId","type":"int"},{"name":"result","type":"bool"},{"name":"orderNumber","type":"string"},{"name":"operationNumber","type":"int"},{"name":"shiftTime","type":"int"}]'::jsonb,
 'status', '✅', 'Planning'),
('Время окончания', 'Получить время окончания операции',
 'Backend.Services.WorkflowFunctions.PlanningFunctions', 'GetOperationTimeEnd',
 '[{"name":"orderNumber","type":"string"},{"name":"operationNumber","type":"int"}]'::jsonb,
 'timeEnd', '⏰', 'Planning'),
('Следующая операция', 'Проверить существование следующей операции',
 'Backend.Services.WorkflowFunctions.PlanningFunctions', 'NextOperationExist',
 '[{"name":"techProcessId","type":"int"},{"name":"currentOpNo","type":"int"}]'::jsonb,
 'result', '➡️', 'Planning'),
('Обновить время', 'Обновить время начала операции',
 'Backend.Services.WorkflowFunctions.PlanningFunctions', 'UpdateOperationTimeStart',
 '[{"name":"orderNumber","type":"string"},{"name":"operationNumber","type":"int"},{"name":"timeEnd","type":"string"},{"name":"deltaTime","type":"int"}]'::jsonb,
 'success', '🕐', 'Planning');

-- Убрать демо-функции Example (если остались от старых seed)
DELETE FROM workflow_functions
WHERE type = 'Example'
   OR function_class LIKE '%ExampleWorkflowFunctions%';

-- ============================================
-- OKPD-2 DICTIONARY SAMPLE DATA
-- ============================================
-- Примеры записей классификатора ОКПД-2 для тестирования

-- Раздел 28: Машины и оборудование
INSERT INTO okpd2_dictionary (code, name, description, parent_code, level, is_active) VALUES
('28', 'Машины и оборудование, не включенные в другие группировки', 'Производство машин и оборудования общего и специального назначения', NULL, 1, true),
('28.4', 'Машины для металлообработки и станки', 'Производство станков и оборудования для обработки металлов', '28', 2, true),
('28.41', 'Станки металлорежущие', 'Производство металлорежущих станков', '28.4', 3, true),
('28.41.1', 'Станки токарные', 'Токарные станки различных типов', '28.41', 4, true),
('28.41.11', 'Станки токарно-винторезные', 'Универсальные токарно-винторезные станки', '28.41.1', 5, true),
('28.41.12', 'Станки токарные с ЧПУ', 'Токарные станки с числовым программным управлением', '28.41.1', 5, true),
('28.41.2', 'Станки фрезерные', 'Фрезерные станки различных типов', '28.41', 4, true),
('28.41.21', 'Станки фрезерные универсальные', 'Универсальные фрезерные станки', '28.41.2', 5, true),
('28.41.22', 'Станки фрезерные с ЧПУ', 'Фрезерные станки с ЧПУ', '28.41.2', 5, true),
('28.41.3', 'Станки сверлильные', 'Сверлильные и расточные станки', '28.41', 4, true),
('28.41.4', 'Станки шлифовальные', 'Шлифовальные, полировальные и доводочные станки', '28.41', 4, true)
ON CONFLICT (code) DO NOTHING;

-- Раздел 25: Готовые металлические изделия
INSERT INTO okpd2_dictionary (code, name, description, parent_code, level, is_active) VALUES
('25', 'Металлические изделия готовые, кроме машин и оборудования', 'Производство готовых металлических изделий', NULL, 1, true),
('25.6', 'Услуги по обработке металлов и нанесению покрытий на металлы', 'Механическая обработка металлических изделий', '25', 2, true),
('25.61', 'Услуги по обработке металлов', 'Токарные, фрезерные, сверлильные работы', '25.6', 3, true),
('25.61.1', 'Услуги токарные', 'Токарная обработка металлов', '25.61', 4, true),
('25.61.2', 'Услуги фрезерные', 'Фрезерная обработка металлов', '25.61', 4, true),
('25.61.3', 'Услуги сверлильные', 'Сверление и расточка отверстий', '25.61', 4, true),
('25.61.4', 'Услуги шлифовальные', 'Шлифование и полирование металлов', '25.61', 4, true)
ON CONFLICT (code) DO NOTHING;

-- Профессии и специальности
INSERT INTO okpd2_dictionary (code, name, description, parent_code, level, is_active) VALUES
('PROF', 'Профессии и специальности', 'Классификатор профессий', NULL, 1, true),
('PROF.01', 'Токарь', 'Токарь по металлу', 'PROF', 2, true),
('PROF.01.1', 'Токарь 2 разряда', 'Токарь 2 разряда', 'PROF.01', 3, true),
('PROF.01.2', 'Токарь 3 разряда', 'Токарь 3 разряда', 'PROF.01', 3, true),
('PROF.01.3', 'Токарь 4 разряда', 'Токарь 4 разряда', 'PROF.01', 3, true),
('PROF.01.4', 'Токарь 5 разряда', 'Токарь 5 разряда', 'PROF.01', 3, true),
('PROF.02', 'Фрезеровщик', 'Фрезеровщик по металлу', 'PROF', 2, true),
('PROF.02.1', 'Фрезеровщик 2 разряда', 'Фрезеровщик 2 разряда', 'PROF.02', 3, true),
('PROF.02.2', 'Фрезеровщик 3 разряда', 'Фрезеровщик 3 разряда', 'PROF.02', 3, true),
('PROF.02.3', 'Фрезеровщик 4 разряда', 'Фрезеровщик 4 разряда', 'PROF.02', 3, true),
('PROF.03', 'Сверловщик', 'Сверловщик по металлу', 'PROF', 2, true),
('PROF.03.1', 'Сверловщик 2 разряда', 'Сверловщик 2 разряда', 'PROF.03', 3, true),
('PROF.03.2', 'Сверловщик 3 разряда', 'Сверловщик 3 разряда', 'PROF.03', 3, true),
('PROF.04', 'Шлифовщик', 'Шлифовщик по металлу', 'PROF', 2, true),
('PROF.04.1', 'Шлифовщик 3 разряда', 'Шлифовщик 3 разряда', 'PROF.04', 3, true),
('PROF.04.2', 'Шлифовщик 4 разряда', 'Шлифовщик 4 разряда', 'PROF.04', 3, true)
ON CONFLICT (code) DO NOTHING;

-- ============================================
-- WORKFLOW TEMPLATES (канонический React Flow формат)
-- ============================================
-- Только 2 рабочих шаблона. Узлы: type=function, data.functionClass/functionName/needParam/paramReturn.

-- Удалить legacy / дубликаты и перезаписать канонические шаблоны
DELETE FROM workflow_templates
WHERE name IN (
  'WF1-WF5.2: Планирование техпроцесса',
  'Простой тест планирования',
  'TECH_PROCESS_DEFAULT_WORKFLOW',
  'Планирование техпроцесса'
);

-- Дедуп по имени (на случай накопившихся копий с другими названиями оставляем как есть;
-- одинаковые имена схлопываем, оставляя минимальный id)
DELETE FROM workflow_templates a
USING workflow_templates b
WHERE a.name = b.name AND a.id > b.id;

-- 1) Короткий шаблон для импорта ТП (имя строго как в TechProcessController)
INSERT INTO workflow_templates (name, description, nodes, edges) VALUES
(
  'TECH_PROCESS_DEFAULT_WORKFLOW',
  'Базовый workflow техпроцесса: подготовка, список операций, завершение',
  $wt_default_nodes$[
    {"id":"n1","type":"function","position":{"x":120,"y":80},"data":{"label":"Подготовить ТП","description":"Подготовить техпроцесс и определить первую операцию","functionClass":"Backend.Services.WorkflowFunctions.PlanningFunctions","functionName":"TechprocessPrepare","needParam":[{"name":"techProcessId","type":"int"}],"paramReturn":"nextOperation","icon":"🏁","config":{}}},
    {"id":"n2","type":"function","position":{"x":120,"y":220},"data":{"label":"Все операции","description":"Получить все операции техпроцесса","functionClass":"Backend.Services.WorkflowFunctions.TechProcessFunctions","functionName":"GetAllOperations","needParam":[{"name":"techProcessId","type":"int"}],"paramReturn":"operations","icon":"📝","config":{}}},
    {"id":"n3","type":"function","position":{"x":120,"y":360},"data":{"label":"Завершить workflow","description":"Завершить выполнение workflow","functionClass":"Backend.Services.WorkflowFunctions.WFListFunctions","functionName":"CompleteWorkflow","needParam":[{"name":"wfId","type":"int"}],"paramReturn":"success","icon":"✔️","config":{}}}
  ]$wt_default_nodes$::jsonb,
  $wt_default_edges$[
    {"id":"e1","source":"n1","target":"n2"},
    {"id":"e2","source":"n2","target":"n3"}
  ]$wt_default_edges$::jsonb
);

-- 2) Полный линейный цикл планирования (без циклов/condition — совместим с converter)
INSERT INTO workflow_templates (name, description, nodes, edges) VALUES
(
  'Планирование техпроцесса',
  'Цикл планирования: запрос ресурсов, СПР, обновление времён операций',
  $wt_plan_nodes$[
    {"id":"p1","type":"function","position":{"x":160,"y":40},"data":{"label":"Подготовить ТП","description":"Подготовить техпроцесс и определить первую операцию","functionClass":"Backend.Services.WorkflowFunctions.PlanningFunctions","functionName":"TechprocessPrepare","needParam":[{"name":"techProcessId","type":"int"}],"paramReturn":"nextOperation","icon":"🏁","config":{}}},
    {"id":"p2","type":"function","position":{"x":160,"y":160},"data":{"label":"Сообщения агентам","description":"Подготовить сообщения ресурсам о работе","functionClass":"Backend.Services.WorkflowFunctions.PlanningFunctions","functionName":"PrepareMessagesToAgentAboutJob","needParam":[{"name":"techProcessId","type":"int"},{"name":"opNo","type":"int"},{"name":"workMessageType","type":"string"}],"paramReturn":"messages","icon":"📋","config":{}}},
    {"id":"p3","type":"function","position":{"x":160,"y":280},"data":{"label":"Отправить готовые","description":"Отправить несколько подготовленных сообщений","functionClass":"Backend.Services.WorkflowFunctions.WorkflowControlFunctions","functionName":"SendReadyMessages","needParam":[{"name":"messages","type":"string"}],"paramReturn":"count","icon":"📬","config":{}}},
    {"id":"p4","type":"function","position":{"x":160,"y":400},"data":{"label":"Пауза workflow","description":"Ожидание ответов ресурсов","functionClass":"Backend.Services.WorkflowFunctions.WorkflowControlFunctions","functionName":"WorkFlowSleep","needParam":[{"name":"wfId","type":"int"},{"name":"delaySeconds","type":"int"}],"paramReturn":"status","icon":"⏸️","config":{}}},
    {"id":"p5","type":"function","position":{"x":160,"y":520},"data":{"label":"Сообщение СПР","description":"Подготовить сообщение для СПР","functionClass":"Backend.Services.WorkflowFunctions.PlanningFunctions","functionName":"PrepareMessageToSPR","needParam":[{"name":"techProcessId","type":"int"},{"name":"opNo","type":"int"},{"name":"typePlan","type":"string"},{"name":"wfId","type":"int"}],"paramReturn":"message","icon":"🎯","config":{}}},
    {"id":"p6","type":"function","position":{"x":160,"y":640},"data":{"label":"Отправить сообщение","description":"Отправить сообщение в СПР","functionClass":"Backend.Services.WorkflowFunctions.WorkflowControlFunctions","functionName":"SendMessage","needParam":[{"name":"messageType","type":"string"},{"name":"routingKey","type":"string"},{"name":"payload","type":"string"}],"paramReturn":"status","icon":"📨","config":{}}},
    {"id":"p7","type":"function","position":{"x":160,"y":760},"data":{"label":"Пауза workflow","description":"Ожидание решения СПР","functionClass":"Backend.Services.WorkflowFunctions.WorkflowControlFunctions","functionName":"WorkFlowSleep","needParam":[{"name":"wfId","type":"int"},{"name":"delaySeconds","type":"int"}],"paramReturn":"status","icon":"⏸️","config":{}}},
    {"id":"p8","type":"function","position":{"x":160,"y":880},"data":{"label":"Обработать ответ СПР","description":"Обработать ответ СПР","functionClass":"Backend.Services.WorkflowFunctions.PlanningFunctions","functionName":"ProcessingSPRResponse","needParam":[{"name":"wfId","type":"int"},{"name":"result","type":"bool"},{"name":"orderNumber","type":"string"},{"name":"operationNumber","type":"int"},{"name":"shiftTime","type":"int"}],"paramReturn":"status","icon":"✅","config":{}}},
    {"id":"p9","type":"function","position":{"x":160,"y":1000},"data":{"label":"Время окончания","description":"Получить время окончания операции","functionClass":"Backend.Services.WorkflowFunctions.PlanningFunctions","functionName":"GetOperationTimeEnd","needParam":[{"name":"orderNumber","type":"string"},{"name":"operationNumber","type":"int"}],"paramReturn":"timeEnd","icon":"⏰","config":{}}},
    {"id":"p10","type":"function","position":{"x":160,"y":1120},"data":{"label":"Следующая операция","description":"Проверить существование следующей операции","functionClass":"Backend.Services.WorkflowFunctions.PlanningFunctions","functionName":"NextOperationExist","needParam":[{"name":"techProcessId","type":"int"},{"name":"currentOpNo","type":"int"}],"paramReturn":"result","icon":"➡️","config":{}}},
    {"id":"p11","type":"function","position":{"x":160,"y":1240},"data":{"label":"Обновить время","description":"Обновить время начала операции","functionClass":"Backend.Services.WorkflowFunctions.PlanningFunctions","functionName":"UpdateOperationTimeStart","needParam":[{"name":"orderNumber","type":"string"},{"name":"operationNumber","type":"int"},{"name":"timeEnd","type":"string"},{"name":"deltaTime","type":"int"}],"paramReturn":"success","icon":"🕐","config":{}}},
    {"id":"p12","type":"function","position":{"x":160,"y":1360},"data":{"label":"Завершить workflow","description":"Завершить выполнение workflow","functionClass":"Backend.Services.WorkflowFunctions.WFListFunctions","functionName":"CompleteWorkflow","needParam":[{"name":"wfId","type":"int"}],"paramReturn":"success","icon":"✔️","config":{}}}
  ]$wt_plan_nodes$::jsonb,
  $wt_plan_edges$[
    {"id":"e1","source":"p1","target":"p2"},
    {"id":"e2","source":"p2","target":"p3"},
    {"id":"e3","source":"p3","target":"p4"},
    {"id":"e4","source":"p4","target":"p5"},
    {"id":"e5","source":"p5","target":"p6"},
    {"id":"e6","source":"p6","target":"p7"},
    {"id":"e7","source":"p7","target":"p8"},
    {"id":"e8","source":"p8","target":"p9"},
    {"id":"e9","source":"p9","target":"p10"},
    {"id":"e10","source":"p10","target":"p11"},
    {"id":"e11","source":"p11","target":"p12"}
  ]$wt_plan_edges$::jsonb
);

-- Уникальность имени шаблона (после очистки дублей)
CREATE UNIQUE INDEX IF NOT EXISTS uq_workflow_templates_name ON workflow_templates(name);


-- ============================================
-- OPTIONAL: OKPD-2 CODE ASSIGNMENT
-- ============================================
-- Раскомментируйте для автоматического назначения OKPD-2 кодов существующим агентам

-- Equipment agents
-- UPDATE agents SET okpd2_code = '28.41.11', agent_type = 'resource' WHERE type = 'equipment' AND okpd2_code IS NULL;
-- UPDATE agents SET okpd2_code = '28.41.12', agent_type = 'resource' WHERE type = 'equipment' AND okpd2_code IS NULL;
-- UPDATE agents SET okpd2_code = '28.41.21', agent_type = 'resource' WHERE type = 'equipment' AND okpd2_code IS NULL;
-- UPDATE agents SET okpd2_code = '28.41.22', agent_type = 'resource' WHERE type = 'equipment' AND okpd2_code IS NULL;
-- UPDATE agents SET okpd2_code = '28.41.3', agent_type = 'resource' WHERE type = 'equipment' AND okpd2_code IS NULL;

-- Personnel agents
-- UPDATE agents SET okpd2_code = 'PROF.01.3', agent_type = 'resource' WHERE type = 'personnel' AND okpd2_code IS NULL;
-- UPDATE agents SET okpd2_code = 'PROF.01.4', agent_type = 'resource' WHERE type = 'personnel' AND okpd2_code IS NULL;
-- UPDATE agents SET okpd2_code = 'PROF.04.1', agent_type = 'resource' WHERE type = 'personnel' AND okpd2_code IS NULL;
-- UPDATE agents SET okpd2_code = 'PROF.03.1', agent_type = 'resource' WHERE type = 'personnel' AND okpd2_code IS NULL;

-- Tech process agents
-- UPDATE agents SET agent_type = 'techprocess' WHERE tech_process_id IS NOT NULL AND agent_type != 'techprocess';

-- ============================================
-- HOSTS (места развёртывания агентов)
-- ============================================

CREATE TABLE IF NOT EXISTS hosts (
  id SERIAL PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  type VARCHAR(50) NOT NULL DEFAULT 'cluster',
  status VARCHAR(50) NOT NULL DEFAULT 'offline',
  max_agents INTEGER NOT NULL DEFAULT 100,
  deploy_target VARCHAR(50) NOT NULL DEFAULT 'docker',
  rmq_username VARCHAR(255),
  rmq_password_enc TEXT,
  queue_name VARCHAR(255) NOT NULL DEFAULT '',
  routing_key VARCHAR(255) NOT NULL DEFAULT '',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT hosts_type_check CHECK (type IN ('cluster', 'single', 'specific')),
  CONSTRAINT hosts_deploy_check CHECK (deploy_target IN ('docker', 'machine', 'custom'))
);

CREATE INDEX IF NOT EXISTS idx_hosts_type ON hosts(type);
CREATE INDEX IF NOT EXISTS idx_hosts_name ON hosts(name);

-- Default Cluster для миграции существующих агентов
INSERT INTO hosts (id, name, type, status, max_agents, deploy_target, rmq_username, queue_name, routing_key)
VALUES (1, 'Default Cluster', 'cluster', 'offline', 1000, 'docker', 'host_1', 'Host.1', 'Host.1')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('hosts', 'id'), GREATEST((SELECT MAX(id) FROM hosts), 1));

-- agents.host_id (обязательная привязка к Host)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'agents' AND column_name = 'host_id'
    ) THEN
        ALTER TABLE agents ADD COLUMN host_id INTEGER;
        UPDATE agents SET host_id = 1 WHERE host_id IS NULL;
        ALTER TABLE agents ALTER COLUMN host_id SET NOT NULL;
        ALTER TABLE agents
          ADD CONSTRAINT fk_agents_host
          FOREIGN KEY (host_id) REFERENCES hosts(id) ON DELETE RESTRICT;
        RAISE NOTICE '✓ Колонка agents.host_id добавлена';
    ELSE
        UPDATE agents SET host_id = 1 WHERE host_id IS NULL;
        RAISE NOTICE '✓ Колонка agents.host_id уже существует';
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_agents_host_id ON agents(host_id);

-- ============================================
-- ФИНАЛЬНАЯ ПРОВЕРКА
-- ============================================

-- App settings (AI API key etc., secrets stored encrypted by backend)
CREATE TABLE IF NOT EXISTS app_settings (
    key VARCHAR(100) PRIMARY KEY,
    value TEXT,
    is_secret BOOLEAN NOT NULL DEFAULT FALSE,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Drawing analyses (Gemini vision → labor intensity)
CREATE TABLE IF NOT EXISTS drawing_analyses (
    id SERIAL PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    content_type VARCHAR(100),
    status VARCHAR(50) NOT NULL DEFAULT 'completed',
    part_name TEXT,
    drawing_number VARCHAR(128),
    material VARCHAR(256),
    total_labor_hours NUMERIC(12, 4),
    confidence VARCHAR(32),
    result_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    model_name VARCHAR(128),
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_drawing_analyses_created_at ON drawing_analyses (created_at DESC);
CREATE INDEX IF NOT EXISTS idx_drawing_analyses_status ON drawing_analyses (status);

DO $$
DECLARE
    table_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO table_count
    FROM pg_tables
    WHERE schemaname = 'public'
    AND tablename IN ('tree_nodes', 'agents', 'agent_parameters', 'file_uploads',
                      'statistics', 'parameter_dictionary', 'okpd2_dictionary', 'blank_okpd2_mapping',
                      'workflow_templates', 'workflow_functions', 'users', 'wf_list', 'timetable', 'hosts',
                      'drawing_analyses');

    IF table_count >= 14 THEN
        RAISE NOTICE '';
        RAISE NOTICE '========================================';
        RAISE NOTICE '✓ База данных создана успешно!';
        RAISE NOTICE '========================================';
        RAISE NOTICE 'Создано таблиц (ядро): %', table_count;
        RAISE NOTICE '';
        RAISE NOTICE 'Включает:';
        RAISE NOTICE '- Hosts (cluster/single/specific)';
        RAISE NOTICE '- Управление агентами и деревом';
        RAISE NOTICE '- Workflow templates (2 шаблона) и функции (46 функций)';
        RAISE NOTICE '- Выполнение workflow (wf_list)';
        RAISE NOTICE '- Расписание работ (timetable)';
        RAISE NOTICE '- Техпроцессы и операции';
        RAISE NOTICE '- ОКПД-2 классификатор';
        RAISE NOTICE '- Маппинг названий на ОКПД-2 (blank_okpd2_mapping)';
        RAISE NOTICE '- Авторизация пользователей';
        RAISE NOTICE '- Анализ чертежей (drawing_analyses)';
        RAISE NOTICE '';
        RAISE NOTICE 'Теперь можно:';
        RAISE NOTICE '1. Импортировать техпроцессы из Excel (docs/metalshop_tp_50_normative.xlsx)';
        RAISE NOTICE '2. Настроить Backend/appsettings.json (PostgreSQL + RabbitMQ + Gemini:ApiKey)';
        RAISE NOTICE '3. Запустить Backend: cd Backend && dotnet run';
        RAISE NOTICE '4. Запустить Frontend: npm run dev';
        RAISE NOTICE '';
    ELSE
        RAISE WARNING 'Создано только % таблиц из 14! Проверьте ошибки выше.', table_count;
    END IF;
END $$;
