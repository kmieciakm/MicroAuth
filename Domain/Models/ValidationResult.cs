namespace Domain.Models;

public record struct ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new List<string>();
}