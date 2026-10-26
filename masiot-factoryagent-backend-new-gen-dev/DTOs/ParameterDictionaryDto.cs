using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class ParameterDictionaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Unit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateParameterDictionaryDto
{
    [Required(ErrorMessage = "Название параметра обязательно")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 255 символов")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Тип не может превышать 100 символов")]
    public string? Type { get; set; }

    [StringLength(50, ErrorMessage = "Единица измерения не может превышать 50 символов")]
    public string? Unit { get; set; }
}

public class UpdateParameterDictionaryDto
{
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 255 символов")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Тип не может превышать 100 символов")]
    public string? Type { get; set; }

    [StringLength(50, ErrorMessage = "Единица измерения не может превышать 50 символов")]
    public string? Unit { get; set; }
}
