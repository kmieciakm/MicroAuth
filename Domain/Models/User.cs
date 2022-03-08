namespace Domain.Models;

public class User
{
    public Guid Guid { get; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }

    public User(Guid guid, string firstname, string lastname, string email)
    {
        Guid = guid;
        Firstname = firstname;
        Lastname = lastname;
        Email = email;
    }

    public override bool Equals(object? obj)
    {
        return obj is User user &&
               Guid.Equals(user.Guid) &&
               Firstname == user.Firstname &&
               Lastname == user.Lastname &&
               Email == user.Email;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Guid, Firstname, Lastname, Email);
    }
}
