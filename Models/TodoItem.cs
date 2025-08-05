namespace TodoApp.Models;

public class TodoItem
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public bool IsCompleted { get; set; } = false;

    public int UserId { get; set; } 

    public User? User { get; set; }  
}
