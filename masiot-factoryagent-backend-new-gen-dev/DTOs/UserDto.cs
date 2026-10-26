using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LoginDto
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Имя пользователя должно быть от 3 до 100 символов")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public string Password { get; set; } = string.Empty;
}

public class CreateUserDto
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Имя пользователя должно быть от 3 до 100 символов")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Роль обязательна")]
    [RegularExpression("^(admin|user|operator)$", ErrorMessage = "Недопустимая роль")]
    public string Role { get; set; } = "user";

    [StringLength(255, ErrorMessage = "Полное имя не может превышать 255 символов")]
    public string? FullName { get; set; }
}

public class UpdateUserDto
{
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public string? Password { get; set; }

    [RegularExpression("^(admin|user|operator)$", ErrorMessage = "Недопустимая роль")]
    public string? Role { get; set; }

    [StringLength(255, ErrorMessage = "Полное имя не может превышать 255 символов")]
    public string? FullName { get; set; }
}
