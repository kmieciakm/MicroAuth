namespace Domain.Models;

public record struct Claims(
    string Email,
    IEnumerable<Role> Roles
);