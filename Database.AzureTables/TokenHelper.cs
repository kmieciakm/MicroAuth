using BC = BCrypt.Net.BCrypt;

namespace Database.AzureTables;

internal class TokenHelper
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Random Random = new Random();

    private static string GenerateRandomString(int length)
    {
        return new string(Enumerable
          .Range(0, length)
          .Select(num => Alphabet[Random.Next() % Alphabet.Length])
          .ToArray());
    }

    public static string GenerateToken()
    {
        var randomString = GenerateRandomString(64);
        return BC.HashPassword(randomString);
    }
}
