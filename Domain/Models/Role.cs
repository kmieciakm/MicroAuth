namespace Domain.Models;

public record Role
{
    public string Name { get; set; }

    public Role(string name)
    {
        Name = name;
    }
}
