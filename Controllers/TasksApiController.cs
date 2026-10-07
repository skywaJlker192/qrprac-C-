using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureTodo.Authorization;
using SecureTodo.Data;
using SecureTodo.Models;
using SecureTodo.Services;

namespace SecureTodo.Controllers;

/// <summary>CRUD задач через JWT. Без токена — 401.</summary>
[ApiController]
[Route("api/tasks")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TasksApiController : ControllerBase
{
    private const string Bearer = JwtBearerDefaults.AuthenticationScheme;

    private readonly AppDbContext _db;
    private readonly IAuthorizationService _auth;

    public TasksApiController(AppDbContext db, IAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    private static TaskDto ToDto(TaskItem t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        CreatedAt = t.CreatedAt,
        IsCompleted = t.IsCompleted,
        UserId = t.UserId
    };

    /// <summary>Обычный пользователь видит только свои задачи, админ — все.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAll()
    {
        var query = _db.Tasks.AsNoTracking();

        if (!User.IsInRole(AppRoles.Admin))
        {
            var userId = User.GetUserId();
            query = query.Where(t => t.UserId == userId);
        }

        var items = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDto>> GetById(int id)
    {
        var task = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound();

        var auth = await _auth.AuthorizeAsync(User, task, "CanEditTask");
        if (!auth.Succeeded) return Forbid(Bearer);

        return Ok(ToDto(task));
    }

    /// <summary>UserId берётся из токена, а не из тела запроса.</summary>
    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] TaskCreateDto dto)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var task = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ToDto(task));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskDto>> Update(int id, [FromBody] TaskUpdateDto dto)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound();

        var auth = await _auth.AuthorizeAsync(User, task, "CanEditTask");
        if (!auth.Succeeded) return Forbid(Bearer);

        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim();
        task.IsCompleted = dto.IsCompleted;
        await _db.SaveChangesAsync();

        return Ok(ToDto(task));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound();

        var auth = await _auth.AuthorizeAsync(User, task, "CanEditTask");
        if (!auth.Succeeded) return Forbid(Bearer);

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
