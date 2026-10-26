-- Добавление записей для державок и измерительного инструмента
-- Выполнять в PostgreSQL (pgAdmin или psql)

INSERT INTO blank_okpd2_mapping (keywords, okpd2_code, description, created_at, updated_at)
VALUES
    ('Державка', '25.73.40.130', 'Державки для сменного режущего инструмента', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Державки для резцов', '25.73.40.130', 'Державки для крепления резцов', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Штангенциркуль', '25.73.40.180', 'Штангенинструмент измерительный', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Штангенциркули', '25.73.40.180', 'Штангенциркули и измерительный инструмент', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Штангенрейсмас', '25.73.40.180', 'Штангенрейсмасы измерительные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Штангенглубиномер', '25.73.40.180', 'Штангенглубиномеры измерительные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Микрометр', '25.73.40.180', 'Микрометры измерительные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('Индикатор часовый', '25.73.40.180', 'Индикаторы часовые измерительные', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Проверка добавленных записей
SELECT id, keywords, okpd2_code, description
FROM blank_okpd2_mapping
WHERE keywords IN ('Державка', 'Штангенциркуль', 'Штангенциркули')
ORDER BY id DESC;
