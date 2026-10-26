-- Исправление значений ОКПД-2 для агентов equipment и instrument
-- Перенос значений из "Код ОКП" и "Код классификатора" в "Код ОКПД-2"
-- Выполнять в PostgreSQL (pgAdmin или psql)

-- ============================================
-- ЧАСТЬ 1: Оборудование (equipment)
-- ============================================

-- 1.1 Обновляем "Код ОКПД-2" из "Код ОКП" для equipment
UPDATE agent_parameters ap
SET value = (
    SELECT old_param.value
    FROM agent_parameters old_param
    WHERE old_param.agent_id = ap.agent_id
      AND old_param.name = 'Код ОКП'
)
FROM agents a
WHERE ap.agent_id = a.id
  AND a.type = 'equipment'
  AND ap.name = 'Код ОКПД-2'
  AND (ap.value IS NULL OR ap.value = '' OR ap.value NOT SIMILAR TO '[0-9.]+');

-- 1.2 Удаляем "Код ОКП" после переноса
DELETE FROM agent_parameters
WHERE name = 'Код ОКП'
  AND agent_id IN (SELECT id FROM agents WHERE type = 'equipment');

-- ============================================
-- ЧАСТЬ 2: Инструмент (instrument)
-- ============================================

-- 2.1 Обновляем "Код ОКПД-2" из "Код классификатора" для instrument
UPDATE agent_parameters ap
SET value = (
    SELECT old_param.value
    FROM agent_parameters old_param
    WHERE old_param.agent_id = ap.agent_id
      AND old_param.name = 'Код классификатора'
)
FROM agents a
WHERE ap.agent_id = a.id
  AND a.type = 'instrument'
  AND ap.name = 'Код ОКПД-2'
  AND (ap.value IS NULL OR ap.value = '' OR ap.value NOT SIMILAR TO '[0-9.]+');

-- 2.2 Удаляем "Код классификатора" после переноса
DELETE FROM agent_parameters
WHERE name = 'Код классификатора'
  AND agent_id IN (SELECT id FROM agents WHERE type = 'instrument');

-- ============================================
-- ФИНАЛЬНАЯ ПРОВЕРКА
-- ============================================

-- Показываем примеры обновлённых данных для equipment
SELECT 'equipment' as type, a.id, a.name, ap.value as okpd2_code
FROM agents a
JOIN agent_parameters ap ON a.id = ap.agent_id
WHERE a.type = 'equipment'
  AND ap.name = 'Код ОКПД-2'
  AND ap.value SIMILAR TO '[0-9.]+'
ORDER BY a.id
LIMIT 10;

-- Показываем примеры обновлённых данных для instrument
SELECT 'instrument' as type, a.id, a.name, ap.value as okpd2_code
FROM agents a
JOIN agent_parameters ap ON a.id = ap.agent_id
WHERE a.type = 'instrument'
  AND ap.name = 'Код ОКПД-2'
  AND ap.value SIMILAR TO '[0-9.]+'
ORDER BY a.id
LIMIT 10;

-- Проверяем, что старые поля удалены
SELECT
    COUNT(CASE WHEN name = 'Код ОКП' THEN 1 END) as kod_okp_count,
    COUNT(CASE WHEN name = 'Код классификатора' THEN 1 END) as kod_classificator_count
FROM agent_parameters
WHERE name IN ('Код ОКП', 'Код классификатора');

-- Итоговая статистика
SELECT
    a.type,
    COUNT(*) as total_agents,
    COUNT(CASE WHEN ap.value SIMILAR TO '[0-9.]+' THEN 1 END) as agents_with_numeric_okpd2
FROM agents a
LEFT JOIN agent_parameters ap ON a.id = ap.agent_id AND ap.name = 'Код ОКПД-2'
WHERE a.type IN ('equipment', 'instrument')
GROUP BY a.type;
