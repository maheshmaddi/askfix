using System.Security.Claims;
using AskFix.Api.Auth;
using AskFix.Api.Common;
using AskFix.Api.Data;
using AskFix.Api.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AskFix.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IDirectoryService directory) : ControllerBase
{
    [HttpGet("info")]
    [AllowAnonymous]
    public ActionResult<ApiInfo> Info() => Ok(new ApiInfo("AskFix", typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0", directory.IsDevMode));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<MeResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        DirectoryProfile? profile;
        try
        {
            profile = directory.Validate(request.Username, request.Password);
        }
        catch (InvalidOperationException ex) // domain controller unreachable
        {
            return StatusCode(503, new { message = ex.Message });
        }
        if (profile is null)
            return Unauthorized(new { message = "Invalid username or password." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.SamAccountName == profile.SamAccountName);
        if (user is null)
        {
            user = new Models.User
            {
                SamAccountName = profile.SamAccountName,
                AvatarHue = Math.Abs(profile.SamAccountName.GetHashCode()) % 360,
                IsAdmin = profile.IsAdmin,
            };
            db.Users.Add(user);
        }
        user.DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.SamAccountName : profile.DisplayName;
        user.Email = profile.Email;
        user.Department = profile.Department;
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.DisplayName),
            new(CurrentUser.IdClaim, user.Id.ToString()),
        };
        if (user.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "admin"));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        return Ok(MeResponse.From(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var user = await ResolveUser();
        return user is null ? Unauthorized() : Ok(MeResponse.From(user));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var user = await ResolveUser();
        if (user is null) return Unauthorized();
        if (request.Bio is not null)
            user.Bio = request.Bio.Trim().Length > 400 ? request.Bio.Trim()[..400] : request.Bio.Trim();
        await db.SaveChangesAsync();
        return Ok(MeResponse.From(user));
    }

    private async Task<Models.User?> ResolveUser() =>
        User.GetUserId() is { } id ? await db.Users.FindAsync(id) : null;
}