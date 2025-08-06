using System.ComponentModel.DataAnnotations;

namespace TodoApp.Models;

public class User
{
    public int Id { get; set; }

    public required string FullName { get; set; }

    [Required]
    [EmailAddress]
    public  required string Email { get; set; }

    public required string PasswordHash { get; set; } 
}
