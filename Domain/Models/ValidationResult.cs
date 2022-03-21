namespace Domain.Models;

public record struct ValidationResult(bool IsValid, List<string> Errors) { }