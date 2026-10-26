using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("users")]
public class User
{
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty; // Зашифрованный пароль (для просмотра админом)

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty; // Хеш пароля (для аутентификации)

    [Column("role")]
    public string Role { get; set; } = "user";

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
