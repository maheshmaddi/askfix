using AskFix.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Data;

/// <summary>Creates the SQLite database on first run, sets up FTS5 full-text indexes and seeds sample content.</summary>
public static class DbInitializer
{
    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        CreateFts(db);

        if (db.Users.Any()) return;
        SeedData.Seed(db);
        RebuildFts(db);
    }

    private static void CreateFts(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE VIRTUAL TABLE IF NOT EXISTS QuestionsFts USING fts5(
                QuestionId UNINDEXED, Title, Body, tokenize='porter unicode61');
            CREATE TRIGGER IF NOT EXISTS QuestionsFts_ins AFTER INSERT ON Questions BEGIN
                INSERT INTO QuestionsFts(QuestionId, Title, Body) VALUES (new.Id, new.Title, new.BodyText);
            END;
            CREATE TRIGGER IF NOT EXISTS QuestionsFts_del AFTER DELETE ON Questions BEGIN
                DELETE FROM QuestionsFts WHERE QuestionId = old.Id;
            END;
            CREATE TRIGGER IF NOT EXISTS QuestionsFts_upd AFTER UPDATE OF Title, BodyText ON Questions BEGIN
                DELETE FROM QuestionsFts WHERE QuestionId = new.Id;
                INSERT INTO QuestionsFts(QuestionId, Title, Body) VALUES (new.Id, new.Title, new.BodyText);
            END;

            CREATE VIRTUAL TABLE IF NOT EXISTS AnswersFts USING fts5(
                AnswerId UNINDEXED, QuestionId UNINDEXED, Body, tokenize='porter unicode61');
            CREATE TRIGGER IF NOT EXISTS AnswersFts_ins AFTER INSERT ON Answers BEGIN
                INSERT INTO AnswersFts(AnswerId, QuestionId, Body) VALUES (new.Id, new.QuestionId, new.BodyText);
            END;
            CREATE TRIGGER IF NOT EXISTS AnswersFts_del AFTER DELETE ON Answers BEGIN
                DELETE FROM AnswersFts WHERE AnswerId = old.Id;
            END;
            CREATE TRIGGER IF NOT EXISTS AnswersFts_upd AFTER UPDATE OF BodyText ON Answers BEGIN
                DELETE FROM AnswersFts WHERE AnswerId = new.Id;
                INSERT INTO AnswersFts(AnswerId, QuestionId, Body) VALUES (new.Id, new.QuestionId, new.BodyText);
            END;
            """);
    }

    /// <summary>Rebuild FTS index from scratch (used after seeding; triggers keep it in sync afterwards).</summary>
    public static void RebuildFts(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            DELETE FROM QuestionsFts;
            INSERT INTO QuestionsFts(QuestionId, Title, Body)
                SELECT Id, Title, BodyText FROM Questions;
            DELETE FROM AnswersFts;
            INSERT INTO AnswersFts(AnswerId, QuestionId, Body)
                SELECT Id, QuestionId, BodyText FROM Answers;
            """);
    }
}
