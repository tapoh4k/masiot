-- ============================================
-- ИНСТРУКЦИЯ ПО ПРИМЕНЕНИЮ МИГРАЦИИ
-- ============================================
-- Дата: 2026-03-20
-- Описание: Добавление таблицы для кэширования данных ОКП из API
--
-- Выполните эту миграцию после обновления кода:
-- psql -U postgres -d agenti_db -f migrations/add_okp_cache_table.sql
-- ============================================

-- Создание таблицы okp_cache
CREATE TABLE IF NOT EXISTS okp_cache (
  id SERIAL PRIMARY KEY,
  code VARCHAR(50) NOT NULL,
  name VARCHAR(500) NOT NULL,
  description TEXT,
  cached_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Создание индексов
CREATE UNIQUE INDEX IF NOT EXISTS idx_okp_cache_code ON okp_cache(code);
CREATE INDEX IF NOT EXISTS idx_okp_cache_name ON okp_cache(name);
CREATE INDEX IF NOT EXISTS idx_okp_cache_cached_at ON okp_cache(cached_at);

-- Комментарии
COMMENT ON TABLE okp_cache IS 'Кэш данных ОКП из внешнего API';
COMMENT ON COLUMN okp_cache.code IS 'Код ОКП (уникальный)';
COMMENT ON COLUMN okp_cache.name IS 'Название из классификатора';
COMMENT ON COLUMN okp_cache.description IS 'Описание из классификатора';
COMMENT ON COLUMN okp_cache.cached_at IS 'Время последнего обновления из API';
COMMENT ON COLUMN okp_cache.created_at IS 'Время первого добавления в кэш';

-- Проверка
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'okp_cache') THEN
        RAISE NOTICE '✓ Таблица okp_cache успешно создана';
    ELSE
        RAISE WARNING '✗ Ошибка создания таблицы okp_cache';
    END IF;
END $$;
