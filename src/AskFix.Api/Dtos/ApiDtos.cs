using AskFix.Api.Models;

namespace AskFix.Api.Dtos;

// ---- Auth ---------------------------------------------------------------------------

public record LoginRequest(string Username, string Password);
public record MeResponse(int Id, string Sam, string DisplayName, string Email, string Department,
    string? Bio, int AvatarHue, bool IsAdmin, int Reputation, string Badge, DateTime CreatedAt)
{
    public static MeResponse From(User u) => new(u.Id, u.SamAccountName, u.DisplayName, u.Email, u.Department,
        u.Bio, u.AvatarHue, u.IsAdmin, u.Reputation, Common.CurrentUser.BadgeFor(u.Reputation), u.CreatedAt);
}
public record UpdateProfileRequest(string? Bio);
public record ApiInfo(string App, string Version, bool DevMode);

// ---- Shared -------------------------------------------------------------------------

public record AuthorDto(int Id, string DisplayName, string Department, int AvatarHue, int Reputation, string Badge)
{
    public static AuthorDto From(User u) =>
        new(u.Id, u.DisplayName, u.Department, u.AvatarHue, u.Reputation, Common.CurrentUser.BadgeFor(u.Reputation));
}

public record TagDto(int Id, string Name, string Slug, string? Description, string Color, int QuestionCount)
{
    public static TagDto From(Tag t) => new(t.Id, t.Name, t.Slug, t.Description, t.Color, t.QuestionCount);
}

public record Paged<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasMore => Page < TotalPages;
}

// ---- Feed & questions ---------------------------------------------------------------

public record FeedItem(int Id, string Title, string Excerpt, AuthorDto Author, IReadOnlyList<TagDto> Tags,
    int AnswerCount, int FollowerCount, int ViewCount, bool HasAccepted, int TotalUpvotes,
    DateTime CreatedAt, DateTime LastActivityAt);

public record QuestionDetail(int Id, string Title, string? BodyHtml, AuthorDto Author, IReadOnlyList<TagDto> Tags,
    int AnswerCount, int FollowerCount, int ViewCount, bool HasAccepted, int TotalUpvotes,
    DateTime CreatedAt, DateTime LastActivityAt, bool ViewerIsAuthor, bool IsFollowing, bool IsBookmarked)
{
    public static QuestionDetail From(Question q, int? viewerId, bool isFollowing, bool isBookmarked) => new(
        q.Id, q.Title, q.BodyHtml, AuthorDto.From(q.Author),
        q.QuestionTags.Select(qt => TagDto.From(qt.Tag)).ToList(),
        q.AnswerCount, q.FollowerCount, q.ViewCount, q.HasAccepted,
        q.Answers.Sum(a => a.UpvoteCount - a.DownvoteCount), q.CreatedAt, q.LastActivityAt,
        viewerId == q.AuthorId, isFollowing, isBookmarked);
}

public record CreateQuestionRequest(string Title, string? BodyHtml, IReadOnlyList<string> TagNames);
public record UpdateQuestionRequest(string Title, string? BodyHtml, IReadOnlyList<string> TagNames);
public record SimilarQuestion(int Id, string Title, int AnswerCount);

// ---- Answers ------------------------------------------------------------------------

public record AnswerDto(int Id, int QuestionId, AuthorDto Author, string BodyHtml,
    int UpvoteCount, int DownvoteCount, int Score, int MyVote, bool IsAccepted, int CommentCount,
    IReadOnlyList<CommentDto> Comments, DateTime CreatedAt, DateTime? UpdatedAt, bool ViewerIsAuthor)
{
    public static AnswerDto From(Answer a, int? viewerId) => new(
        a.Id, a.QuestionId, AuthorDto.From(a.Author), a.BodyHtml,
        a.UpvoteCount, a.DownvoteCount, a.UpvoteCount - a.DownvoteCount,
        viewerId is null ? 0 : a.Votes.FirstOrDefault(v => v.UserId == viewerId)?.Value ?? 0,
        a.IsAccepted, a.CommentCount,
        a.Comments.OrderBy(c => c.CreatedAt).Select(CommentDto.From).ToList(),
        a.CreatedAt, a.UpdatedAt, viewerId == a.AuthorId);
}

public record CreateAnswerRequest(string BodyHtml);
public record UpdateAnswerRequest(string BodyHtml);
public record VoteRequest(int Value);
public record VoteResult(int UpvoteCount, int DownvoteCount, int Score, int MyVote);

// ---- Comments -----------------------------------------------------------------------

public record CommentDto(int Id, AuthorDto Author, string Body, DateTime CreatedAt, bool ViewerIsAuthor)
{
    public static CommentDto FromWithViewer(Comment c, int? viewerId) => new(c.Id, AuthorDto.From(c.Author), c.Body, c.CreatedAt, viewerId == c.AuthorId);
    public static CommentDto From(Comment c) => FromWithViewer(c, null);
}
public record CreateCommentRequest(string Body);

// ---- Users --------------------------------------------------------------------------

public record UserProfile(int Id, string DisplayName, string Email, string Department, string? Bio,
    int AvatarHue, int Reputation, string Badge, int QuestionCount, int AnswerCount,
    int UpvotesReceived, int AnswersAccepted, DateTime CreatedAt, bool IsViewer);

public record UserAnswerItem(AnswerDto Answer, int QuestionId, string QuestionTitle, bool QuestionHasAccepted);

// ---- Notifications ------------------------------------------------------------------

public record NotificationDto(long Id, string Type, string ActorName, int ActorAvatarHue,
    int QuestionId, string QuestionTitle, int? AnswerId, bool IsRead, DateTime CreatedAt);

// ---- Search -------------------------------------------------------------------------

public record SearchResults(IReadOnlyList<FeedItem> Questions, IReadOnlyList<UserAnswerItem> Answers,
    IReadOnlyList<TagDto> Tags, int Total);

// ---- Misc ---------------------------------------------------------------------------

public record SiteStats(int Questions, int Answers, int Users, int Tags, int Unanswered);
public record UploadResult(string Url);
public record ToggleResult(bool Enabled);

// ---- Admin -----------------------------------------------------------------------------

public record AdminContributor(int Id, string DisplayName, string Department, int AvatarHue, int Reputation, string Badge, int Answers);
public record AdminActivity(string Type, int Id, string Title, string AuthorName, DateTime CreatedAt);
public record AdminStats(SiteStats Stats, IReadOnlyList<AdminContributor> TopContributors,
    IReadOnlyList<AdminActivity> RecentActivity, IReadOnlyList<AdminActivity> OldestUnanswered);
public record AdminUserRow(int Id, string DisplayName, string Sam, string Email, string Department,
    int AvatarHue, int Reputation, string Badge, bool IsAdmin, int QuestionCount, int AnswerCount,
    DateTime? LastLoginAt, DateTime CreatedAt);
public record UpdateTagRequest(string Name, string Color, string? Description);
public record MergeTagRequest(int TargetTagId);
public record AdminContentRow(int Id, int QuestionId, string Title, string Excerpt, string AuthorName,
    int Score, DateTime CreatedAt);
public record EmailSettingsDto(bool Enabled, string Host, int Port, string Username, bool UseSsl,
    string FromAddress, string FromName, string BaseUrl, bool HasPassword);
public record SaveEmailSettingsRequest(bool Enabled, string Host, int Port, string Username, string? Password,
    bool UseSsl, string FromAddress, string FromName, string BaseUrl);
public record NotificationPrefsDto(string Email, bool EmailOnAnswer, bool EmailOnComment, bool EmailOnAccepted);
public record SaveNotificationPrefsRequest(bool EmailOnAnswer, bool EmailOnComment, bool EmailOnAccepted);
