using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;

namespace AskFix.Api.Services.Email;

/// <summary>Loads/caches the SMTP settings row; encrypts the password with DPAPI (server-bound).</summary>
public class EmailSettingsService(IServiceScopeFactory scopeFactory, ILogger<EmailSettingsService> logger)
{
    private const string SettingsKey = "EmailSettings";
    private static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(20);

    // stable app-specific entropy (obfuscation; DPAPI LocalMachine provides the actual machine binding)
    private static readonly byte[] Entropy = System.Text.Encoding.UTF8.GetBytes("AskFix-SMTP-entropy-v1");

    private EmailSettings? _cached;
    private DateTime _cachedAt;
    private readonly object _lock = new();

    public EmailSettings Load()
    {
        lock (_lock)
        {
            if (_cached is not null && DateTime.UtcNow - _cachedAt < CacheTime) return _cached;
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var row = db.AppSettings.Find(SettingsKey);
            EmailSettings settings;
            try
            {
                settings = row is null || string.IsNullOrWhiteSpace(row.Value)
                    ? new EmailSettings()
                    : JsonSerializer.Deserialize<EmailSettings>(row.Value) ?? new EmailSettings();
            }
            catch (JsonException)
            {
                logger.LogWarning("EmailSettings row is corrupt, resetting");
                settings = new EmailSettings();
            }
            _cached = settings;
            _cachedAt = DateTime.UtcNow;
            return settings;
        }
    }

    public void Save(EmailSettings settings)
    {
        lock (_lock)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var row = db.AppSettings.Find(SettingsKey);
            var json = JsonSerializer.Serialize(settings);
            if (row is null)
            {
                db.AppSettings.Add(new Models.AppSetting { Key = SettingsKey, Value = json });
            }
            else
            {
                row.Value = json;
            }
            db.SaveChanges();
            _cached = settings;
            _cachedAt = DateTime.UtcNow;
        }
    }

    public string? DecryptPassword()
    {
        var enc = Load().PasswordEnc;
        if (string.IsNullOrEmpty(enc)) return null;
        try
        {
            if (!OperatingSystem.IsWindows()) return null;
            var plain = ProtectedData.Unprotect(Convert.FromBase64String(enc), Entropy, DataProtectionScope.LocalMachine);
            return System.Text.Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decrypt SMTP password (DPAPI)");
            return null;
        }
    }

    public string EncryptPassword(string password)
    {
        if (!OperatingSystem.IsWindows())
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password)); // dev fallback on non-Windows hosts
        return Convert.ToBase64String(ProtectedData.Protect(System.Text.Encoding.UTF8.GetBytes(password), Entropy, DataProtectionScope.LocalMachine));
    }
}

/// <summary>Sends via the in-box SMTP client — no external packages, works with internal relays.</summary>
public class SmtpEmailSender(EmailSettingsService settingsService, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailJob job, EmailSettings settings, CancellationToken ct = default)
    {
        using var message = new MailMessage();
        message.From = new MailAddress(settings.FromAddress, string.IsNullOrWhiteSpace(settings.FromName) ? "AskFix" : settings.FromName);
        message.To.Add(job.To);
        message.Subject = job.Subject;
        message.Body = job.BodyHtml;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(settings.Username),
            Timeout = 15_000,
        };
        if (!client.UseDefaultCredentials)
        {
            var password = settingsService.DecryptPassword() ?? "";
            client.Credentials = new NetworkCredential(settings.Username, password);
        }

        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP send to {To} via {Host}:{Port} failed", job.To, settings.Host, settings.Port);
            throw;
        }
    }
}
