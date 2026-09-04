using System.Security.Claims;

namespace AskFix.Api.Common;

public static class CurrentUser
{
    public const string IdClaim = "askfix_uid";

    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(IdClaim), out var id) ? id : null;

    public static bool GetIsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole("admin");

    /// <summary>Reputation badge ladder.</summary>
    public static string BadgeFor(int reputation) => reputation switch
    {
        >= 5000 => "Expert",
        >= 1000 => "Problem Solver",
        >= 200 => "Helper",
        >= 50 => "Contributor",
        _ => "Newcomer",
    };
}
