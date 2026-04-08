using System.ComponentModel.DataAnnotations;

namespace ReclamationService.Application.DTOs;

public class AssignReclamationDto
{
    /// <summary>
    /// Optional when called by ADMIN to assign to a specific SAV.
    /// When called by SAV, the service will ignore these and assign to the caller.
    /// </summary>
    public long? SavId { get; set; }

    [StringLength(100)]
    public string? SavName { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}
