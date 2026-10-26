namespace Backend.DTOs;

public class Okpd2DictionaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentCode { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOkpd2DictionaryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentCode { get; set; }
    public int Level { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateOkpd2DictionaryDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ParentCode { get; set; }
    public int? Level { get; set; }
    public bool? IsActive { get; set; }
}
