using AskFix.Api.Common;
using AskFix.Api.Data;
using AskFix.Api.Dtos;
using AskFix.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Paged<NotificationDto>>> GetNotifications(
        [FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId()!.Value;
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Notifications.AsNoTracking()
            .Include(n => n.Actor)
            .Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(n => new NotificationDto(
            n.Id, n.Type.ToString(), n.Actor.DisplayName, n.Actor.AvatarHue,
            n.QuestionId, n.QuestionTitle, n.AnswerId, n.IsRead, n.CreatedAt)).ToList();
        return Ok(new Paged<NotificationDto>(dtos, page, pageSize, total));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount() =>
        Ok(await db.Notifications.CountAsync(n => n.UserId == User.GetUserId()!.Value && !n.IsRead));

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll()
    {
        var userId = User.GetUserId()!.Value;
        await db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return NoContent();
    }

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> ReadOne(long id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == User.GetUserId()!.Value);
        if (n is null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
