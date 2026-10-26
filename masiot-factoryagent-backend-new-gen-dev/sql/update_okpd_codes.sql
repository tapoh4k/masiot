-- Скрипт для переноса кодов из "Код ОКП" и "Код классификатора" в "Код ОКПД-2"
-- Выполнять в PostgreSQL (pgAdmin или psql)

-- ============================================
-- Создаём временную таблицу для сохранения значений перед удалением
-- ============================================

-- Создаём временную таблицу с текущими значениями "Код ОКП" и "Код классификатора"
CREATE TEMP TABLE temp_okp_values AS
SELECT
    ap.agent_id,
    ap.name as param_name,
    ap.value as param_value
FROM agent_parameters ap
WHERE ap.name IN ('Код ОКП', 'Код классификатора');

-- Проверяем, что данные сохранены
SELECT COUNT(*) as "Сохранено записей" FROM temp_okp_values;

-- ============================================
-- ЧАСТЬ 1: Оборудование (equipment)
-- ============================================

-- 1.1 Обновляем "Код ОКПД-2" из временной таблицы
UPDATE agent_parameters ap
SET value = t.param_value
FROM temp_okp_values t
WHERE ap.agent_id = t.agent_id
  AND t.param_name = 'Код ОКП'
  AND ap.name = 'Код ОКПД-2';

-- 1.2 Проверяем сколько обновлено
SELECT COUNT(*) as "Обновлено equipment"
FROM agent_parameters ap
JOIN agents a ON ap.agent_id = a.id
WHERE ap.name = 'Код ОКПД-2'
  AND a.type = 'equipment'
  AND ap.value IS NOT NULL
  AND ap.value != '';

-- ============================================
-- ЧАСТЬ 2: Инструмент (instrument)
-- ============================================

-- 2.1 Обновляем "Код ОКПД-2" из временной таблицы
UPDATE agent_parameters ap
SET value = t.param_value
FROM temp_okp_values t
WHERE ap.agent_id = t.agent_id
  AND t.param_name = 'Код классификатора'
  AND ap.name = 'Код ОКПД-2';

-- 2.2 Проверяем сколько обновлено
SELECT COUNT(*) as "Обновлено instrument"
FROM agent_parameters ap
JOIN agents a ON ap.agent_id = a.id
WHERE ap.name = 'Код ОКПД-2'
  AND a.type = 'instrument'
  AND ap.value IS NOT NULL
  AND ap.value != '';

-- ============================================
-- Очистка: удаляем старые параметры (если они ещё есть)
-- ============================================
DELETE FROM agent_parameters WHERE name IN ('Код ОКП', 'Код классификатора');

-- ============================================
-- ФИНАЛЬНАЯ ПРОВЕРКА
-- ============================================

-- Показываем примеры обновлённых данных для equipment
SELECT 'equipment' as type, a.id, a.name, ap.value as okpd2_code
FROM agents a
JOIN agent_parameters ap ON a.id = ap.agent_id
WHERE a.type = 'equipment'
  AND ap.name = 'Код ОКПД-2'
  AND ap.value IS NOT NULL
  AND ap.value != ''
ORDER BY a.id
LIMIT 5;

-- Показываем примеры обновлённых данных для instrument
SELECT 'instrument' as type, a.id, a.name, ap.value as okpd2_code
FROM agents a
JOIN agent_parameters ap ON a.id = ap.agent_id
WHERE a.type = 'instrument'
  AND ap.name = 'Код ОКПД-2'
  AND ap.value IS NOT NULL
  AND ap.value != ''
ORDER BY a.id
LIMIT 5;
