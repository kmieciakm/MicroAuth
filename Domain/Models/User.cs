namespace Domain.Models;

public class User
{
    public Guid Guid { get; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }
    public IReadOnlyList<Role> Roles { get; }

    public User(Guid guid, string firstname, string lastname, string email)
    {
        Guid = guid;
        Firstname = firstname;
        Lastname = lastname;
        Email = email;
        Roles = new List<Role>().AsReadOnly();
    }

    public User(Guid guid, string firstname, string lastname, string email, List<Role> roles)
    {
        Guid = guid;
        Firstname = firstname;
        Lastname = lastname;
        Email = email;
        Roles = roles.AsReadOnly();
    }

    public override bool Equals(object? obj)
    {
        return obj is User user &&
               Guid.Equals(user.Guid) &&
               Firstname == user.Firstname &&
               Lastname == user.Lastname &&
               Email == user.Email &&
               Roles.SequenceEqual(user.Roles);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Guid, Firstname, Lastname, Email, Roles);
    }
}
