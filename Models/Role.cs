namespace AuthenticationConsoleSystem;

public class Role
{
    public int Id { get; set; } 
    public string? Name { get; set; }

    public Role(int id, string? name)
    {
        this.Id = id;
        this.Name = name;
    }

    public Role(){}
}
