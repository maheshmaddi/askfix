using AskFix.Api.Data;
using AskFix.Api.Models;
using AskFix.Api.Services.Email;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AskFix.Api.Services;

/// <summary>Creates in-app notifications (never for your own action) and queues emails for
/// recipients whose per-user email preferences allow it.</summary>
public class NotificationService(AppDbContext db, IEmailQueue emailQueue, EmailSettingsService emailSettings)
{
    public const int AnswerRep = 2;
    public const int UpvoteRep = 10;
    public const int AcceptedRep = 25;

    public void QuestionAnswered(Question q, Answer answer)
    {
        q.LastActivityAt = DateTime.UtcNow;
        var recipients = q.Follows.Select(f => f.UserId).Append(q.AuthorId)
            .Where(id => id != answer.AuthorId).Distinct().ToList();
        foreach (var userId in recipients)
            db.Notifications.Add(new Notification
            {
                UserId = userId, ActorId = answer.AuthorId, Type = NotificationType.Answer,
                QuestionId = q.Id, QuestionTitle = q.Title, AnswerId = answer.Id,
            });
        QueueEmails(recipients, pref => pref.EmailOnAnswer, answer.Author.DisplayName,
            "answered your question:", q, $"{answer.Author.DisplayName} answered your question");
    }

    public void Upvoted(Answer answer, int voterId, bool added)
    {
        answer.Author.Reputation += added ? UpvoteRep : -UpvoteRep;
        if (!added || voterId == answer.AuthorId) return;
        db.Notifications.Add(new Notification
        {
            UserId = answer.AuthorId, ActorId = voterId, Type = NotificationType.Upvote,
            QuestionId = answer.QuestionId, QuestionTitle = answer.Question.Title, AnswerId = answer.Id,
        });
        // upvotes are in-app only — too noisy for email
    }

    public void Commented(Answer answer, Comment comment, string actorName)
    {
        answer.CommentCount++;
        answer.Question.LastActivityAt = DateTime.UtcNow;
        if (comment.AuthorId == answer.AuthorId) return;
        db.Notifications.Add(new Notification
        {
            UserId = answer.AuthorId, ActorId = comment.AuthorId, Type = NotificationType.Comment,
            QuestionId = answer.QuestionId, QuestionTitle = answer.Question.Title, AnswerId = answer.Id,
        });
        QueueEmails([answer.AuthorId], pref => pref.EmailOnComment, actorName,
            "commented on your answer:", answer.Question, $"{actorName} commented on your answer");
    }

    public void Accepted(Answer answer, bool accepted, string actorName)
    {
        answer.Author.Reputation += accepted ? AcceptedRep : -AcceptedRep;
        if (!accepted) return;
        db.Notifications.Add(new Notification
        {
            UserId = answer.AuthorId, ActorId = answer.Question.AuthorId, Type = NotificationType.Accepted,
            QuestionId = answer.QuestionId, QuestionTitle = answer.Question.Title, AnswerId = answer.Id,
        });
        QueueEmails([answer.AuthorId], pref => pref.EmailOnAccepted, actorName,
            "marked your answer as the fix:", answer.Question, $"Your answer was marked as the fix: {answer.Question.Title}");
    }

    public void QuestionFollowed(Question q, int followerId)
    {
        if (followerId == q.AuthorId) return;
        db.Notifications.Add(new Notification
        {
            UserId = q.AuthorId, ActorId = followerId, Type = NotificationType.Follow,
            QuestionId = q.Id, QuestionTitle = q.Title,
        });
        // follows are in-app only
    }

    // ---- email helpers --------------------------------------------------------------------

    private void QueueEmails(List<int> userIds, Expression<Func<User, bool>> prefAllowed, string actorName,
        string action, Question q, string subject)
    {
        var settings = emailSettings.Load();
        if (!settings.IsConfigured || string.IsNullOrWhiteSpace(settings.BaseUrl)) return;

        var targets = db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.Email != "")
            .Where(prefAllowed)
            .Select(u => u.Email)
            .ToList();
        if (targets.Count == 0) return;

        var url = $"{settings.BaseUrl.TrimEnd('/')}/question/{q.Id}";
        var manage = $"{settings.BaseUrl.TrimEnd('/')}/settings";
        var body = EmailTemplates.Notification(actorName, action, q.Title, url, manage);
        foreach (var to in targets)
            emailQueue.TryEnqueue(new EmailJob(to, subject, body));
    }
}
