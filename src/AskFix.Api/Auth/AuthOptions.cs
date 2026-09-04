namespace AskFix.Api.Auth;

public class AuthOptions
{
    /// <summary>"Ldap" = validate against Active Directory (production). "Dev" = seeded demo users (local development).</summary>
    public string Mode { get; set; } = "Ldap";

    /// <summary>Domain used when the user signs in without one ("jdoe" -> CORP\jdoe).</summary>
    public string DefaultDomain { get; set; } = "";

    /// <summary>Optional explicit domain controller host; empty = auto-locate.</summary>
    public string? LdapServer { get; set; }
}

/// <summary>Identity attributes resolved from the directory after a successful login.</summary>
public record DirectoryProfile(string SamAccountName, string DisplayName, string Email, string Department, bool IsAdmin);
