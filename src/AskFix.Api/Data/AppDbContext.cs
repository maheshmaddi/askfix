using AskFix.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<AnswerVote> AnswerVotes => Set<AnswerVote>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<QuestionTag> QuestionTags => Set<QuestionTag>();
    public DbSet<QuestionFollow> QuestionFollows => Set<QuestionFollow>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.SamAccountName).IsUnique();
            e.Property(x => x.SamAccountName).HasMaxLength(120);
            e.Property(x => x.DisplayName).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Department).HasMaxLength(120);
        });

        b.Entity<Question>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(300);
            e.HasOne(x => x.Author).WithMany(u => u.Questions).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.LastActivityAt);
        });

        b.Entity<Answer>(e =>
        {
            e.HasOne(x => x.Question).WithMany(q => q.Answers).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany(u => u.Answers).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.QuestionId);
        });

        b.Entity<AnswerVote>(e =>
        {
            e.HasKey(x => new { x.UserId, x.AnswerId });
            e.HasOne(x => x.Answer).WithMany(a => a.Votes).HasForeignKey(x => x.AnswerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("AnswerVotes");
        });

        b.Entity<Comment>(e =>
        {
            e.Property(x => x.Body).HasMaxLength(1000);
            e.HasOne(x => x.Answer).WithMany(a => a.Comments).HasForeignKey(x => x.AnswerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Tag>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(50);
            e.Property(x => x.Slug).HasMaxLength(60);
            e.Property(x => x.Color).HasMaxLength(20);
        });

        b.Entity<QuestionTag>(e =>
        {
            e.HasKey(x => new { x.QuestionId, x.TagId });
            e.HasOne(x => x.Question).WithMany(q => q.QuestionTags).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany(t => t.QuestionTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable("QuestionTags");
        });

        b.Entity<QuestionFollow>(e =>
        {
            e.HasKey(x => new { x.UserId, x.QuestionId });
            e.HasOne(x => x.Question).WithMany(q => q.Follows).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("QuestionFollows");
        });

        b.Entity<Bookmark>(e =>
        {
            e.HasKey(x => new { x.UserId, x.QuestionId });
            e.HasOne(x => x.Question).WithMany(q => q.Bookmarks).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("Bookmarks");
        });

        b.Entity<Notification>(e =>
        {
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.QuestionTitle).HasMaxLength(300);
            e.HasIndex(x => new { x.UserId, x.IsRead });
        });

        b.Entity<AppSetting>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(60);
            e.Property(x => x.Value).HasMaxLength(8000);
        });
    }
}
