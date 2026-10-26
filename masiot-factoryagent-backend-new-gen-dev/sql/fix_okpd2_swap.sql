-- Исправление: перенос настоящего кода ОКПД-2 из параметров в okpd2_code и "Код ОКПД-2"
-- Проблема: okpd2_code содержит название, а настоящий код в разных параметрах
-- Выполнять в PostgreSQL (pgAdmin или psql)

-- ============================================
-- 1. Оборудование: перенос из "Код ОКП"
-- ============================================

UPDATE agents a
SET okpd2_code = ap.value
FROM agent_parameters ap
WHERE a.id = ap.agent_id
  AND a.type = 'equipment'
  AND ap.name = 'Код ОКП'
  AND ap.value SIMILAR TO '[0-9.]+'
  AND (a.okpd2_code IS NULL OR a.okpd2_code = '' OR a.okpd2_code NOT SIMILAR TO '[0-9.]+');

-- ============================================
-- 2. Инструмент: перенос из "Код классификатора"
-- ============================================

UPDATE agents a
SET okpd2_code = ap.value
FROM agent_parameters ap
WHERE a.id = ap.agent_id
  AND a.type = 'instrument'
  AND ap.name = 'Код классификатора'
  AND ap.value SIMILAR TO '[0-9.]+'
  AND (a.okpd2_code IS NULL OR a.okpd2_code = '' OR a.okpd2_code NOT SIMILAR TO '[0-9.]+');

-- ============================================
-- 3. Персонал: перенос из "Код профессии (ОКПДТР)"
-- ============================================

UPDATE agents a
SET okpd2_code = ap.value
FROM agent_parameters ap
WHERE a.id = ap.agent_id
  AND a.type = 'personnel'
  AND ap.name = 'Код профессии (ОКПДТР)'
  AND ap.value SIMILAR TO '[0-9.]+'
  AND (a.okpd2_code IS NULL OR a.okpd2_code = '' OR a.okpd2_code NOT SIMILAR TO '[0-9.]+');

-- ============================================
-- 4. Обновляем параметр "Код ОКПД-2" настоящим кодом (для всех типов)
-- ============================================

UPDATE agent_parameters ap
SET value = a.okpd2_code
FROM agents a
WHERE ap.agent_id = a.id
  AND ap.name = 'Код ОКПД-2'
  AND a.okpd2_code SIMILAR TO '[0-9.]+'
  AND (ap.value IS NULL OR ap.value = '' OR ap.value NOT SIMILAR TO '[0-9.]+');

-- ============================================
-- 5. Удаляем устаревшие параметры
-- ============================================

DELETE FROM agent_parameters
WHERE name IN ('Код ОКП', 'Код классификатора');

-- ============================================
-- ПРОВЕРКА
-- ============================================

-- Агенты с корректным okpd2_code (числовой)
SELECT a.type, COUNT(*) as ok_count
FROM agents a
WHERE a.okpd2_code SIMILAR TO '[0-9.]+'
GROUP BY a.type;

-- Агенты с некорректным okpd2_code (остались названия)
SELECT a.id, a.name, a.type, a.okpd2_code
FROM agents a
WHERE a.okpd2_code NOT SIMILAR TO '[0-9.]+'
  AND a.okpd2_code IS NOT NULL
  AND a.okpd2_code != ''
ORDER BY a.type, a.id;

-- Остаточные устаревшие параметры
SELECT name, COUNT(*) FROM agent_parameters
WHERE name IN ('Код ОКП', 'Код классификатора')
GROUP BY name;
