using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Scripts;

public static class EnsureAppSettingsTable
{
    public static async Task EnsureAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS app_settings (
                key VARCHAR(100) PRIMARY KEY,
                value TEXT,
                is_secret BOOLEAN NOT NULL DEFAULT FALSE,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """);
    }
}
