-- Добавление записей в blank_okpd2_mapping для инструмента
-- Выполнять в PostgreSQL (pgAdmin или psql)

-- ============================================
-- Добавляем коды ОКПД-2 для режущего инструмента
-- ============================================

INSERT INTO blank_okpd2_mapping (keywords, okpd2_code, description, created_at, updated_at)
VALUES
    ('Сверло', '25.73.40.110', 'Сверла спиральные и твердосплавные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Сверла спиральные', '25.73.40.110', 'Сверла спиральные из быстрорежущей стали', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Сверла твердосплавные', '25.73.40.110', 'Сверла с твердосплавными напайками', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Фреза', '25.73.40.120', 'Фрезы концевые, торцевые, дисковые', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Фрезы концевые', '25.73.40.120', 'Фрезы концевые для металлообработки', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Фрезы торцевые', '25.73.40.120', 'Фрезы торцевые насадные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Фрезы дисковые', '25.73.40.120', 'Фрезы дисковые для пазов и прорезей', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Резец', '25.73.40.130', 'Резцы токарные проходные, отрезные, расточные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Резцы токарные', '25.73.40.130', 'Резцы для токарных станков', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Резцы отрезные', '25.73.40.130', 'Резцы отрезные для проточки канавок', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Резцы расточные', '25.73.40.130', 'Резцы расточные для обработки отверстий', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Развертка', '25.73.40.140', 'Развертки для окончательной обработки отверстий', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Развертки цилиндрические', '25.73.40.140', 'Развертки цилиндрические машинные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Метчик', '25.73.40.150', 'Метчики для нарезания внутренней резьбы', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Метчики машинные', '25.73.40.150', 'Метчики для станков с ЧПУ', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Метчики ручные', '25.73.40.150', 'Метчики комплекты для ручного инструмента', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Плашка', '25.73.40.160', 'Плашки для нарезания наружной резьбы', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Плашки круглые', '25.73.40.160', 'Плашки круглые разрезные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Цанга', '25.73.50.110', 'Цанги патронные для зажима инструмента', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Цанги ER', '25.73.50.110', 'Цанги стандарта ER для патрон-оправок', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Патрон токарный', '25.73.50.120', 'Патроны токарные трехкулачковые, четырехкулачковые', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Патрон цанговый', '25.73.50.120', 'Патроны цанговые для фрезерных станков', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Тиски станочные', '25.73.50.130', 'Тиски машинные для фрезерных и сверлильных станков', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Протяжка', '25.73.40.170', 'Протяжки для обработки внутренних и наружных поверхностей', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Протяжки внутренние', '25.73.40.170', 'Протяжки для обработки отверстий', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Зенковка', '25.73.40.115', 'Зенковки для обработки углублений', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Зенкер', '25.73.40.115', 'Зенкеры для обработки отверстий', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ============================================
-- ПРОВЕРКА: показываем добавленные записи
-- ============================================
SELECT id, keywords, okpd2_code, description
FROM blank_okpd2_mapping
WHERE keywords IN (
    'Сверло', 'Фреза', 'Резец', 'Метчик', 'Плашка',
    'Развертка', 'Цанга', 'Патрон токарный', 'Тиски станочные',
    'Протяжка', 'Зенковка', 'Зенкер'
)
ORDER BY id DESC;
