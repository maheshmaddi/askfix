using AskFix.Api.Common;
using AskFix.Api.Data;
using AskFix.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AskFix.Api.Controllers;

/// <summary>Per-user notification preferences (Settings page).</summary>
[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet("notifications")]
    public async Task<ActionResult<NotificationPrefsDto>> Get()
    {
        var user = await db.Users.FindAsync(User.GetUserId()!.Value);
        if (user is null) return Unauthorized();
        return Ok(new NotificationPrefsDto(user.Email, user.EmailOnAnswer, user.EmailOnComment, user.EmailOnAccepted));
    }

    [HttpPut("notifications")]
    public async Task<ActionResult<NotificationPrefsDto>> Save([FromBody] SaveNotificationPrefsRequest request)
    {
        var user = await db.Users.FindAsync(User.GetUserId()!.Value);
        if (user is null) return Unauthorized();
        user.EmailOnAnswer = request.EmailOnAnswer;
        user.EmailOnComment = request.EmailOnComment;
        user.EmailOnAccepted = request.EmailOnAccepted;
        await db.SaveChangesAsync();
        return Ok(new NotificationPrefsDto(user.Email, user.EmailOnAnswer, user.EmailOnComment, user.EmailOnAccepted));
    }
}
