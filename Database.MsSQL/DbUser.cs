using Domain.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Database.MsSQL;

public class DbUser : IdentityUser
{
    [Required]
    [MaxLength(200)]
    public string Firstname { get; set; }
    [Required]
    [MaxLength(200)]
    public string Lastname { get; set; }

    public DbUser(string firstname, string lastname, string email) : base()
    {
        Firstname = firstname;
        Lastname = lastname;
        Email = email;
        UserName = Email;
    }

    public DbUser(User user) : base()
    {
        Id = user.Guid.ToString();
        Firstname = user.Firstname;
        Lastname = user.Lastname;
        Email = user.Email;
        UserName = Email;
    }

    public User ToDomainUser(List<Role> roles)
    {
        return new User(
            Guid.Parse(Id),
            Firstname,
            Lastname,
            Email,
            roles
        );
    }
}