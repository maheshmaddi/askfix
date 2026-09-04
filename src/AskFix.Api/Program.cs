using System.Threading.RateLimiting;
using AskFix.Api.Auth;
using AskFix.Api.Data;
using AskFix.Api.Services;
using AskFix.Api.Services.Email;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

#pragma warning disable CA1416 // LdapDirectoryService is Windows-only by design; deployment target is Windows

var builder = WebApplication.CreateBuilder(args);

// ---- configuration -------------------------------------------------------------------

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));

var dbPath = builder.Configuration["Database:Path"] ?? "askfix.db";
if (!Path.IsPathRooted(dbPath))
    dbPath = Path.Combine(builder.Environment.ContentRootPath, dbPath);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

// ---- auth: cookie session on top of a login page backed by AD ------------------------

var authMode = builder.Configuration["Auth:Mode"] ?? "Ldap";
if (string.Equals(authMode, "Dev", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IDirectoryService, DevDirectoryService>();
else
    builder.Services.AddSingleton<IDirectoryService, LdapDirectoryService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "askfix_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<EmailSettingsService>();
builder.Services.AddSingleton<ChannelEmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<ChannelEmailQueue>());
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<EmailWorker>(); // tests replace queue/sender with fakes
builder.Services.AddControllers();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
    });
}

var app = builder.Build();

// ---- database bootstrap ---------------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    DbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

// ---- pipeline -------------------------------------------------------------------------

if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();

if (app.Environment.WebRootPath is not null)
{
    var uploadsDir = Path.Combine(app.Environment.WebRootPath, "uploads");
    Directory.CreateDirectory(uploadsDir);

    app.UseStaticFiles(); // SPA assets
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsDir),
        RequestPath = "/uploads",
        OnPrepareResponse = ctx =>
            ctx.Context.Response.Headers.CacheControl = "public,max-age=604800", // 7 days
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SPA deep-link fallback (only when a client build is present, e.g. published output)
var indexPath = Path.Combine(app.Environment.WebRootPath ?? "", "index.html");
if (System.IO.File.Exists(indexPath))
    app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
