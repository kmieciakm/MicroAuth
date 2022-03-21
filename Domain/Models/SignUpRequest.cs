namespace Domain.Models;

public record struct SignUpRequest(
    string Firstname,
    string Lastname,
    string Email,
    string Password,
    string ConfirmationPassword
);