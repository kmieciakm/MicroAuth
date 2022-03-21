namespace Domain.Models;

public record struct SignInRequest(
    string Email,
    string Password
);

