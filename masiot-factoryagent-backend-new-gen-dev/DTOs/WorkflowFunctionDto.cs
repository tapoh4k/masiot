using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class WorkflowFunctionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string FunctionClass { get; set; } = "";
    public string FunctionName { get; set; } = "";
    public List<WorkflowParameterDto> Params { get; set; } = new();
    public string? Returns { get; set; }
    public string? Icon { get; set; }
    public string Type { get; set; } = "function";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateWorkflowFunctionDto
{
    [Required(ErrorMessage = "Название функции обязательно")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 255 символов")]
    public string Name { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Класс функции обязателен")]
    [StringLength(500, ErrorMessage = "Класс функции не может превышать 500 символов")]
    public string FunctionClass { get; set; } = "";

    [Required(ErrorMessage = "Имя функции обязательно")]
    [StringLength(255, ErrorMessage = "Имя функции не может превышать 255 символов")]
    public string FunctionName { get; set; } = "";

    [Required(ErrorMessage = "Параметры обязательны")]
    public List<WorkflowParameterDto> Params { get; set; } = new();

    [StringLength(255, ErrorMessage = "Возвращаемое значение не может превышать 255 символов")]
    public string? Returns { get; set; }

    [StringLength(100, ErrorMessage = "Иконка не может превышать 100 символов")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "Тип обязателен")]
    [StringLength(50, ErrorMessage = "Тип не может превышать 50 символов")]
    public string Type { get; set; } = "function";
}

public class UpdateWorkflowFunctionDto
{
    [Required(ErrorMessage = "Название функции обязательно")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 255 символов")]
    public string Name { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Класс функции обязателен")]
    [StringLength(500, ErrorMessage = "Класс функции не может превышать 500 символов")]
    public string FunctionClass { get; set; } = "";

    [Required(ErrorMessage = "Имя функции обязательно")]
    [StringLength(255, ErrorMessage = "Имя функции не может превышать 255 символов")]
    public string FunctionName { get; set; } = "";

    [Required(ErrorMessage = "Параметры обязательны")]
    public List<WorkflowParameterDto> Params { get; set; } = new();

    [StringLength(255, ErrorMessage = "Возвращаемое значение не может превышать 255 символов")]
    public string? Returns { get; set; }

    [StringLength(100, ErrorMessage = "Иконка не может превышать 100 символов")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "Тип обязателен")]
    [StringLength(50, ErrorMessage = "Тип не может превышать 50 символов")]
    public string Type { get; set; } = "function";
}
