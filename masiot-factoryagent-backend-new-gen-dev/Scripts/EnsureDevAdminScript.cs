using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Backend.Scripts;

/// <summary>
/// Гарантирует наличиеспособность admin/admin123 в Development.
/// </summary>
public static class EnsureDevAdminScript
{
    public const string DefaultUsername = "admin";
    public const string DefaultPassword = "admin123";

    public static async Task EnsureAsync(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IPasswordHashService passwordHashService)
    {
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == DefaultUsername);
        var passwordHash = passwordHashService.HashPassword(DefaultPassword);
        var encryptedPassword = encryptionService.Encrypt(DefaultPassword);

        if (admin == null)
        {
            context.Users.Add(new User
            {
                Username = DefaultUsername,
                Password = encryptedPassword,
                PasswordHash = passwordHash,
                Role = "admin",
                FullName = "Администратор",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            Log.Information("Dev admin created: {Username} / {Password}", DefaultUsername, DefaultPassword);
            return;
        }

        // Всегда синхронизируем хеш на известный dev-пароль, чтобы вход не «ломался» из-за старой БД
        admin.Password = encryptedPassword;
        admin.PasswordHash = passwordHash;
        admin.Role = "admin";
        admin.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        Log.Information("Dev admin password reset to {Username} / {Password}", DefaultUsername, DefaultPassword);
    }
}
