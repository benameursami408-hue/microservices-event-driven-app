using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/reclamations/upload")]
[Authorize]
[EnableRateLimiting("Uploads")]
public class ReclamationUploadsController : ControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly HashSet<string> ProofExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf"
    };

    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<object>> Upload([FromQuery] string kind = "image", IFormFile? file = null, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (file.Length > MaxUploadBytes)
        {
            return BadRequest("File is too large. Max 10 MB.");
        }

        var isImage = string.Equals(kind, "image", StringComparison.OrdinalIgnoreCase);
        var isProof = string.Equals(kind, "proof", StringComparison.OrdinalIgnoreCase);
        if (!isImage && !isProof)
        {
            return BadRequest("Invalid upload kind. Use image or proof.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = isImage ? ImageExtensions : ProofExtensions;
        if (string.IsNullOrWhiteSpace(extension) || !allowed.Contains(extension))
        {
            return BadRequest("Unsupported file type.");
        }

        if (!IsAllowedContentType(file.ContentType, extension, isImage))
        {
            return BadRequest(isImage ? "Invalid image type." : "Invalid proof type.");
        }

        if (!await HasAllowedSignatureAsync(file, extension, cancellationToken))
        {
            return BadRequest("The file content does not match the declared file type.");
        }

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsRoot = Path.Combine(webRoot, "uploads", "reclamations");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{(isImage ? "image" : "proof")}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"/uploads/reclamations/{fileName}";
        return Ok(new
        {
            url,
            fileName,
            size = file.Length,
            contentType = file.ContentType
        });
    }

    private static bool IsAllowedContentType(string? contentType, string extension, bool isImage)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;

        var normalized = contentType.Split(';')[0].Trim().ToLowerInvariant();
        if (isImage)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => normalized is "image/jpeg" or "image/pjpeg",
                ".png" => normalized == "image/png",
                ".gif" => normalized == "image/gif",
                ".webp" => normalized == "image/webp",
                _ => false
            };
        }

        return extension == ".pdf"
            ? normalized == "application/pdf"
            : IsAllowedContentType(contentType, extension, isImage: true);
    }

    private static async Task<bool> HasAllowedSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        var buffer = new byte[16];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

        return extension switch
        {
            ".jpg" or ".jpeg" => read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".png" => read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 && buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A,
            ".gif" => read >= 6 && buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38 && (buffer[4] == 0x37 || buffer[4] == 0x39) && buffer[5] == 0x61,
            ".webp" => read >= 12 && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50,
            ".pdf" => read >= 5 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46 && buffer[4] == 0x2D,
            _ => false
        };
    }
}
