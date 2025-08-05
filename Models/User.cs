using System.ComponentModel.DataAnnotations;

namespace TodoApp.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string PasswordHash { get; set; } 
}
