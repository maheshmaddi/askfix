using AskFix.Api.Data;
using AskFix.Api.Dtos;
using AskFix.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetTags(
        [FromQuery] string sort = "popular", [FromQuery] int take = 100)
    {
        var tags = await db.Tags.AsNoTracking()
            .OrderByDescending(t => t.QuestionCount).ThenBy(t => t.Name)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();
        return Ok(tags.Select(TagDto.From).ToList());
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<TagDto>> GetTag(string slug)
    {
        var tag = await db.Tags.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant());
        return tag is null ? NotFound(new { message = "Tag not found." }) : Ok(TagDto.From(tag));
    }
}
