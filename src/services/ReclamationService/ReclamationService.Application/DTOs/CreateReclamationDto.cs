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

    public bool IsBlocking { get; set; }

    [Range(0, 100)]
    public int FollowUpCount { get; set; }

    [StringLength(150)]
    public string? ProductName { get; set; }

    [StringLength(64)]
    public string? Barcode { get; set; }

    [StringLength(500)]
    public string? ProductImageUrl { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    public string? ProductReference { get; set; }

    [StringLength(150)]
    public string? SellerName { get; set; }

    [StringLength(500)]
    public string? PurchaseProofUrl { get; set; }
}
