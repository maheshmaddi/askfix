using AskFix.Api.Common;
using AskFix.Api.Data;
using AskFix.Api.Dtos;
using AskFix.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserProfile>> GetProfile(int id)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { message = "User not found." });

        var answers = await db.Answers.AsNoTracking().Where(a => a.AuthorId == id).ToListAsync();
        var profile = new UserProfile(
            user.Id, user.DisplayName, user.Email, user.Department, user.Bio, user.AvatarHue,
            user.Reputation, CurrentUser.BadgeFor(user.Reputation),
            await db.Questions.CountAsync(q => q.AuthorId == id),
            answers.Count,
            answers.Sum(a => a.UpvoteCount),
            answers.Count(a => a.IsAccepted),
            user.CreatedAt,
            IsViewer: User.GetUserId() == id);
        return Ok(profile);
    }

    [HttpGet("{id:int}/questions")]
    public async Task<ActionResult<IReadOnlyList<FeedItem>>> GetQuestions(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 50);
        var questions = await db.Questions.AsNoTracking()
            .Include(q => q.Author).Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag).Include(q => q.Answers)
            .Where(q => q.AuthorId == id)
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        return Ok(questions.Select(FeedController.Map).ToList());
    }

    [HttpGet("{id:int}/answers")]
    public async Task<ActionResult<IReadOnlyList<UserAnswerItem>>> GetAnswers(int id)
    {
        var viewerId = User.GetUserId();
        var answers = await db.Answers.AsNoTracking()
            .Include(a => a.Author).Include(a => a.Comments).ThenInclude(c => c.Author).Include(a => a.Votes)
            .Where(a => a.AuthorId == id)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToListAsync();
        var questionIds = answers.Select(a => a.QuestionId).Distinct().ToList();
        var questions = await db.Questions.AsNoTracking()
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => new { q.Title, q.HasAccepted });

        var items = answers
            .Where(a => questions.ContainsKey(a.QuestionId))
            .Select(a => new UserAnswerItem(AnswerDto.From(a, viewerId), a.QuestionId,
                questions[a.QuestionId].Title, questions[a.QuestionId].HasAccepted))
            .ToList();
        return Ok(items);
    }

    [HttpGet("me/bookmarks")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<FeedItem>>> MyBookmarks()
    {
        var viewerId = User.GetUserId()!.Value;
        var questionIds = await db.Bookmarks.AsNoTracking()
            .Where(b => b.UserId == viewerId)
            .Select(b => b.QuestionId)
            .ToListAsync();
        var questions = await db.Questions.AsNoTracking()
            .Include(q => q.Author).Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag).Include(q => q.Answers)
            .Where(q => questionIds.Contains(q.Id))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return Ok(questions.Select(FeedController.Map).ToList());
    }
}
