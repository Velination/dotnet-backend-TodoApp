using Microsoft.AspNetCore.Mvc;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Controllers;

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
    public IActionResult AddTask([FromBody] TodoItem task)
    {
         var userIdClaim = User.FindFirst("id");
    if (userIdClaim == null)
        return Unauthorized("User not authenticated.");

    task.UserId = int.Parse(userIdClaim.Value);


        _context.TodoItems.Add(task);
        _context.SaveChanges();

        return Ok(task);
    }

    // GET all tasks
    [HttpGet("all")]
    public IActionResult GetTasks()
    {
        var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
       var tasks = _context.TodoItems.Where(t => t.UserId == userId).ToList();
        return Ok(tasks);
    }

    // UPDATE a task
    [HttpPut("update/{id}")]
    public IActionResult UpdateTask(int id, [FromBody] TodoItem updatedTask)
    {
        var task = _context.TodoItems.Find(id);
        if (task == null) 
        {return NotFound();}

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
        var task = _context.TodoItems.Find(id);
        if (task == null) return NotFound();

        _context.TodoItems.Remove(task);
        _context.SaveChanges();

        return Ok("Task deleted.");
    }
}
