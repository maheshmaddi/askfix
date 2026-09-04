namespace AskFix.Api.Services.Email;

/// <summary>SMTP configuration, stored as one JSON row in AppSettings. Password is DPAPI-encrypted.</summary>
public class EmailSettings
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 25;
    public string Username { get; set; } = "";
    public string PasswordEnc { get; set; } = "";   // DPAPI-protected, base64
    public bool UseSsl { get; set; }
    public string FromAddress { get; set; } = "askfix@localhost";
    public string FromName { get; set; } = "AskFix";
    /// <summary>Absolute base URL used in email links, e.g. http://askfix.corp.example:8080</summary>
    public string BaseUrl { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(Host) && Port > 0;
}

public record EmailJob(string To, string Subject, string BodyHtml);

public interface IEmailQueue
{
    /// <summary>Non-blocking enqueue; returns false when the queue is full (job dropped + logged).</summary>
    bool TryEnqueue(EmailJob job);
}

public interface IEmailSender
{
    /// <summary>Sends one message using the given settings. Throws on failure.</summary>
    Task SendAsync(EmailJob job, EmailSettings settings, CancellationToken ct = default);
}
