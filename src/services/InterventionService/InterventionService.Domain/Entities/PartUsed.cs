using System.ComponentModel.DataAnnotations;

namespace InterventionService.Domain.Entities;

public class PartUsed
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InterventionId { get; set; }

    [MaxLength(80)]
    public string PartCode { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Label { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    [MaxLength(40)]
    public string AvailabilityStatus { get; set; } = "Used";
}
