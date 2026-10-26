-- Исправление колонки okpd2_code в таблице agents
-- Обновляем okpd2_code из параметра "Код ОКПД-2"
-- Выполнять в PostgreSQL (pgAdmin или psql)

-- ============================================
-- Обновляем okpd2_code для equipment и instrument
-- ============================================

UPDATE agents a
SET okpd2_code = ap.value
FROM agent_parameters ap
WHERE a.id = ap.agent_id
  AND ap.name = 'Код ОКПД-2'
  AND ap.value SIMILAR TO '[0-9.]+'
  AND (a.okpd2_code IS NULL OR a.okpd2_code = '' OR a.okpd2_code NOT SIMILAR TO '[0-9.]+');

-- ============================================
-- ПРОВЕРКА результата
-- ============================================

-- Показываем агентов с некорректным okpd2_code
SELECT a.id, a.name, a.type, a.okpd2_code as agent_okpd2, ap.value as param_okpd2
FROM agents a
JOIN agent_parameters ap ON a.id = ap.agent_id AND ap.name = 'Код ОКПД-2'
WHERE a.type IN ('equipment', 'instrument')
  AND (a.okpd2_code IS NULL OR a.okpd2_code = '' OR a.okpd2_code NOT SIMILAR TO '[0-9.]+')
ORDER BY a.id
LIMIT 20;

-- Итоговая статистика
SELECT
    a.type,
    COUNT(*) as total_agents,
    COUNT(CASE WHEN a.okpd2_code SIMILAR TO '[0-9.]+' THEN 1 END) as agents_with_numeric_okpd2
FROM agents a
WHERE a.type IN ('equipment', 'instrument')
GROUP BY a.type;
