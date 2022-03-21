namespace Domain.Models;

public record AuthenticationSettings
{
    public bool OpenRegistration { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? Secret { get; set; }
    public int ExpirationHours { get; set; } = 1;
}
