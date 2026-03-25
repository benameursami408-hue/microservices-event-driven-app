using System.ComponentModel.DataAnnotations;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class CreateReclamationDto
{
    [Required]
    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(NamePriority))]
    public NamePriority Priority { get; set; }

    [Required]
    public long ClientId { get; set; }

    [Required]
    [StringLength(100)]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    public long SAVId { get; set; }

    [Required]
    [StringLength(100)]
    public string SAVName { get; set; } = string.Empty;
}
