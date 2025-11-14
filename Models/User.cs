using System.ComponentModel.DataAnnotations;

namespace AuthenticationConsoleSystem;

public class User
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    [Required]
    public string? HashPassword { get; set; }
    public string? Email { get; set; }
    public bool IsLogged { get; set; } = false;
}
