namespace AskFix.Api.Auth;

/// <summary>Validates Windows/domain credentials and resolves the user's directory profile.</summary>
public interface IDirectoryService
{
    /// <summary>Accepts "DOMAIN\user", "user@domain" or plain "user" (DefaultDomain applies).
    /// Returns null when credentials are wrong or the account is disabled.</summary>
    DirectoryProfile? Validate(string username, string password);

    /// <summary>True when running in local development mode (login page shows demo hints).</summary>
    bool IsDevMode { get; }
}
