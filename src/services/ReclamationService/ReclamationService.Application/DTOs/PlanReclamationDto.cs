using System.ComponentModel.DataAnnotations;

namespace ReclamationService.Application.DTOs;

public class PlanReclamationDto
{
    [Required]
    public long TechnicianId { get; set; }

    [StringLength(100)]
    public string? TechnicianName { get; set; }

    [Required]
    public DateTime PlannedStartAt { get; set; }

    public DateTime? PlannedEndAt { get; set; }

    [StringLength(500)]
    public string? PlanningNote { get; set; }
}
