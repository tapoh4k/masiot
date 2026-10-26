using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Scripts;

public static class EnsureDrawingAnalysesTable
{
    public static async Task EnsureAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS drawing_analyses (
                id SERIAL PRIMARY KEY,
                file_name VARCHAR(255) NOT NULL,
                content_type VARCHAR(100),
                status VARCHAR(50) NOT NULL DEFAULT 'completed',
                part_name TEXT,
                drawing_number VARCHAR(128),
                material VARCHAR(256),
                total_labor_hours NUMERIC(12, 4),
                confidence VARCHAR(32),
                result_json JSONB NOT NULL DEFAULT '{}'::jsonb,
                model_name VARCHAR(128),
                error_message TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            CREATE INDEX IF NOT EXISTS idx_drawing_analyses_created_at ON drawing_analyses (created_at DESC);
            CREATE INDEX IF NOT EXISTS idx_drawing_analyses_status ON drawing_analyses (status);
            """);
    }
}
