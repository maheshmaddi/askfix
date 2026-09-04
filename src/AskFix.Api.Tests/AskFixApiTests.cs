using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AskFix.Api.Auth;
using AskFix.Api.Services.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AskFix.Api.Tests;

public class FakeEmailQueue : IEmailQueue
{
    public List<EmailJob> Jobs { get; } = [];
    public bool TryEnqueue(EmailJob job)
    {
        Jobs.Add(job);
        return true;
    }
}

public class FakeEmailSender : IEmailSender
{
    public List<(EmailJob Job, EmailSettings Settings)> Sent { get; } = [];
    public Task SendAsync(EmailJob job, EmailSettings settings, CancellationToken ct = default)
    {
        Sent.Add((job, settings));
        return Task.CompletedTask;
    }
}

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"askfix-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:Mode", "Dev");
        builder.UseSetting("Database:Path", _dbPath);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailQueue>();
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<FakeEmailQueue>();
            services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<FakeEmailQueue>());
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<FakeEmailSender>());
        });
    }

    public FakeEmailQueue EmailQueue => Services.GetRequiredService<FakeEmailQueue>();
    public FakeEmailSender EmailSender => Services.GetRequiredService<FakeEmailSender>();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
            if (File.Exists(_dbPath + "-wal")) File.Delete(_dbPath + "-wal");
            if (File.Exists(_dbPath + "-shm")) File.Delete(_dbPath + "-shm");
        }
        catch
        {
            /* best effort cleanup */
        }
    }
}

/// <summary>Per-user cookie session on top of the in-memory TestServer transport.</summary>
public class SessionHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    public CookieContainer Cookies { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookieHeader = Cookies.GetCookieHeader(request.RequestUri);
        if (!string.IsNullOrEmpty(cookieHeader)) request.Headers.Add("Cookie", cookieHeader);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            foreach (var cookie in setCookies)
            {
                try { Cookies.SetCookies(request.RequestUri, cookie); } catch (CookieException) { /* ignore malformed */ }
            }
        return response;
    }
}

/// <summary>One factory + one seeded DB for the whole suite; each test gets fresh HTTP clients (fresh sessions).</summary>
[Collection("api")]
public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private HttpClient Client() => factory.CreateDefaultClient();

    private static async Task<HttpClient> LoginAsync(ApiFactory factory, string username)
    {
        var client = new HttpClient(new SessionHandler(factory.Server.CreateHandler()))
        {
            BaseAddress = factory.Server.BaseAddress,
        };
        var res = await client.PostAsJsonAsync("/api/auth/login", new { username, password = DevDirectoryService.DemoPassword });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return client;
    }

    // ---- auth -------------------------------------------------------------------------

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var res = await Client().PostAsJsonAsync("/api/auth/login", new { username = "corp\\mahesh", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithPlainSamAccount_ReturnsProfile()
    {
        var res = await Client().PostAsJsonAsync("/api/auth/login", new { username = "corp\\mahesh", password = DevDirectoryService.DemoPassword });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Mahesh Patil", body.GetProperty("displayName").GetString());
        Assert.Equal("Developer", body.GetProperty("department").GetString());
        Assert.True(body.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Me_WithoutSession_Returns401()
    {
        var res = await Client().GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_SetsSessionCookie_AndMeWorks()
    {
        var client = await LoginAsync(factory, "corp\\mahesh");
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("Mahesh Patil", me.GetProperty("displayName").GetString());
    }

    // ---- feed & questions ---------------------------------------------------------------

    [Fact]
    public async Task Feed_ReturnsSeededQuestions()
    {
        var feed = await Client().GetFromJsonAsync<JsonElement>("/api/feed?pageSize=50");
        Assert.True(feed.GetProperty("total").GetInt32() >= 12, "seed should provide at least 12 questions");
        Assert.True(feed.GetProperty("items")[0].TryGetProperty("title", out _));
    }

    [Fact]
    public async Task CreateQuestion_WithTags_SavesAndShowsSimilar()
    {
        var client = await LoginAsync(factory, "corp\\arjun.p");
        var res = await client.PostAsJsonAsync("/api/questions", new
        {
            title = "How do I export the Jenkins pipeline logs to a shared drive?",
            bodyHtml = "<p>Need the logs archived nightly.</p>",
            tagNames = new[] { "Jenkins", "Windows" },
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var q = await res.Content.ReadFromJsonAsync<JsonElement>();
        var id = q.GetProperty("id").GetInt32();
        Assert.Equal(2, q.GetProperty("tags").GetArrayLength());

        var similar = await client.GetFromJsonAsync<JsonElement>("/api/questions/similar?q=jenkins pipeline logs export");
        Assert.True(similar.GetArrayLength() >= 1, "similar-questions should match the just-created Jenkins question");
        Assert.Contains(similar.EnumerateArray(), s => s.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task CreateQuestion_WithoutTags_IsRejected()
    {
        var client = await LoginAsync(factory, "corp\\mahesh");
        var res = await client.PostAsJsonAsync("/api/questions", new { title = "A question without any tags at all", bodyHtml = (string?)null, tagNames = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- answers, votes, accept -----------------------------------------------------------

    private static async Task<(HttpClient asker, HttpClient answerer, int questionId, int answerId)> SetupQandA(
        ApiFactory factory, string answererSam = "corp\\priya.s")
    {
        var asker = await LoginAsync(factory, "corp\\rahul.v");
        var res = await asker.PostAsJsonAsync("/api/questions", new
        {
            title = "Wireshark missing from the standard laptop image — where is it now?",
            bodyHtml = "<p>Can't capture traffic after the reimage.</p>",
            tagNames = new[] { "Windows", "Access" },
        });
        var question = await res.Content.ReadFromJsonAsync<JsonElement>();
        var questionId = question.GetProperty("id").GetInt32();

        var answerer = await LoginAsync(factory, answererSam);
        var aRes = await answerer.PostAsJsonAsync($"/api/questions/{questionId}/answers",
            new { bodyHtml = "<p>It moved to Company Portal → Network Tools. Install from there, no admin needed.</p>" });
        var answer = await aRes.Content.ReadFromJsonAsync<JsonElement>();
        return (asker, answerer, questionId, answer.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Vote_UpvoteIncrementsCount_ToggleRemovesIt()
    {
        var (_, voter, _, answerId) = await SetupQandA(factory);

        var on = await voter.PostAsJsonAsync($"/api/answers/{answerId}/vote", new { value = 1 });
        var onBody = await on.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, onBody.GetProperty("myVote").GetInt32());
        Assert.Equal(1, onBody.GetProperty("score").GetInt32());

        var off = await voter.PostAsJsonAsync($"/api/answers/{answerId}/vote", new { value = 1 }); // same arrow toggles off
        var offBody = await off.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, offBody.GetProperty("myVote").GetInt32());
        Assert.Equal(0, offBody.GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task Accept_OnlyAllowedForAsker_AndNotifiesAnswerAuthor()
    {
        // answerer is a regular user (priya is admin since the seed fix, and admins may accept anything)
        var (asker, answerer, questionId, answerId) = await SetupQandA(factory, "corp\\meera.i");

        // the answerer is NOT the asker -> forbidden
        var forbidden = await answerer.PostAsJsonAsync($"/api/answers/{answerId}/accept", new { });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        // the asker accepts
        var accepted = await asker.PostAsJsonAsync($"/api/answers/{answerId}/accept", new { });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var acceptedBody = await accepted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(acceptedBody.GetProperty("enabled").GetBoolean());

        // question now flagged as having a working answer
        var q = await asker.GetFromJsonAsync<JsonElement>($"/api/questions/{questionId}");
        Assert.True(q.GetProperty("hasAccepted").GetBoolean());

        // answer author got an Accepted notification
        var notifications = await answerer.GetFromJsonAsync<JsonElement>("/api/notifications");
        Assert.Contains(notifications.GetProperty("items").EnumerateArray(),
            n => n.GetProperty("type").GetString() == "Accepted");
    }

    [Fact]
    public async Task Answering_AskersQuestion_CreatesAnswerNotification()
    {
        var asker = await LoginAsync(factory, "corp\\sneha.r");
        var res = await asker.PostAsJsonAsync("/api/questions", new
        {
            title = "Teams recording button greyed out in meeting rooms",
            bodyHtml = "<p>Only in room bookings, fine on laptops.</p>",
            tagNames = new[] { "Outlook" },
        });
        var question = await res.Content.ReadFromJsonAsync<JsonElement>();
        var questionId = question.GetProperty("id").GetInt32();

        var helper = await LoginAsync(factory, "corp\\priya.s");
        await helper.PostAsJsonAsync($"/api/questions/{questionId}/answers", new { bodyHtml = "<p>Room policy disables recording; IT can flip it per room on request.</p>" });

        var notifications = await asker.GetFromJsonAsync<JsonElement>("/api/notifications");
        Assert.Contains(notifications.GetProperty("items").EnumerateArray(),
            n => n.GetProperty("type").GetString() == "Answer" && n.GetProperty("questionId").GetInt32() == questionId);
    }

    [Fact]
    public async Task Comments_AddAndNotify()
    {
        var (_, answerer, _, answerId) = await SetupQandA(factory);
        var commenter = await LoginAsync(factory, "corp\\arjun.p");

        var res = await commenter.PostAsJsonAsync($"/api/answers/{answerId}/comments", new { body = "Worked for me too, thanks!" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var notifications = await answerer.GetFromJsonAsync<JsonElement>("/api/notifications");
        Assert.Contains(notifications.GetProperty("items").EnumerateArray(), n => n.GetProperty("type").GetString() == "Comment");
    }

    // ---- search ------------------------------------------------------------------------

    [Fact]
    public async Task Search_FindsSeededContentViaFts()
    {
        var client = await LoginAsync(factory, "corp\\mahesh");

        var proxy = await client.GetFromJsonAsync<JsonElement>("/api/search?q=ETIMEDOUT%20proxy");
        Assert.True(proxy.GetProperty("questions").GetArrayLength() >= 1, "FTS should find the npm proxy question");

        var vpn = await client.GetFromJsonAsync<JsonElement>("/api/search?q=vpn%20disconnects");
        Assert.True(vpn.GetProperty("questions").GetArrayLength() >= 1, "FTS should find the VPN question");
        Assert.True(vpn.GetProperty("tags").GetArrayLength() >= 1, "tag 'VPN' should match");

        var shortQuery = await client.GetFromJsonAsync<JsonElement>("/api/search?q=v");
        Assert.Equal(0, shortQuery.GetProperty("total").GetInt32());
    }

    // ---- follow & bookmarks --------------------------------------------------------------

    [Fact]
    public async Task FollowAndBookmark_Toggle()
    {
        var client = await LoginAsync(factory, "corp\\meera.i");
        var feed = await client.GetFromJsonAsync<JsonElement>("/api/feed?pageSize=1");
        var questionId = feed.GetProperty("items")[0].GetProperty("id").GetInt32();

        var follow = await client.PostAsJsonAsync($"/api/questions/{questionId}/follow", new { });
        Assert.True((await follow.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enabled").GetBoolean());
        var unfollow = await client.PostAsJsonAsync($"/api/questions/{questionId}/follow", new { });
        Assert.False((await unfollow.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enabled").GetBoolean());

        var bm = await client.PostAsJsonAsync($"/api/questions/{questionId}/bookmark", new { });
        Assert.True((await bm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enabled").GetBoolean());
        var saved = await client.GetFromJsonAsync<JsonElement>("/api/users/me/bookmarks");
        Assert.Contains(saved.EnumerateArray(), q => q.GetProperty("id").GetInt32() == questionId);
    }

    // ---- profiles ------------------------------------------------------------------------

    [Fact]
    public async Task Profile_ReturnsAggregatedStats()
    {
        var profile = await Client().GetFromJsonAsync<JsonElement>("/api/users/1");
        Assert.Equal("Mahesh Patil", profile.GetProperty("displayName").GetString());
        Assert.True(profile.GetProperty("questionCount").GetInt32() >= 1);
        Assert.True(profile.GetProperty("answerCount").GetInt32() >= 1);
        Assert.True(profile.GetProperty("answersAccepted").GetInt32() >= 1);
    }

    // ---- admin ---------------------------------------------------------------------------

    private static async Task<HttpClient> LoginAdminAsync(ApiFactory factory) => await LoginAsync(factory, "corp\\priya.s");

    [Fact]
    public async Task Admin_Endpoints_ForbiddenForRegularUsers()
    {
        var user = await LoginAsync(factory, "corp\\mahesh");
        foreach (var url in new[] { "/api/admin/stats", "/api/admin/users", "/api/admin/tags", "/api/admin/content", "/api/admin/email-settings" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync(url)).StatusCode);
        }
    }

    [Fact]
    public async Task Admin_PromoteDemote_WorksWithReLogin()
    {
        var admin = await LoginAdminAsync(factory);

        // find arjun
        var users = await admin.GetFromJsonAsync<JsonElement>("/api/admin/users?query=arjun");
        var arjun = users.GetProperty("items").EnumerateArray().First(u => u.GetProperty("sam").GetString() == "corp\\arjun.p");
        var arjunId = arjun.GetProperty("id").GetInt32();
        Assert.False(arjun.GetProperty("isAdmin").GetBoolean());

        // priya (seeded admin) promotes him
        var promote = await admin.PostAsJsonAsync($"/api/admin/users/{arjunId}/toggle-admin", new { });
        Assert.True((await promote.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enabled").GetBoolean());

        // after re-login arjun carries the role and can use admin endpoints
        var arjunClient = await LoginAsync(factory, "corp\\arjun.p");
        var me = await arjunClient.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.True(me.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await arjunClient.GetAsync("/api/admin/stats")).StatusCode);

        // demote back, and self-demotion is rejected
        await admin.PostAsJsonAsync($"/api/admin/users/{arjunId}/toggle-admin", new { });
        var selfDemote = await admin.PostAsJsonAsync($"/api/admin/users/2/toggle-admin", new { }); // priya's own id
        Assert.Equal(HttpStatusCode.BadRequest, selfDemote.StatusCode);
    }

    [Fact]
    public async Task Admin_Users_SearchFindsByPartialName()
    {
        var admin = await LoginAdminAsync(factory);
        var users = await admin.GetFromJsonAsync<JsonElement>("/api/admin/users?query=Meera");
        var row = Assert.Single(users.GetProperty("items").EnumerateArray());
        Assert.Equal("Meera Iyer", row.GetProperty("displayName").GetString());
        Assert.True(row.GetProperty("questionCount").GetInt32() >= 1);
        Assert.True(row.GetProperty("answerCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task Admin_Tag_EditMergeDelete()
    {
        var admin = await LoginAdminAsync(factory);

        // fresh tags via a new question
        var author = await LoginAsync(factory, "corp\\arjun.p");
        var created = await author.PostAsJsonAsync("/api/questions", new
        {
            title = "Where are the license servers for the design tools?",
            bodyHtml = "<p>Need them for the firewall exception.</p>",
            tagNames = new[] { "DesignSuite", "FirewallX" },
        });
        var q = await created.Content.ReadFromJsonAsync<JsonElement>();
        var tags = await admin.GetFromJsonAsync<JsonElement>("/api/admin/tags");
        var design = tags.EnumerateArray().First(t => t.GetProperty("name").GetString() == "DesignSuite");
        var firewall = tags.EnumerateArray().First(t => t.GetProperty("name").GetString() == "FirewallX");
        var designId = design.GetProperty("id").GetInt32();
        var firewallId = firewall.GetProperty("id").GetInt32();

        // invalid color rejected
        var bad = await admin.PutAsJsonAsync($"/api/admin/tags/{designId}", new { name = "DesignSuite", color = "red", description = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        // valid edit
        var edit = await admin.PutAsJsonAsync($"/api/admin/tags/{designId}", new { name = "Design Suite", color = "#7C4FD8", description = "Design tools licensing" });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var edited = await edit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("design-suite", edited.GetProperty("slug").GetString());

        // delete blocked while questions attached
        var blocked = await admin.DeleteAsync($"/api/admin/tags/{designId}");
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);

        // merge DesignSuite -> FirewallX: question keeps one tag, source disappears
        var merge = await admin.PostAsJsonAsync($"/api/admin/tags/{designId}/merge", new { targetTagId = firewallId });
        Assert.Equal(HttpStatusCode.OK, merge.StatusCode);
        var after = await admin.GetFromJsonAsync<JsonElement>("/api/admin/tags");
        Assert.DoesNotContain(after.EnumerateArray(), t => t.GetProperty("id").GetInt32() == designId);
        var firewallAfter = after.EnumerateArray().First(t => t.GetProperty("id").GetInt32() == firewallId);
        Assert.Equal(1, firewallAfter.GetProperty("questionCount").GetInt32());

        // question now shows only the target tag
        var question = await author.GetFromJsonAsync<JsonElement>($"/api/questions/{q.GetProperty("id").GetInt32()}");
        Assert.Single(question.GetProperty("tags").EnumerateArray());

        // orphan lifecycle: attached tag can't be deleted; after its question goes away it can
        var orphanRes = await author.PostAsJsonAsync("/api/questions", new
        {
            title = "Temporary question that creates an orphanable tag for cleanup",
            bodyHtml = "<p>temp</p>",
            tagNames = new[] { "OrphanTag" },
        });
        var orphanQ = await orphanRes.Content.ReadFromJsonAsync<JsonElement>();
        var tags2 = await admin.GetFromJsonAsync<JsonElement>("/api/admin/tags");
        var orphanTag = tags2.EnumerateArray().First(t => t.GetProperty("name").GetString() == "OrphanTag");
        var orphanTagId = orphanTag.GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/admin/tags/{orphanTagId}")).StatusCode); // still attached

        await author.DeleteAsync($"/api/questions/{orphanQ.GetProperty("id").GetInt32()}"); // cascade removes the link
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/tags/{orphanTagId}")).StatusCode);
    }

    [Fact]
    public async Task Admin_Stats_And_Content_List()
    {
        var admin = await LoginAdminAsync(factory);
        var stats = await admin.GetFromJsonAsync<JsonElement>("/api/admin/stats");
        Assert.True(stats.GetProperty("stats").GetProperty("questions").GetInt32() >= 12);
        Assert.NotEmpty(stats.GetProperty("topContributors").EnumerateArray());
        Assert.NotEmpty(stats.GetProperty("recentActivity").EnumerateArray());

        var questions = await admin.GetFromJsonAsync<JsonElement>("/api/admin/content?type=question&query=VPN");
        Assert.True(questions.GetProperty("total").GetInt32() >= 1);
        var answers = await admin.GetFromJsonAsync<JsonElement>("/api/admin/content?type=answer&query=AnyConnect");
        Assert.True(answers.GetProperty("total").GetInt32() >= 1);
    }

    // ---- notification preferences -----------------------------------------------------------

    [Fact]
    public async Task Prefs_SaveAndLoad()
    {
        var client = await LoginAsync(factory, "corp\\sneha.r");
        var initial = await client.GetFromJsonAsync<JsonElement>("/api/settings/notifications");
        Assert.True(initial.GetProperty("emailOnAnswer").GetBoolean());

        await client.PutAsJsonAsync("/api/settings/notifications", new { emailOnAnswer = false, emailOnComment = false, emailOnAccepted = true });
        var updated = await client.GetFromJsonAsync<JsonElement>("/api/settings/notifications");
        Assert.False(updated.GetProperty("emailOnAnswer").GetBoolean());
        Assert.False(updated.GetProperty("emailOnComment").GetBoolean());
        Assert.True(updated.GetProperty("emailOnAccepted").GetBoolean());

        // unauthenticated access rejected
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client().GetAsync("/api/settings/notifications")).StatusCode);
    }

    // ---- email settings & delivery ------------------------------------------------------------

    private static async Task EnableEmailAsync(HttpClient admin)
    {
        var res = await admin.PutAsJsonAsync("/api/admin/email-settings", new
        {
            enabled = true, host = "smtp.corp.example", port = 25, username = "askfix", password = "secret123",
            useSsl = false, fromAddress = "askfix@corp.example", fromName = "AskFix", baseUrl = "http://askfix.corp.example:8080",
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task EmailSettings_Validate_Mask_And_Keep()
    {
        var admin = await LoginAdminAsync(factory);

        // reset first: other tests in this shared-DB suite may have enabled email already
        var reset = await admin.PutAsJsonAsync("/api/admin/email-settings", new
        {
            enabled = false, host = "", port = 25, username = "", password = "", useSsl = false,
            fromAddress = "", fromName = "AskFix", baseUrl = "",
        });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var initial = await admin.GetFromJsonAsync<JsonElement>("/api/admin/email-settings");
        Assert.False(initial.GetProperty("enabled").GetBoolean()); // reset above (password may persist by design)

        // enabling without host is rejected
        var invalid = await admin.PutAsJsonAsync("/api/admin/email-settings", new
        {
            enabled = true, host = "", port = 25, username = "", password = "", useSsl = false,
            fromAddress = "askfix@corp.example", fromName = "AskFix", baseUrl = "http://x",
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        await EnableEmailAsync(admin);
        var saved = await admin.GetFromJsonAsync<JsonElement>("/api/admin/email-settings");
        Assert.True(saved.GetProperty("enabled").GetBoolean());
        Assert.True(saved.GetProperty("hasPassword").GetBoolean());

        // re-save with empty password keeps the stored one
        var res = await admin.PutAsJsonAsync("/api/admin/email-settings", new
        {
            enabled = true, host = "smtp.corp.example", port = 587, username = "askfix", password = "",
            useSsl = true, fromAddress = "askfix@corp.example", fromName = "AskFix", baseUrl = "http://askfix.corp.example:8080",
        });
        var kept = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(kept.GetProperty("hasPassword").GetBoolean());
    }

    [Fact]
    public async Task Email_EnqueuedOnlyForOptedInRecipients()
    {
        var admin = await LoginAdminAsync(factory);
        await EnableEmailAsync(admin);
        factory.EmailQueue.Jobs.Clear();

        var (asker, _, questionId, _) = await SetupQandA(factory); // rahul asks, priya answers
        Assert.Contains(factory.EmailQueue.Jobs, j => j.To == "rahul.verma@corp.example");

        // opt rahul out of answer emails, ask again -> no email for him
        await asker.PutAsJsonAsync("/api/settings/notifications", new { emailOnAnswer = false, emailOnComment = true, emailOnAccepted = true });
        factory.EmailQueue.Jobs.Clear();

        var helper = await LoginAsync(factory, "corp\\meera.i");
        await helper.PostAsJsonAsync($"/api/questions/{questionId}/answers", new { bodyHtml = "<p>Also check the licensing service, it can block the tool.</p>" });
        Assert.DoesNotContain(factory.EmailQueue.Jobs, j => j.To == "rahul.verma@corp.example");
    }

    [Fact]
    public async Task Email_TestSendUsesFakeSender()
    {
        var admin = await LoginAdminAsync(factory);
        await EnableEmailAsync(admin);

        var res = await admin.PostAsJsonAsync("/api/admin/email-settings/test", new { });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var sent = Assert.Single(factory.EmailSender.Sent);
        Assert.Equal("priya.sharma@corp.example", sent.Job.To);
        Assert.Contains("AskFix", sent.Job.Subject);
    }
}
