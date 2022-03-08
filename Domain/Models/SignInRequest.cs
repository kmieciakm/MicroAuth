namespace Domain.Models;

public record struct SignInRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

