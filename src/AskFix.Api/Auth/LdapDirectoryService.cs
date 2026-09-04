using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using Microsoft.Extensions.Options;

namespace AskFix.Api.Auth;

/// <summary>Validates credentials against Active Directory by binding with the user's own
/// credentials (no service account needed) and reading their profile from the directory.
/// Windows-only; selected via Auth:Mode = "Ldap" (production).</summary>
[SupportedOSPlatform("windows")]
public class LdapDirectoryService(IOptions<AuthOptions> options, ILogger<LdapDirectoryService> logger) : IDirectoryService
{
    public bool IsDevMode => false;

    public DirectoryProfile? Validate(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return null;

        var (domain, sam) = ParseUsername(username.Trim());
        domain = string.IsNullOrEmpty(domain) ? (options.Value.DefaultDomain ?? "").Trim() : domain;
        if (string.IsNullOrEmpty(sam)) return null;

        try
        {
            using var context = string.IsNullOrEmpty(domain) || !string.IsNullOrEmpty(options.Value.LdapServer)
                ? new PrincipalContext(ContextType.Domain, options.Value.LdapServer ?? domain)
                : new PrincipalContext(ContextType.Domain, domain);

            // Catches bad password, disabled account, locked out, expired password.
            if (!context.ValidateCredentials($"{domain}\\{sam}", password, ContextOptions.Negotiate))
                return null;

            var principal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, sam)
                            ?? UserPrincipal.FindByIdentity(context, $"{domain}\\{sam}");
            if (principal is null)
                return new DirectoryProfile($"{domain}\\{sam}".ToLowerInvariant(), sam, "", "", IsAdmin: false);

            string email = principal.EmailAddress ?? "";
            string dept = "";
            string display = principal.DisplayName ?? principal.Name ?? sam;
            try
            {
                if (principal.GetUnderlyingObject() is DirectoryEntry entry)
                {
                    dept = entry.Properties["department"]?.Value as string ?? "";
                    if (string.IsNullOrEmpty(email))
                        email = entry.Properties["mail"]?.Value as string ?? "";
                    if (string.IsNullOrWhiteSpace(display) || display == sam)
                        display = entry.Properties["displayName"]?.Value as string ?? sam;
                }
            }
            catch
            {
                // profile enrichment is best-effort; login already succeeded
            }

            return new DirectoryProfile($"{domain}\\{sam}".ToLowerInvariant(), display, email, dept,
                IsAdmin: false); // set to a security group check if admins are needed
        }
        catch (PrincipalServerDownException ex)
        {
            logger.LogError(ex, "Domain controller unreachable for domain {Domain}", domain);
            throw new InvalidOperationException("Domain controller is not reachable. Contact IT.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AD validation failed for user {User}", sam);
            return null;
        }
    }

    private (string domain, string sam) ParseUsername(string username)
    {
        // "CORP\jdoe" -> (CORP, jdoe); "jdoe@corp.example.com" -> (corp.example.com, jdoe)
        if (username.Contains('\\'))
        {
            var parts = username.Split('\\', 2);
            return (parts[0].Trim(), parts[1].Trim());
        }
        var at = username.IndexOf('@');
        if (at > 0 && at < username.Length - 1)
            return (username[(at + 1)..].Trim(), username[..at].Trim());
        return ("", username);
    }
}
