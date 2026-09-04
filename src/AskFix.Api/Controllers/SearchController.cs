using AskFix.Api.Common;
using AskFix.Api.Data;
using AskFix.Api.Dtos;
using AskFix.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(AppDbContext db) : ControllerBase
{
    /// <summary>Full-text search across questions (title+body) and answers, plus tag name match.</summary>
    [HttpGet]
    public async Task<ActionResult<SearchResults>> Search([FromQuery] string q, [FromQuery] int take = 20)
    {
        q = (q ?? "").Trim();
        if (q.Length < 2) return Ok(new SearchResults([], [], [], 0));
        take = Math.Clamp(take, 1, 50);
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var ftsQuery = BuildFtsQuery(q);
        List<int> questionIds = [];
        List<int> answerIds = [];
        if (ftsQuery is not null)
        {
            try
            {
                questionIds = await db.Database
                    .SqlQueryRaw<int>("SELECT QuestionId AS Value FROM QuestionsFts WHERE QuestionsFts MATCH {0} ORDER BY rank LIMIT {1}", ftsQuery, take)
                    .ToListAsync();
                answerIds = await db.Database
                    .SqlQueryRaw<int>("SELECT AnswerId AS Value FROM AnswersFts WHERE AnswersFts MATCH {0} ORDER BY rank LIMIT {1}", ftsQuery, take)
                    .ToListAsync();
            }
            catch
            {
                questionIds = []; // malformed query: fall back to LIKE search below
            }
        }

        if (questionIds.Count == 0)
            questionIds = await db.Questions.AsNoTracking()
                .Where(x => x.Title.Contains(q) || x.BodyText.Contains(q))
                .OrderByDescending(x => x.ViewCount)
                .Take(take).Select(x => x.Id).ToListAsync();
        if (answerIds.Count == 0)
            answerIds = await db.Answers.AsNoTracking()
                .Where(x => x.BodyText.Contains(q))
                .OrderByDescending(x => x.UpvoteCount)
                .Take(take).Select(x => x.Id).ToListAsync();

        var questions = await db.Questions.AsNoTracking()
            .Include(x => x.Author).Include(x => x.QuestionTags).ThenInclude(qt => qt.Tag).Include(x => x.Answers)
            .Where(x => questionIds.Contains(x.Id))
            .ToListAsync();
        questions = [.. questions.OrderByDescending(x => questionIds.IndexOf(x.Id))];

        var answers = await db.Answers.AsNoTracking()
            .Include(a => a.Author).Include(a => a.Comments).ThenInclude(c => c.Author).Include(a => a.Votes)
            .Where(a => answerIds.Contains(a.Id))
            .ToListAsync();
        answers = [.. answers.OrderByDescending(a => answerIds.IndexOf(a.Id))];

        var questionTitles = questions.ToDictionary(x => x.Id, x => x.Title);
        var hasAccepted = await db.Questions.AsNoTracking()
            .Where(x => answers.Select(a => a.QuestionId).Distinct().Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.HasAccepted);

        // tags match on any single word of the query ("vpn disconnects" -> VPN)
        var patterns = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length >= 2).Select(w => $"%{w}%").Take(5).ToList();
        var tags = await db.Tags.AsNoTracking()
            .Where(t => patterns.Any(p => EF.Functions.Like(t.Name, p))) // SQLite LIKE is ASCII case-insensitive
            .OrderByDescending(t => t.QuestionCount).Take(10).ToListAsync();

        var answerItems = answers.Select(a => new UserAnswerItem(
            AnswerDto.From(a, viewerId), a.QuestionId,
            questionTitles.GetValueOrDefault(a.QuestionId, ""), hasAccepted.GetValueOrDefault(a.QuestionId))).ToList();

        return Ok(new SearchResults(
            questions.Select(FeedController.Map).ToList(),
            answerItems,
            tags.Select(TagDto.From).ToList(),
            questions.Count + answerItems.Count));
    }

    /// <summary>Converts free text to an FTS5 prefix query: "vpn disconn" -> "vpn* OR disconn*".</summary>
    private static string? BuildFtsQuery(string q)
    {
        var tokens = q.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Replace("\"", "").Trim())
            .Where(t => t.Length >= 2 && t.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '#' or '+' or '*' or '^'))
            .Distinct().Take(8).ToList();
        if (tokens.Count == 0) return null;
        return string.Join(" OR ", tokens.Select(t => $"\"{t}\"*"));
    }
}

[ApiController]
[Route("api/stats")]
public class StatsController(AppDbContext db2) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SiteStats>> Get() => Ok(new SiteStats(
        await db2.Questions.CountAsync(),
        await db2.Answers.CountAsync(),
        await db2.Users.CountAsync(),
        await db2.Tags.CountAsync(),
        await db2.Questions.CountAsync(x => x.AnswerCount == 0)));
}
