namespace Domain.Models;

public record struct Token(
    string JWT
);

public record struct ResetToken(
    string Value
);