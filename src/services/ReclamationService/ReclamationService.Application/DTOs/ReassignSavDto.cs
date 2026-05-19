using System.ComponentModel.DataAnnotations;

namespace ReclamationService.Application.DTOs;

public class ReassignSavDto
{
    [Range(1, long.MaxValue)]
    public long SavUserId { get; set; }

    [StringLength(100)]
    public string? SavUserName { get; set; }
}
