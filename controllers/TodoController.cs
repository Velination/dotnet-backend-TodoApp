using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TodoApp.Data;
using TodoApp.Models;


namespace TodoApp.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    private readonly AppDbContext _context;

    public TodoController(AppDbContext context)
    {
        _context = context;
    }

    // CREATE a new todo item
   [HttpPost("add")]
[Authorize]
public IActionResult AddTask([FromBody] TodoItem newTask)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim == null)
    {
        Console.WriteLine("Token missing user ID claim.");
        return Unauthorized(new { message = "User not authenticated" });
    }

    int userId = int.Parse(userIdClaim.Value);
    Console.WriteLine($"Adding task for user ID: {userId}");

    var userExists = _context.Users.Any(u => u.Id == userId);
    Console.WriteLine($"User exists: {userExists}");

    if (!userExists)
    {
        return BadRequest(new { message = "Invalid user ID in token" });
    }

    var task = new TodoItem
    {
        Title = newTask.Title,
         Description = newTask.Description,
        IsCompleted = false,
        UserId = userId
    };

    _context.TodoItems.Add(task);
    _context.SaveChanges();

    return Ok(task);
}


    // GET all tasks
  [HttpGet("all")]
[Authorize]
public IActionResult GetTasks()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim == null) return Unauthorized();

    var userId = int.Parse(userIdClaim.Value);
    var tasks = _context.TodoItems
        .Where(t => t.UserId == userId)
        .ToList();

    return Ok(tasks);
}

    // UPDATE a task
  [HttpPut("update/{id}")]
public IActionResult UpdateTask(int id, [FromBody] TodoItem updatedTask)
{
    // Get the user ID from JWT claims
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    // Make sure task belongs to the user
    var task = _context.TodoItems.FirstOrDefault(t => t.Id == id && t.UserId == userId);
    if (task == null)
    {
        return NotFound("Task not found or does not belong to user.");
    }

    // Update task fields
    task.Title = updatedTask.Title;
    task.Description = updatedTask.Description;
    task.IsCompleted = updatedTask.IsCompleted;

    _context.SaveChanges();

    return Ok(task);
}


    // DELETE a task
    [HttpDelete("delete/{id}")]
    public IActionResult DeleteTask(int id)
    {
       var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

       var task = _context.TodoItems.FirstOrDefault(t => t.Id == id && t.UserId == userId);
        if (task == null) return NotFound();

        _context.TodoItems.Remove(task);
        _context.SaveChanges();

        return Ok("Task deleted.");
    }
}
