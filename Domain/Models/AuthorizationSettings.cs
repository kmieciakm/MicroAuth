namespace Domain.Models;

public record AuthorizationSettings
{
    public List<string> ManagementRoles { get; set; } = new() { "Administrator" };
    public List<string> DefaultRoles { get; set; } = new() { "User" };
    public List<string> PredefinedRoles { get; set; } = new() { "User", "Administrator" };
}

