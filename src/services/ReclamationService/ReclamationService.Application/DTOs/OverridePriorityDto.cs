using System.ComponentModel.DataAnnotations;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.DTOs;

public class OverridePriorityDto
{
    [Required]
    [EnumDataType(typeof(NamePriority))]
    public NamePriority Priority { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
