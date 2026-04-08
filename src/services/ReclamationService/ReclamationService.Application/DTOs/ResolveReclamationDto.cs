using System.ComponentModel.DataAnnotations;

namespace ReclamationService.Application.DTOs;

public class ResolveReclamationDto
{
    [Required]
    [StringLength(2000)]
    public string ResolutionNote { get; set; } = string.Empty;
}
