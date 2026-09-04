using AskFix.Api.Common;
using AskFix.Api.Data;
using AskFix.Api.Dtos;
using AskFix.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Controllers;

[ApiController]
[Route("api/feed")]
public class FeedController(AppDbContext db) : ControllerBase
{
    /// <summary>Home feed. tab = latest | trending | unanswered; optional tag slug filter.</summary>
    [HttpGet]
    public async Task<ActionResult<Paged<FeedItem>>> GetFeed(
        [FromQuery] string tab = "latest", [FromQuery] string? tag = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Questions
            .Include(q => q.Author)
            .Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var slug = tag.Trim().ToLowerInvariant();
            query = query.Where(q => q.QuestionTags.Any(qt => qt.Tag.Slug == slug));
        }
        query = tab switch
        {
            "unanswered" => query.Where(q => q.AnswerCount == 0).OrderByDescending(q => q.CreatedAt),
            "trending" => Trending(query),
            _ => query.OrderByDescending(q => q.LastActivityAt),
        };

        var total = await query.CountAsync();
        var questions = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new Paged<FeedItem>(questions.Select(Map).ToList(), page, pageSize, total));
    }

    /// <summary>Activity in the last 30 days weighted by answers, votes, follows and views.</summary>
    private static IQueryable<Question> Trending(IQueryable<Question> query)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        return query
            .Where(q => q.LastActivityAt >= cutoff)
            .OrderByDescending(q =>
                q.AnswerCount * 3 + q.FollowerCount * 2 + q.ViewCount / 10 +
                q.Answers.Sum(a => a.UpvoteCount - a.DownvoteCount) * 2)
            .ThenByDescending(q => q.LastActivityAt);
    }

    internal static FeedItem Map(Question q) => new(
        q.Id, q.Title, HtmlText.Excerpt(q.BodyHtml ?? "", 180), AuthorDto.From(q.Author),
        q.QuestionTags.Select(qt => TagDto.From(qt.Tag)).ToList(),
        q.AnswerCount, q.FollowerCount, q.ViewCount, q.HasAccepted,
        q.Answers.Sum(a => a.UpvoteCount - a.DownvoteCount), q.CreatedAt, q.LastActivityAt);
}
