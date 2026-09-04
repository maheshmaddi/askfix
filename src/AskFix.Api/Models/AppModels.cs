namespace AskFix.Api.Models;

public class User
{
    public int Id { get; set; }
    public string SamAccountName { get; set; } = "";       // DOMAIN\user (lowercase), from AD
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public string? Bio { get; set; }
    public int AvatarHue { get; set; }                     // deterministic color for initial-avatars
    public bool IsAdmin { get; set; }
    public int Reputation { get; set; }
    public bool EmailOnAnswer { get; set; } = true;      // someone answered my/followed question
    public bool EmailOnComment { get; set; } = true;     // someone commented on my answer
    public bool EmailOnAccepted { get; set; } = true;    // my answer was marked "worked"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public List<Question> Questions { get; set; } = [];
    public List<Answer> Answers { get; set; } = [];
}

public class Question
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Title { get; set; } = "";
    public string? BodyHtml { get; set; }                  // rich text (sanitized)
    public string BodyText { get; set; } = "";             // plain text for FTS + excerpts

    public int ViewCount { get; set; }
    public int AnswerCount { get; set; }
    public int FollowerCount { get; set; }
    public bool HasAccepted { get; set; }                  // some answer marked "worked"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public List<QuestionTag> QuestionTags { get; set; } = [];
    public List<Answer> Answers { get; set; } = [];
    public List<QuestionFollow> Follows { get; set; } = [];
    public List<Bookmark> Bookmarks { get; set; } = [];
}

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string BodyHtml { get; set; } = "";
    public string BodyText { get; set; } = "";

    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsAccepted { get; set; }                   // asker marked "this worked"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<AnswerVote> Votes { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
}

public class AnswerVote
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int AnswerId { get; set; }
    public Answer Answer { get; set; } = null!;
    public int Value { get; set; }                          // 1 or -1
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Comment
{
    public int Id { get; set; }
    public int AnswerId { get; set; }
    public Answer Answer { get; set; } = null!;
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Body { get; set; } = "";                  // plain text, 1000 chars max
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string Color { get; set; } = "#5457D6";
    public int QuestionCount { get; set; }
    public List<QuestionTag> QuestionTags { get; set; } = [];
}

public class QuestionTag
{
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

public class QuestionFollow
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Bookmark
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    Answer = 1,       // someone answered your (or followed) question
    Upvote = 2,       // someone upvoted your answer
    Comment = 3,      // someone commented on your answer
    Accepted = 4,     // your answer was marked "worked"
    Follow = 5        // someone followed your question
}

/// <summary>Key-value store for runtime-editable settings (e.g. SMTP configuration).</summary>
public class AppSetting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class Notification
{
    public long Id { get; set; }
    public int UserId { get; set; }                        // recipient
    public User User { get; set; } = null!;
    public int ActorId { get; set; }                       // who caused it
    public User Actor { get; set; } = null!;
    public NotificationType Type { get; set; }
    public int QuestionId { get; set; }
    public string QuestionTitle { get; set; } = "";
    public int? AnswerId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
