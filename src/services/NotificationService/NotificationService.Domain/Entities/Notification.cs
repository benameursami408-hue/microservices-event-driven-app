using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class Notification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Logical type/category (e.g. WELCOME, RECLAMATION_CREATED, ADMIN_ALERT).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference to a user identifier (from AuthService).
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// Optional recipient email address.
    /// </summary>
    [MaxLength(320)]
    public string? RecipientEmail { get; set; }

    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    [MaxLength(200)]
    public string? SourceEvent { get; set; }
}
