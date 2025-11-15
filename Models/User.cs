using System.ComponentModel.DataAnnotations;

namespace AuthenticationConsoleSystem;

public class User
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    [Required]
    public string? HashPassword { get; set; }
    public bool IsLogged { get; set; } = false;

    public User() { }

    public User(int id, string? userName, string? hashPassword, bool isLogged)
    {
        Id = id;
        UserName = userName;
        HashPassword = hashPassword;
        IsLogged = isLogged;
    }
}
