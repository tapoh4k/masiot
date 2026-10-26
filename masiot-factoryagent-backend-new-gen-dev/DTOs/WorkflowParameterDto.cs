using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class WorkflowParameterDto
{
    [Required(ErrorMessage = "Имя параметра обязательно")]
    [MaxLength(255, ErrorMessage = "Имя параметра не может превышать 255 символов")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Тип параметра обязателен")]
    [MaxLength(50, ErrorMessage = "Тип параметра не может превышать 50 символов")]
    public string Type { get; set; } = "string"; // int, string, bool, float, date

    [MaxLength(500, ErrorMessage = "Описание не может превышать 500 символов")]
    public string? Description { get; set; }
}
