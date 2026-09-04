using System.Text.RegularExpressions;
using AskFix.Api.Common;
using AskFix.Api.Data;
using AskFix.Api.Dtos;
using AskFix.Api.Models;
using AskFix.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Controllers;

[ApiController]
[Route("api/questions")]
public class QuestionsController(AppDbContext db, NotificationService notifications) : ControllerBase
{
    private const int MaxTags = 5;

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionDetail>> GetById(int id)
    {
        var question = await db.Questions
            .Include(q => q.Author)
            .Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound(new { message = "Question not found." });

        question.ViewCount++; // best-effort view tracking
        db.Entry(question).Property(q => q.ViewCount).IsModified = true;
        await db.SaveChangesAsync();

        var viewerId = User.GetUserId();
        var isFollowing = viewerId is null ? false :
            await db.QuestionFollows.AnyAsync(f => f.UserId == viewerId && f.QuestionId == id);
        var isBookmarked = viewerId is null ? false :
            await db.Bookmarks.AnyAsync(b => b.UserId == viewerId && b.QuestionId == id);
        return Ok(QuestionDetail.From(question, viewerId, isFollowing, isBookmarked));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<QuestionDetail>> Create([FromBody] CreateQuestionRequest request)
    {
        var author = await RequireUser();
        if (author is null) return Unauthorized();

        var title = request.Title?.Trim() ?? "";
        if (title.Length < 10 || title.Length > 300)
            return BadRequest(new { message = "Title must be between 10 and 300 characters." });

        var bodyHtml = string.IsNullOrWhiteSpace(request.BodyHtml) ? null : HtmlText.Sanitize(request.BodyHtml.Trim());
        if (bodyHtml?.Length > 100_000) return BadRequest(new { message = "Question body is too long." });

        var question = new Question
        {
            AuthorId = author.Id,
            Title = title,
            BodyHtml = bodyHtml,
            BodyText = HtmlText.ToText(bodyHtml ?? ""),
        };
        await ApplyTags(question, request.TagNames);
        if (question.QuestionTags.Count == 0)
            return BadRequest(new { message = "Add at least one tag so the right people find your question." });

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = question.Id },
            QuestionDetail.From(LoadFull(question.Id), author.Id, false, false));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<QuestionDetail>> Update(int id, [FromBody] UpdateQuestionRequest request)
    {
        var question = await db.Questions
            .Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag)
            .Include(q => q.Author)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound(new { message = "Question not found." });

        var viewerId = User.GetUserId();
        if (viewerId != question.AuthorId && !User.GetIsAdmin())
            return Forbid();

        var title = request.Title?.Trim() ?? "";
        if (title.Length < 10 || title.Length > 300)
            return BadRequest(new { message = "Title must be between 10 and 300 characters." });

        question.Title = title;
        question.BodyHtml = string.IsNullOrWhiteSpace(request.BodyHtml) ? null : HtmlText.Sanitize(request.BodyHtml.Trim());
        question.BodyText = HtmlText.ToText(question.BodyHtml ?? "");

        foreach (var qt in await db.QuestionTags.Where(x => x.QuestionId == id).Include(x => x.Tag).ToListAsync())
        {
            qt.Tag.QuestionCount--;
            db.QuestionTags.Remove(qt);
        }
        await ApplyTags(question, request.TagNames);

        await db.SaveChangesAsync();
        var viewerId2 = User.GetUserId();
        var following = viewerId2 is null ? false : await db.QuestionFollows.AnyAsync(f => f.UserId == viewerId2 && f.QuestionId == id);
        var bookmarked = viewerId2 is null ? false : await db.Bookmarks.AnyAsync(b => b.UserId == viewerId2 && b.QuestionId == id);
        return Ok(QuestionDetail.From(LoadFull(id), viewerId2, following, bookmarked));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await db.Questions
            .Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound(new { message = "Question not found." });
        if (User.GetUserId() != question.AuthorId && !User.GetIsAdmin())
            return Forbid();

        foreach (var qt in question.QuestionTags)
        {
            qt.Tag.QuestionCount--;
            db.QuestionTags.Remove(qt);
        }
        db.Questions.Remove(question); // cascades answers, comments, votes, follows, bookmarks
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Duplicate check while typing a new question title, to avoid repeated questions.</summary>
    [HttpGet("similar")]
    public async Task<ActionResult<IReadOnlyList<SimilarQuestion>>> Similar([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<SimilarQuestion>());

        var words = SplitWords(q).Distinct().Take(5).ToList();
        if (words.Count == 0) return Ok(Array.Empty<SimilarQuestion>());

        var candidates = await db.Questions.AsNoTracking()
            .Where(x => words.Any(w => x.Title.Contains(w)))
            .OrderByDescending(x => x.LastActivityAt)
            .Take(30)
            .Select(x => new { x.Id, x.Title, x.AnswerCount })
            .ToListAsync();

        var ranked = candidates
            .Select(x => new { x.Id, x.Title, x.AnswerCount, Hits = words.Count(w => x.Title.Contains(w, StringComparison.OrdinalIgnoreCase)) })
            .OrderByDescending(x => x.Hits).ThenByDescending(x => x.AnswerCount)
            .Take(5)
            .Select(x => new SimilarQuestion(x.Id, x.Title, x.AnswerCount))
            .ToList();
        return Ok(ranked);
    }

    [HttpPost("{id:int}/follow")]
    [Authorize]
    public async Task<ActionResult<ToggleResult>> ToggleFollow(int id)
    {
        var user = await RequireUser();
        if (user is null) return Unauthorized();
        var question = await db.Questions.Include(q => q.Follows).FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound(new { message = "Question not found." });

        var existing = question.Follows.FirstOrDefault(f => f.UserId == user.Id);
        if (existing is not null)
        {
            db.QuestionFollows.Remove(existing);
            question.FollowerCount--;
            await db.SaveChangesAsync();
            return Ok(new ToggleResult(false));
        }

        question.Follows.Add(new QuestionFollow { UserId = user.Id });
        question.FollowerCount++;
        notifications.QuestionFollowed(question, user.Id);
        await db.SaveChangesAsync();
        return Ok(new ToggleResult(true));
    }

    [HttpPost("{id:int}/bookmark")]
    [Authorize]
    public async Task<ActionResult<ToggleResult>> ToggleBookmark(int id)
    {
        var user = await RequireUser();
        if (user is null) return Unauthorized();
        if (!await db.Questions.AnyAsync(q => q.Id == id))
            return NotFound(new { message = "Question not found." });

        var existing = await db.Bookmarks.FindAsync(user.Id, id);
        if (existing is not null)
        {
            db.Bookmarks.Remove(existing);
            await db.SaveChangesAsync();
            return Ok(new ToggleResult(false));
        }
        db.Bookmarks.Add(new Bookmark { UserId = user.Id, QuestionId = id });
        await db.SaveChangesAsync();
        return Ok(new ToggleResult(true));
    }

    /// <summary>Questions sharing tags with this one (right sidebar module).</summary>
    [HttpGet("{id:int}/related")]
    public async Task<ActionResult<IReadOnlyList<FeedItem>>> Related(int id)
    {
        var question = await db.Questions
            .Include(q => q.QuestionTags)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return Ok(Array.Empty<FeedItem>());

        var tagIds = question.QuestionTags.Select(qt => qt.TagId).ToList();
        var related = await db.Questions.AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Where(q => q.Id != id && q.QuestionTags.Any(qt => tagIds.Contains(qt.TagId)))
            .OrderByDescending(q => q.QuestionTags.Count(qt => tagIds.Contains(qt.TagId)))
            .ThenByDescending(q => q.LastActivityAt)
            .Take(5)
            .ToListAsync();
        return Ok(related.Select(FeedController.Map).ToList());
    }

    // ---- Answers -------------------------------------------------------------------------

    [HttpGet("{id:int}/answers")]
    public async Task<ActionResult<Paged<AnswerDto>>> GetAnswers(int id,
        [FromQuery] string sort = "top", [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!await db.Questions.AnyAsync(q => q.Id == id))
            return NotFound(new { message = "Question not found." });

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var viewerId = User.GetUserId();

        var query = db.Answers
            .Include(a => a.Author)
            .Include(a => a.Comments).ThenInclude(c => c.Author)
            .Include(a => a.Votes)
            .Where(a => a.QuestionId == id)
            .AsNoTracking();

        var total = await query.CountAsync();
        var answers = await (sort == "new"
                ? query.OrderByDescending(a => a.CreatedAt)
                : query.OrderByDescending(a => a.IsAccepted)
                       .ThenByDescending(a => a.UpvoteCount - a.DownvoteCount)
                       .ThenBy(a => a.CreatedAt))
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return Ok(new Paged<AnswerDto>(answers.Select(a => AnswerDto.From(a, viewerId)).ToList(), page, pageSize, total));
    }

    [HttpPost("{id:int}/answers")]
    [Authorize]
    public async Task<ActionResult<AnswerDto>> CreateAnswer(int id, [FromBody] CreateAnswerRequest request)
    {
        var author = await RequireUser();
        if (author is null) return Unauthorized();

        var question = await db.Questions
            .Include(q => q.Follows)
            .Include(q => q.Author)
            .Include(q => q.Answers).ThenInclude(a => a.Author)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound(new { message = "Question not found." });

        var bodyHtml = HtmlText.Sanitize(request.BodyHtml?.Trim() ?? "");
        if (HtmlText.ToText(bodyHtml).Length < 15)
            return BadRequest(new { message = "Answer is too short — add some detail so it actually helps." });
        if (bodyHtml.Length > 100_000) return BadRequest(new { message = "Answer is too long." });

        var answer = new Answer
        {
            QuestionId = id,
            AuthorId = author.Id,   // set explicitly: notification rows read it before SaveChanges fixup
            Author = author,
            BodyHtml = bodyHtml,
            BodyText = HtmlText.ToText(bodyHtml),
        };
        question.Answers.Add(answer);
        question.AnswerCount++;
        notifications.QuestionAnswered(question, answer);
        author.Reputation += NotificationService.AnswerRep;
        await db.SaveChangesAsync();

        return Ok(AnswerDto.From(LoadAnswer(answer.Id), author.Id));
    }

    // ---- helpers -------------------------------------------------------------------------

    internal async Task<User?> RequireUser() =>
        User.GetUserId() is { } id ? await db.Users.FindAsync(id) : null;

    private Question LoadFull(int id) => db.Questions
        .Include(q => q.Author)
        .Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag)
        .Include(q => q.Answers)
        .First(q => q.Id == id);

    private Answer LoadAnswer(int id) => db.Answers
        .Include(a => a.Author)
        .Include(a => a.Comments).ThenInclude(c => c.Author)
        .Include(a => a.Votes)
        .First(a => a.Id == id);

    private async Task ApplyTags(Question question, IReadOnlyList<string>? tagNames)
    {
        if (tagNames is null) return;
        foreach (var raw in tagNames.Take(MaxTags))
        {
            var name = raw.Trim().TrimStart('#');
            if (name.Length is < 2 or > 30) continue;
            var slug = name.ToLowerInvariant().Replace(' ', '-');
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (tag is null)
            {
                tag = new Tag { Name = name, Slug = slug, Color = "#5457D6" };
                db.Tags.Add(tag);
            }
            if (question.QuestionTags.All(qt => qt.Tag.Slug != slug))
            {
                tag.QuestionCount++;
                question.QuestionTags.Add(new QuestionTag { Tag = tag });
            }
        }
    }

    private static List<string> SplitWords(string text) =>
        [.. Regex.Matches(text.ToLowerInvariant(), "[a-z0-9.#]{3,}").Select(m => m.Value).Where(w => w.Length >= 3)];
}
