using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Services;

namespace Backend.Scripts;

/// <summary>
/// Скрипт для миграции существующих пользователей на новую систему безопасности
/// Запускается один раз для обновления всех паролей
/// </summary>
public class MigrateUserPasswordsScript
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IPasswordHashService _passwordHashService;

    public MigrateUserPasswordsScript(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IPasswordHashService passwordHashService)
    {
        _context = context;
        _encryptionService = encryptionService;
        _passwordHashService = passwordHashService;
    }

    public async Task MigrateAsync()
    {
        Console.WriteLine("Starting user password migration...");

        var users = await _context.Users.ToListAsync();
        int migratedCount = 0;

        foreach (var user in users)
        {
            // Если password_hash пустой, значит пользователь еще не мигрирован
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                // Предполагаем, что в поле Password хранится открытый пароль
                var plainPassword = user.Password;

                // Шифруем пароль для просмотра админом
                user.Password = _encryptionService.Encrypt(plainPassword);

                // Хешируем пароль для аутентификации
                user.PasswordHash = _passwordHashService.HashPassword(plainPassword);

                user.UpdatedAt = DateTime.UtcNow;

                migratedCount++;
                Console.WriteLine($"Migrated user: {user.Username}");
            }
        }

        if (migratedCount > 0)
        {
            await _context.SaveChangesAsync();
            Console.WriteLine($"Migration completed. {migratedCount} users migrated.");
        }
        else
        {
            Console.WriteLine("No users to migrate. All users are already using the new security system.");
        }
    }
}
