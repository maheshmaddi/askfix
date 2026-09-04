using System.Text.RegularExpressions;
using AskFix.Api.Common;
using AskFix.Api.Data;
using AskFix.Api.Dtos;
using AskFix.Api.Models;
using AskFix.Api.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Controllers;

/// <summary>Admin panel API: dashboard stats, user roles, tag grooming, content moderation and email settings.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public partial class AdminController(AppDbContext db, EmailSettingsService emailSettings, IEmailSender emailSender) : ControllerBase
{
    // ---- dashboard -----------------------------------------------------------------------

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStats>> Stats()
    {
        var stats = new SiteStats(
            await db.Questions.CountAsync(),
            await db.Answers.CountAsync(),
            await db.Users.CountAsync(),
            await db.Tags.CountAsync(),
            await db.Questions.CountAsync(q => q.AnswerCount == 0));

        var topContributors = await db.Users.AsNoTracking()
            .OrderByDescending(u => u.Reputation).ThenBy(u => u.DisplayName).Take(5)
            .Select(u => new AdminContributor(u.Id, u.DisplayName, u.Department, u.AvatarHue, u.Reputation,
                "", db.Answers.Count(a => a.AuthorId == u.Id)))
            .ToListAsync();
        topContributors = [.. topContributors.Select(c => c with { Badge = CurrentUser.BadgeFor(c.Reputation) })];

        var recentQuestions = await db.Questions.AsNoTracking()
            .OrderByDescending(q => q.CreatedAt).Take(5)
            .Select(q => new AdminActivity("question", q.Id, q.Title, q.Author.DisplayName, q.CreatedAt))
            .ToListAsync();
        var recentAnswers = await db.Answers.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt).Take(5)
            .Select(a => new AdminActivity("answer", a.Id, a.Question.Title, a.Author.DisplayName, a.CreatedAt))
            .ToListAsync();
        var recentActivity = recentQuestions.Concat(recentAnswers)
            .OrderByDescending(a => a.CreatedAt).Take(10).ToList();

        var oldestUnanswered = await db.Questions.AsNoTracking()
            .Where(q => q.AnswerCount == 0)
            .OrderBy(q => q.CreatedAt).Take(5)
            .Select(q => new AdminActivity("question", q.Id, q.Title, q.Author.DisplayName, q.CreatedAt))
            .ToListAsync();

        return Ok(new AdminStats(stats, topContributors, recentActivity, oldestUnanswered));
    }

    // ---- users ---------------------------------------------------------------------------

    [HttpGet("users")]
    public async Task<ActionResult<Paged<AdminUserRow>>> Users([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var q = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            q = q.Where(u => EF.Functions.Like(u.DisplayName, pattern)
                          || EF.Functions.Like(u.SamAccountName, pattern)
                          || EF.Functions.Like(u.Email, pattern));
        }

        var total = await q.CountAsync();
        var users = await q.OrderByDescending(u => u.Reputation).ThenBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new AdminUserRow(u.Id, u.DisplayName, u.SamAccountName, u.Email, u.Department,
                u.AvatarHue, u.Reputation, "", u.IsAdmin, 0, 0, u.LastLoginAt, u.CreatedAt))
            .ToListAsync();

        var ids = users.Select(u => u.Id).ToList();
        var questionCounts = await db.Questions.Where(x => ids.Contains(x.AuthorId))
            .GroupBy(x => x.AuthorId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var answerCounts = await db.Answers.Where(x => ids.Contains(x.AuthorId))
            .GroupBy(x => x.AuthorId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);

        var rows = users.Select(u => u with
        {
            Badge = CurrentUser.BadgeFor(u.Reputation),
            QuestionCount = questionCounts.GetValueOrDefault(u.Id),
            AnswerCount = answerCounts.GetValueOrDefault(u.Id),
        }).ToList();
        return Ok(new Paged<AdminUserRow>(rows, page, pageSize, total));
    }

    [HttpPost("users/{id:int}/toggle-admin")]
    public async Task<IActionResult> ToggleAdmin(int id)
    {
        var target = await db.Users.FindAsync(id);
        if (target is null) return NotFound(new { message = "User not found." });
        if (id == User.GetUserId()) return BadRequest(new { message = "You cannot change your own admin role." });

        target.IsAdmin = !target.IsAdmin;
        await db.SaveChangesAsync();
        return Ok(new ToggleResult(target.IsAdmin));
    }

    // ---- tags ----------------------------------------------------------------------------

    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> Tags() =>
        Ok(await db.Tags.AsNoTracking()
            .OrderByDescending(t => t.QuestionCount).ThenBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Slug, t.Description, t.Color, t.QuestionCount))
            .ToListAsync());

    [HttpPut("tags/{id:int}")]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagRequest request)
    {
        var tag = await db.Tags.FindAsync(id);
        if (tag is null) return NotFound(new { message = "Tag not found." });

        var name = request.Name?.Trim() ?? "";
        if (name.Length is < 2 or > 30) return BadRequest(new { message = "Tag name must be 2-30 characters." });
        if (!ColorRegex().IsMatch(request.Color ?? "")) return BadRequest(new { message = "Color must be a hex value like #5457D6." });

        var slug = name.ToLowerInvariant().Replace(' ', '-');
        if (await db.Tags.AnyAsync(t => t.Slug == slug && t.Id != id))
            return BadRequest(new { message = $"A tag with the name “{name}” already exists." });

        tag.Name = name;
        tag.Slug = slug;
        tag.Color = request.Color;
        tag.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()[..Math.Min(200, request.Description.Trim().Length)];
        await db.SaveChangesAsync();
        return Ok(TagDto.From(tag));
    }

    [HttpPost("tags/{id:int}/merge")]
    public async Task<IActionResult> MergeTag(int id, [FromBody] MergeTagRequest request)
    {
        if (request.TargetTagId == id) return BadRequest(new { message = "Cannot merge a tag into itself." });
        var source = await db.Tags.FindAsync(id);
        var target = await db.Tags.FindAsync(request.TargetTagId);
        if (source is null || target is null) return NotFound(new { message = "Source or target tag not found." });

        var sourceName = source.Name;
        var targetName = target.Name;

        // join rows can't be re-pointed in place (composite key) — remove + re-add
        var sourceLinks = await db.QuestionTags.Where(qt => qt.TagId == id).ToListAsync();
        var targetQuestionIds = (await db.QuestionTags.Where(qt => qt.TagId == target.Id)
            .Select(qt => qt.QuestionId).ToListAsync()).ToHashSet();
        foreach (var link in sourceLinks)
        {
            db.QuestionTags.Remove(link);
            if (!targetQuestionIds.Contains(link.QuestionId))
            {
                db.QuestionTags.Add(new QuestionTag { QuestionId = link.QuestionId, TagId = target.Id });
                targetQuestionIds.Add(link.QuestionId);
            }
        }
        target.QuestionCount = targetQuestionIds.Count;
        db.Tags.Remove(source);
        await db.SaveChangesAsync();
        return Ok(new { merged = sourceName, into = targetName, target.QuestionCount });
    }

    [HttpDelete("tags/{id:int}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var tag = await db.Tags.FindAsync(id);
        if (tag is null) return NotFound(new { message = "Tag not found." });
        if (await db.QuestionTags.AnyAsync(qt => qt.TagId == id))
            return BadRequest(new { message = "This tag still has questions attached — merge it instead." });

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- content moderation ----------------------------------------------------------------

    [HttpGet("content")]
    public async Task<ActionResult<Paged<AdminContentRow>>> Content(
        [FromQuery] string type = "question", [FromQuery] string? query = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var pattern = string.IsNullOrWhiteSpace(query) ? null : $"%{query.Trim()}%";

        if (type == "answer")
        {
            var aq = db.Answers.AsNoTracking().AsQueryable();
            if (pattern is not null)
                aq = aq.Where(a => EF.Functions.Like(a.BodyText, pattern) || EF.Functions.Like(a.Question.Title, pattern));
            var total = await aq.CountAsync();
            var rows = await aq.OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new AdminContentRow(a.Id, a.QuestionId, a.Question.Title,
                    a.BodyText.Substring(0, Math.Min(140, a.BodyText.Length)),
                    a.Author.DisplayName, a.UpvoteCount - a.DownvoteCount, a.CreatedAt))
                .ToListAsync();
            return Ok(new Paged<AdminContentRow>(rows, page, pageSize, total));
        }

        var qq = db.Questions.AsNoTracking().AsQueryable();
        if (pattern is not null)
            qq = qq.Where(x => EF.Functions.Like(x.Title, pattern) || EF.Functions.Like(x.BodyText, pattern));
        var qTotal = await qq.CountAsync();
        var qRows = await qq.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminContentRow(x.Id, x.Id, x.Title,
                x.BodyText.Substring(0, Math.Min(140, x.BodyText.Length)),
                x.Author.DisplayName, x.AnswerCount, x.CreatedAt))
            .ToListAsync();
        return Ok(new Paged<AdminContentRow>(qRows, page, pageSize, qTotal));
    }

    // ---- email settings --------------------------------------------------------------------

    [HttpGet("email-settings")]
    public ActionResult<EmailSettingsDto> GetEmailSettings()
    {
        var s = emailSettings.Load();
        return Ok(new EmailSettingsDto(s.Enabled, s.Host, s.Port, s.Username, s.UseSsl,
            s.FromAddress, s.FromName, s.BaseUrl, HasPassword: !string.IsNullOrEmpty(s.PasswordEnc)));
    }

    [HttpPut("email-settings")]
    public async Task<IActionResult> SaveEmailSettings([FromBody] SaveEmailSettingsRequest request)
    {
        var current = emailSettings.Load();
        var host = request.Host?.Trim() ?? "";
        var from = request.FromAddress?.Trim() ?? "";
        var baseUrl = request.BaseUrl?.Trim().TrimEnd('/') ?? "";

        if (request.Enabled)
        {
            if (host.Length == 0) return BadRequest(new { message = "SMTP host is required to enable email." });
            if (request.Port is < 1 or > 65535) return BadRequest(new { message = "Port must be between 1 and 65535." });
            if (!EmailRegex().IsMatch(from)) return BadRequest(new { message = "A valid from address is required." });
            if (baseUrl.Length == 0 || !baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Base URL is required for links inside emails (e.g. http://askfix.corp:8080)." });
        }

        var settings = new EmailSettings
        {
            Enabled = request.Enabled,
            Host = host,
            Port = request.Port,
            Username = request.Username?.Trim() ?? "",
            PasswordEnc = string.IsNullOrEmpty(request.Password) ? current.PasswordEnc : emailSettings.EncryptPassword(request.Password),
            UseSsl = request.UseSsl,
            FromAddress = from,
            FromName = string.IsNullOrWhiteSpace(request.FromName) ? "AskFix" : request.FromName.Trim(),
            BaseUrl = baseUrl,
        };
        emailSettings.Save(settings);
        return Ok(new EmailSettingsDto(settings.Enabled, settings.Host, settings.Port, settings.Username,
            settings.UseSsl, settings.FromAddress, settings.FromName, settings.BaseUrl,
            HasPassword: !string.IsNullOrEmpty(settings.PasswordEnc)));
    }

    [HttpPost("email-settings/test")]
    public async Task<IActionResult> TestEmail()
    {
        var s = emailSettings.Load();
        var admin = await db.Users.FindAsync(User.GetUserId()!.Value);
        if (string.IsNullOrWhiteSpace(admin?.Email))
            return BadRequest(new { message = "Your account has no email address (check the directory sync)." });
        if (!s.Enabled || string.IsNullOrWhiteSpace(s.Host))
            return BadRequest(new { message = "Save the SMTP settings and enable email first." });

        try
        {
            await emailSender.SendAsync(new EmailJob(admin.Email, "AskFix — test email",
                EmailTemplates.Test(s.BaseUrl)), s);
            return Ok(new { sent = true, to = admin.Email });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = $"SMTP send failed: {ex.Message}" });
        }
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex ColorRegex();

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$")]
    private static partial Regex EmailRegex();
}
