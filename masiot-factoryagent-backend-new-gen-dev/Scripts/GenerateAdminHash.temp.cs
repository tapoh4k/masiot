using System;

namespace Backend.Scripts;

/// <summary>
/// Утилита для генерации BCrypt хеша пароля администратора
/// Использование: dotnet run --project Backend Scripts/GenerateAdminHash.cs
/// </summary>
public class GenerateAdminHash
{
#pragma warning disable CS7022
    public static void Main(string[] args)
    {
        string password = "admin";
        string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        Console.WriteLine("===========================================");
        Console.WriteLine("BCrypt Hash для пароля 'admin':");
        Console.WriteLine(hash);
        Console.WriteLine("===========================================");
        Console.WriteLine();
        Console.WriteLine("SQL для обновления:");
        Console.WriteLine($"UPDATE users SET password_hash = '{hash}' WHERE username = 'admin';");
        Console.WriteLine();
    }
#pragma warning restore CS7022
}
