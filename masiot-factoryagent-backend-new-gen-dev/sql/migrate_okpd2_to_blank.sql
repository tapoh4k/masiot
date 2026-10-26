-- Миграция данных из okpd2_dictionary в blank_okpd2_mapping
-- Запускать после того, как таблица blank_okpd2_mapping уже создана
-- Выполнять в PostgreSQL (pgAdmin или psql)

-- 0. Добавляем поле updated_at если его нет
ALTER TABLE blank_okpd2_mapping
ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

-- 1. Переносим данные из okpd2_dictionary в blank_okpd2_mapping
-- code → okpd2_code, name → keywords, description → description
INSERT INTO blank_okpd2_mapping (keywords, okpd2_code, description, created_at, updated_at)
SELECT
    name AS keywords,
    code AS okpd2_code,
    description,
    created_at,
    COALESCE(updated_at, created_at) AS updated_at
FROM okpd2_dictionary
WHERE is_active = true
ON CONFLICT (id) DO NOTHING;

-- 2. Проверяем количество перенесенных записей
SELECT COUNT(*) AS "Перенесено записей" FROM blank_okpd2_mapping;

-- 3. Удаляем таблицу okpd2_dictionary
DROP TABLE IF EXISTS okpd2_dictionary CASCADE;

-- 4. Проверяем результат - все записи в новой таблице
SELECT id, keywords, okpd2_code, description, created_at
FROM blank_okpd2_mapping
ORDER BY okpd2_code;

-- ============================================
-- ИНСТРУКЦИЯ ПО ПРИМЕНЕНИЮ:
-- ============================================
-- 1. Остановить backend приложение
-- 2. Выполнить этот SQL скрипт в pgAdmin:
--    - Подключиться к базе данных
--    - Открыть Query Tool
--    - Вставить этот скрипт и выполнить (F5)
-- 3. Применить миграцию EF Core:
--    cd Backend
--    dotnet ef database update
-- 4. Запустить backend
-- ============================================
