using AskFix.Api.Data;
using AskFix.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AskFix.Api.Controllers;

/// <summary>Inline image uploads for the answer editor (stored on disk under wwwroot/uploads).</summary>
[ApiController]
[Route("api/uploads")]
[Authorize]
public class UploadsController(IWebHostEnvironment env, ILogger<UploadsController> logger) : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];
    private const long MaxBytes = 2 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxBytes)]
    public async Task<ActionResult<UploadResult>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file received." });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = "Image must be 2 MB or smaller." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Only PNG, JPG, GIF or WebP images are allowed." });

        // sniff the header too — extension alone is not trustworthy
        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            await stream.ReadExactlyAsync(header);
        }
        if (!LooksLikeImage(header))
            return BadRequest(new { message = "This does not look like an image file." });

        var uploadsDir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsDir);

        var name = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploadsDir, name);
        await using (var target = System.IO.File.Create(path))
        {
            await file.CopyToAsync(target);
        }
        logger.LogInformation("Image upload {Name} ({Size} bytes) by {User}", name, file.Length, User.Identity?.Name);
        return Ok(new UploadResult($"/uploads/{name}"));
    }

    private static bool LooksLikeImage(ReadOnlySpan<byte> h) =>
        h[0] == 0x89 && h[1] == 0x50 || // PNG
        h[0] == 0xFF && h[1] == 0xD8 || // JPEG
        h[0] == 0x47 && h[1] == 0x49 || // GIF
        h[0] == 0x52 && h[1] == 0x49 && h[8] == 0x57 && h[9] == 0x45; // WebP (RIFF....WEBP)
}
