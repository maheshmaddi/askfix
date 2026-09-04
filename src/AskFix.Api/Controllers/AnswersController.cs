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
[Route("api/answers")]
public class AnswersController(AppDbContext db, NotificationService notifications) : ControllerBase
{
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<AnswerDto>> Update(int id, [FromBody] UpdateAnswerRequest request)
    {
        var answer = await LoadAnswer(id);
        if (answer is null) return NotFound(new { message = "Answer not found." });
        if (User.GetUserId() != answer.AuthorId && !User.GetIsAdmin()) return Forbid();

        var bodyHtml = HtmlText.Sanitize(request.BodyHtml?.Trim() ?? "");
        if (HtmlText.ToText(bodyHtml).Length < 15)
            return BadRequest(new { message = "Answer is too short — add some detail so it actually helps." });

        answer.BodyHtml = bodyHtml;
        answer.BodyText = HtmlText.ToText(bodyHtml);
        answer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(AnswerDto.From((await LoadAnswer(id))!, User.GetUserId()));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var answer = await db.Answers
            .Include(a => a.Question)
            .Include(a => a.Votes)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (answer is null) return NotFound(new { message = "Answer not found." });
        if (User.GetUserId() != answer.AuthorId && !User.GetIsAdmin()) return Forbid();

        if (answer.IsAccepted) answer.Question.HasAccepted = false;
        answer.Question.AnswerCount--;
        db.Answers.Remove(answer);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Set the asker's "this worked for me" marker. Asker only; toggles off when called again.</summary>
    [HttpPost("{id:int}/accept")]
    [Authorize]
    public async Task<ActionResult<ToggleResult>> ToggleAccept(int id)
    {
        var answer = await db.Answers
            .Include(a => a.Question).ThenInclude(q => q.Author)
            .Include(a => a.Question).ThenInclude(q => q.Answers).ThenInclude(a => a.Author)
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (answer is null) return NotFound(new { message = "Answer not found." });

        var viewerId = User.GetUserId()!.Value;
        if (viewerId != answer.Question.AuthorId && !User.GetIsAdmin())
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only the person who asked can mark which answer worked." });

        if (answer.IsAccepted) // un-accept
        {
            answer.IsAccepted = false;
            answer.Question.HasAccepted = false;
            notifications.Accepted(answer, accepted: false, answer.Question.Author.DisplayName);
            await db.SaveChangesAsync();
            return Ok(new ToggleResult(false));
        }

        foreach (var other in answer.Question.Answers.Where(a => a.IsAccepted && a.Id != id))
        {
            other.IsAccepted = false;
            notifications.Accepted(other, accepted: false, answer.Question.Author.DisplayName);
        }
        answer.IsAccepted = true;
        answer.Question.HasAccepted = true;
        notifications.Accepted(answer, accepted: true, answer.Question.Author.DisplayName);
        await db.SaveChangesAsync();
        return Ok(new ToggleResult(true));
    }

    /// <summary>Vote on an answer. Value: 1 upvote, -1 downvote, 0 clears. Same value again = toggle off.</summary>
    [HttpPost("{id:int}/vote")]
    [Authorize]
    public async Task<ActionResult<VoteResult>> Vote(int id, [FromBody] VoteRequest request)
    {
        var value = request.Value is 1 or -1 or 0 ? request.Value
            : throw new BadHttpRequestException("Value must be 1, -1 or 0.");

        var answer = await db.Answers
            .Include(a => a.Author)
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (answer is null) return NotFound(new { message = "Answer not found." });

        var viewerId = User.GetUserId()!.Value;
        var existing = await db.AnswerVotes.FindAsync(viewerId, id);

        if (existing is not null)
        {
            if (value == existing.Value) value = 0; // clicking the same arrow toggles the vote off
            if (value == 0)
            {
                if (existing.Value == 1) { answer.UpvoteCount--; notifications.Upvoted(answer, viewerId, added: false); }
                else answer.DownvoteCount--;
                db.AnswerVotes.Remove(existing);
            }
            else // flip direction
            {
                if (existing.Value == 1) { answer.UpvoteCount--; notifications.Upvoted(answer, viewerId, added: false); }
                else answer.DownvoteCount--;
                if (value == 1) { answer.UpvoteCount++; notifications.Upvoted(answer, viewerId, added: true); }
                else answer.DownvoteCount++;
                existing.Value = value;
                existing.CreatedAt = DateTime.UtcNow;
            }
        }
        else if (value != 0)
        {
            db.AnswerVotes.Add(new AnswerVote { UserId = viewerId, AnswerId = id, Value = value });
            if (value == 1) { answer.UpvoteCount++; notifications.Upvoted(answer, viewerId, added: true); }
            else answer.DownvoteCount++;
        }

        await db.SaveChangesAsync();
        return Ok(new VoteResult(answer.UpvoteCount, answer.DownvoteCount,
            answer.UpvoteCount - answer.DownvoteCount,
            value)); // value after toggle logic = resulting viewer vote
    }

    // ---- comments ------------------------------------------------------------------------

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments(int id)
    {
        var viewerId = User.GetUserId();
        var comments = await db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .Where(c => c.AnswerId == id)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        return Ok(comments.Select(c => CommentDto.FromWithViewer(c, viewerId)).ToList());
    }

    [HttpPost("{id:int}/comments")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> AddComment(int id, [FromBody] CreateCommentRequest request)
    {
        var author = await db.Users.FindAsync(User.GetUserId()!.Value);
        if (author is null) return Unauthorized();

        var answer = await db.Answers
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (answer is null) return NotFound(new { message = "Answer not found." });

        var body = (request.Body ?? "").Trim();
        if (body.Length is < 2 or > 1000)
            return BadRequest(new { message = "Comment must be between 2 and 1000 characters." });

        var comment = new Comment { AnswerId = id, AuthorId = author.Id, Body = body };
        db.Comments.Add(comment);
        notifications.Commented(answer, comment, author.DisplayName);
        await db.SaveChangesAsync();

        return Ok(new CommentDto(comment.Id, AuthorDto.From(author), body, comment.CreatedAt, ViewerIsAuthor: true));
    }

    [HttpDelete("comments/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await db.Comments.Include(c => c.Answer).FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null) return NotFound(new { message = "Comment not found." });
        if (User.GetUserId() != comment.AuthorId && !User.GetIsAdmin()) return Forbid();

        comment.Answer.CommentCount--;
        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Answer?> LoadAnswer(int id) => await db.Answers
        .Include(a => a.Author)
        .Include(a => a.Comments).ThenInclude(c => c.Author)
        .Include(a => a.Votes)
        .FirstOrDefaultAsync(a => a.Id == id);
}
