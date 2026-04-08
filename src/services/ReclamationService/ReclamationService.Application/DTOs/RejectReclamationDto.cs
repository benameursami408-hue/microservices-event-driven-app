using System.ComponentModel.DataAnnotations;

namespace ReclamationService.Application.DTOs;

public class RejectReclamationDto
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
