using System.ComponentModel.DataAnnotations;

namespace ReclamationService.Application.DTOs;

public class CloseReclamationDto
{
    [StringLength(500)]
    public string? Comment { get; set; }
}
