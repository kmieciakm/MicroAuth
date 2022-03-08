namespace Domain.Models;

public record struct SignUpRequest
{
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmationPassword { get; set; }
}
