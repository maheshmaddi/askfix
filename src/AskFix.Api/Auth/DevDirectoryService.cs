using Microsoft.Extensions.Options;

namespace AskFix.Api.Auth;

/// <summary>Local development directory: seeded demo accounts with a shared password,
/// so the app is fully testable without domain access. Selected via Auth:Mode = "Dev".</summary>
public class DevDirectoryService(IOptions<AuthOptions> options) : IDirectoryService
{
    public const string DemoPassword = "AskFix!123";

    private static readonly (string Sam, string Name, string Email, string Dept)[] DemoUsers =
    [
        ("corp\\mahesh",  "Mahesh Patil", "mahesh.patil@corp.example",  "Developer"),
        ("corp\\priya.s", "Priya Sharma", "priya.sharma@corp.example",  "IT Support"),
        ("corp\\rahul.v", "Rahul Verma",  "rahul.verma@corp.example",   "Developer"),
        ("corp\\meera.i", "Meera Iyer",   "meera.iyer@corp.example",    "DevOps Engineer"),
        ("corp\\arjun.p", "Arjun Patel",  "arjun.patel@corp.example",   "QA Engineer"),
        ("corp\\sneha.r", "Sneha Reddy",  "sneha.reddy@corp.example",   "Engineering Manager"),
    ];

    public bool IsDevMode => true;

    public DirectoryProfile? Validate(string username, string password)
    {
        if (!string.Equals(password, DemoPassword, StringComparison.Ordinal))
            return null;

        username = username.Trim().ToLowerInvariant().Replace('/', '\\');
        var match = DemoUsers.FirstOrDefault(u =>
            u.Sam == username ||
            u.Sam[(u.Sam.IndexOf('\\') + 1)..] == username ||
            string.IsNullOrEmpty(options.Value.DefaultDomain) == false &&
            $"{options.Value.DefaultDomain.ToLowerInvariant()}\\{username}" == u.Sam);
        if (match == default) return null;

        return new DirectoryProfile(match.Sam, match.Name, match.Email, match.Dept,
            IsAdmin: match.Sam == "corp\\priya.s");
    }
}
