using BC =  BCrypt.Net.BCrypt;

namespace Database.AzureTables;

internal static class PasswordHelper
{
    public static string GenerateSalt()
    {
        return BC.GenerateSalt();
    }

    public static string HashPassword(string password, string salt)
    {
        return BC.HashPassword(password, salt);
    }

    public static bool Validate(string password, string salt, string passwordHash)
    {
        var providedHash = HashPassword(password, salt);
        return providedHash.Equals(passwordHash);
    }
}
