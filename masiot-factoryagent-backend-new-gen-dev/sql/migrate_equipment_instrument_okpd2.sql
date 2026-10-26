-- Миграция ОКПД-2 кодов для агентов "Оборудование" (equipment) и "Инструмент" (instrument)
-- Выполнять в PostgreSQL (pgAdmin или psql)
-- Версия 2: улучшенная логика поиска с приоритетом более точных совпадений

-- ============================================
-- ЧАСТЬ 1: Оборудование (equipment)
-- ============================================

-- 1.1 Сбрасываем некорректные значения (общие коды типа "28", "25" и т.д.)
UPDATE agent_parameters ap
SET value = ''
FROM agents a
WHERE ap.agent_id = a.id
  AND a.type = 'equipment'
  AND ap.name = 'Код ОКПД-2'
  AND ap.value IN ('28', '25', '25.6', '25.61', '28.4');

-- 1.2 Обновляем параметр "Код ОКПД-2" для equipment с приоритетом более точных совпадений
-- Используем подзапрос с приоритетами
UPDATE agent_parameters ap
SET value = (
    SELECT b.okpd2_code
    FROM blank_okpd2_mapping b
    WHERE (
        -- Токарные станки - самый высокий приоритет
        (a.name ILIKE '%токарн%' AND b.keywords ILIKE '%токарн%') OR
        -- Фрезерные станки
        (a.name ILIKE '%фрезерн%' AND b.keywords ILIKE '%фрезерн%') OR
        -- Сверлильные станки
        (a.name ILIKE '%сверлильн%' AND b.keywords ILIKE '%сверлильн%') OR
        -- Шлифовальные станки
        (a.name ILIKE '%шлифовальн%' AND b.keywords ILIKE '%шлифовальн%')
    )
    -- Исключаем общие коды
    AND b.okpd2_code NOT IN ('28', '25', '25.6', '25.61', '28.4')
    ORDER BY
        -- Приоритет: более длинные keywords (более специфичные) primero
        LENGTH(b.keywords) DESC,
        LENGTH(b.okpd2_code) DESC
    LIMIT 1
)
FROM agents a
WHERE ap.agent_id = a.id
  AND a.type = 'equipment'
  AND ap.name = 'Код ОКПД-2'
  AND (ap.value IS NULL OR ap.value = '' OR ap.value NOT SIMILAR TO '[0-9.]+');

-- 1.3 Проверяем результат для equipment
SELECT 'equipment' as type, a.id, a.name, ap.value as okpd2_code
FROM agents a
JOIN agent_parameters ap ON a.id = ap.agent_id
WHERE a.type = 'equipment'
  AND ap.name = 'Код ОКПД-2'
  AND ap.value IS NOT NULL
  AND ap.value != ''
ORDER BY a.id
LIMIT 10;

-- ============================================
-- ЧАСТЬ 2: Инструмент (instrument)
-- ============================================

-- 2.1 Сбрасываем некорректные значения
UPDATE agent_parameters ap
SET value = ''
FROM agents a
WHERE ap.agent_id = a.id
  AND a.type = 'instrument'
  AND ap.name = 'Код ОКПД-2'
  AND (ap.value LIKE 'PROF%' OR ap.value IN ('28', '25', '28.4', '28.41', '28.41.2'));

-- 2.2 Обновляем параметр "Код ОКПД-2" для instrument с приоритетом более точных совпадений
UPDATE agent_parameters ap
SET value = (
    SELECT b.okpd2_code
    FROM blank_okpd2_mapping b
    WHERE (
        -- Сверла
        (a.name ILIKE '%сверло%' AND b.keywords = 'Сверло') OR
        (a.name ILIKE '%сверло%' AND b.keywords ILIKE '%сверла%') OR
        -- Фрезы
        (a.name ILIKE '%фреза%' AND b.keywords = 'Фреза') OR
        (a.name ILIKE '%фрез%' AND b.keywords ILIKE '%фрезы%') OR
        -- Резцы
        (a.name ILIKE '%резец%' AND b.keywords = 'Резец') OR
        (a.name ILIKE '%резцы%' AND b.keywords ILIKE '%резцы%') OR
        -- Развертки
        (a.name ILIKE '%развертк%' AND b.keywords ILIKE '%развертк%') OR
        -- Метчики
        (a.name ILIKE '%метчик%' AND b.keywords = 'Метчик') OR
        -- Плашки
        (a.name ILIKE '%плашк%' AND b.keywords = 'Плашка') OR
        -- Цанги
        (a.name ILIKE '%цанг%' AND b.keywords ILIKE '%цанг%') OR
        -- Патроны
        (a.name ILIKE '%патрон%' AND b.keywords ILIKE '%патрон%') OR
        -- Тиски
        (a.name ILIKE '%тиск%' AND b.keywords ILIKE '%тиск%') OR
        -- Протяжки
        (a.name ILIKE '%протяжк%' AND b.keywords ILIKE '%протяжк%') OR
        -- Зенковки/зенкеры
        (a.name ILIKE '%зенковк%' AND b.keywords ILIKE '%зенковк%') OR
        (a.name ILIKE '%зенкер%' AND b.keywords ILIKE '%зенкер%')
    )
    -- Исключаем коды профессий
    AND b.okpd2_code NOT LIKE 'PROF%'
    ORDER BY
        LENGTH(b.keywords) DESC,
        LENGTH(b.okpd2_code) DESC
    LIMIT 1
)
FROM agents a
WHERE ap.agent_id = a.id
  AND a.type = 'instrument'
  AND ap.name = 'Код ОКПД-2'
  AND (ap.value IS NULL OR ap.value = '' OR ap.value NOT SIMILAR TO '[0-9.]+');

-- 2.3 Проверяем результат для instrument
SELECT 'instrument' as type, a.id, a.name, ap.value as okpd2_code
FROM agents a
JOIN agent_parameters ap ON a.id = ap.agent_id
WHERE a.type = 'instrument'
  AND ap.name = 'Код ОКПД-2'
  AND ap.value IS NOT NULL
  AND ap.value != ''
ORDER BY a.id
LIMIT 10;

-- ============================================
-- ЧАСТЬ 3: Обновление колонки okpd2_code в таблице agents
-- ============================================

-- 3.1 Обновляем okpd2_code в таблице agents для equipment
UPDATE agents a
SET okpd2_code = (
    SELECT b.okpd2_code
    FROM blank_okpd2_mapping b
    WHERE (
        (a.name ILIKE '%токарн%' AND b.keywords ILIKE '%токарн%') OR
        (a.name ILIKE '%фрезерн%' AND b.keywords ILIKE '%фрезерн%') OR
        (a.name ILIKE '%сверлильн%' AND b.keywords ILIKE '%сверлильн%') OR
        (a.name ILIKE '%шлифовальн%' AND b.keywords ILIKE '%шлифовальн%')
    )
    AND b.okpd2_code NOT IN ('28', '25', '25.6', '25.61', '28.4')
    ORDER BY LENGTH(b.keywords) DESC, LENGTH(b.okpd2_code) DESC
    LIMIT 1
)
WHERE a.type = 'equipment'
  AND (a.okpd2_code IS NULL OR a.okpd2_code = '' OR a.okpd2_code NOT SIMILAR TO '[0-9.]+');

-- 3.2 Обновляем okpd2_code в таблице agents для instrument
UPDATE agents a
SET okpd2_code = (
    SELECT b.okpd2_code
    FROM blank_okpd2_mapping b
    WHERE (
        (a.name ILIKE '%сверло%' AND b.keywords ILIKE '%сверло%') OR
        (a.name ILIKE '%фрез%' AND b.keywords ILIKE '%фрез%') OR
        (a.name ILIKE '%резец%' AND b.keywords ILIKE '%резец%') OR
        (a.name ILIKE '%метчик%' AND b.keywords ILIKE '%метчик%') OR
        (a.name ILIKE '%плашк%' AND b.keywords ILIKE '%плашк%') OR
        (a.name ILIKE '%развертк%' AND b.keywords ILIKE '%развертк%') OR
        (a.name ILIKE '%цанг%' AND b.keywords ILIKE '%цанг%') OR
        (a.name ILIKE '%патрон%' AND b.keywords ILIKE '%патрон%') OR
        (a.name ILIKE '%тиск%' AND b.keywords ILIKE '%тиск%') OR
        (a.name ILIKE '%протяжк%' AND b.keywords ILIKE '%протяжк%')
    )
    AND b.okpd2_code NOT LIKE 'PROF%'
    ORDER BY LENGTH(b.keywords) DESC, LENGTH(b.okpd2_code) DESC
    LIMIT 1
)
WHERE a.type = 'instrument'
  AND (a.okpd2_code IS NULL OR a.okpd2_code = '' OR a.okpd2_code NOT SIMILAR TO '[0-9.]+');

-- ============================================
-- ФИНАЛЬНАЯ ПРОВЕРКА
-- ============================================

-- Показываем статистику обновлений
SELECT
    a.type,
    COUNT(*) as total_agents,
    COUNT(CASE WHEN ap.value SIMILAR TO '[0-9.]+' THEN 1 END) as agents_with_numeric_okpd2
FROM agents a
LEFT JOIN agent_parameters ap ON a.id = ap.agent_id AND ap.name = 'Код ОКПД-2'
WHERE a.type IN ('equipment', 'instrument')
GROUP BY a.type;

-- Примеры обновленных данных
SELECT
    a.type,
    a.id,
    a.name,
    a.okpd2_code as agent_okpd2,
    ap.value as param_okpd2
FROM agents a
LEFT JOIN agent_parameters ap ON a.id = ap.agent_id AND ap.name = 'Код ОКПД-2'
WHERE a.type IN ('equipment', 'instrument')
  AND (ap.value SIMILAR TO '[0-9.]+' OR a.okpd2_code SIMILAR TO '[0-9.]+')
ORDER BY a.type, a.id
LIMIT 20;
