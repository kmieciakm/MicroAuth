using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public static class Validator
{
    public static bool IsValidEmail(string email)
    {
        return new EmailAddressAttribute().IsValid(email);
    }
}
