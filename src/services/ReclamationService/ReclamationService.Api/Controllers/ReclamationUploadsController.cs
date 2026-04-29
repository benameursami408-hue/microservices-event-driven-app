using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/reclamations/upload")]
[Authorize]
public class ReclamationUploadsController : ControllerBase
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly HashSet<string> ProofExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf"
    };

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<object>> Upload([FromQuery] string kind = "image", IFormFile? file = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest("File is too large. Max 10 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        var isImage = string.Equals(kind, "image", StringComparison.OrdinalIgnoreCase);
        var allowed = isImage ? ImageExtensions : ProofExtensions;

        if (!allowed.Contains(extension))
        {
            return BadRequest("Unsupported file type.");
        }

        if (isImage && (file.ContentType == null || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Invalid image type.");
        }

        if (!isImage && file.ContentType != null)
        {
            var ok = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
            if (!ok)
            {
                return BadRequest("Invalid proof type.");
            }
        }

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsRoot = Path.Combine(webRoot, "uploads", "reclamations");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{(isImage ? "image" : "proof")}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
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
}
