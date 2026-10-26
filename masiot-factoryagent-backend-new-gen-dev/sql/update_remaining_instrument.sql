-- Обновление оставшихся агентов instrument (державки, штангенциркули)
-- Выполнять в PostgreSQL (pgAdmin или psql)

UPDATE agent_parameters ap
SET value = b.okpd2_code
FROM agents a
JOIN blank_okpd2_mapping b ON (
    (a.name ILIKE '%державк%' AND b.keywords ILIKE '%державк%') OR
    (a.name ILIKE '%штанген%' AND b.keywords ILIKE '%штанген%')
)
WHERE ap.agent_id = a.id
  AND a.type = 'instrument'
  AND ap.name = 'Код ОКПД-2'
  AND (ap.value IS NULL OR ap.value = '');

-- Проверка результата
SELECT a.id, a.name, ap.value as okpd2_code
FROM agents a
JOIN agent_parameters ap ON a.id = ap.agent_id
WHERE a.type = 'instrument'
  AND ap.name = 'Код ОКПД-2'
ORDER BY a.id
LIMIT 15;

-- Итоговая статистика
SELECT
    a.type,
    COUNT(*) as total_agents,
    COUNT(CASE WHEN ap.value SIMILAR TO '[0-9.]+' THEN 1 END) as agents_with_numeric_okpd2,
    COUNT(CASE WHEN ap.value IS NULL OR ap.value = '' THEN 1 END) as agents_without_okpd2
FROM agents a
LEFT JOIN agent_parameters ap ON a.id = ap.agent_id AND ap.name = 'Код ОКПД-2'
WHERE a.type IN ('equipment', 'instrument')
GROUP BY a.type;
