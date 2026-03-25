using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Domain.Entities
{
    public class Reclamation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Reference { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
        public string Description { get; set; }

        [Required]
        [EnumDataType(typeof(NamePriority))]
        public NamePriority Priority { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Required]
        public long ClientId { get; set; }

        [Required]
        [StringLength(100)]
        public string ClientName { get; set; }

        [Required]
        public long SAVId { get; set; }

        [Required]
        [StringLength(100)]
        public string SAVName { get; set; }

        // Constructeur vide requis par EF Core
        public Reclamation() { }

        // Constructeur paramétré
        public Reclamation(long id, string reference, string description, NamePriority priority, string status, DateTime createdAt, DateTime updatedAt, long clientId, string clientName, long sAVId, string sAVName)
        {
            Id = id;
            Reference = reference;
            Description = description;
            Priority = priority;
            Status = status;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            ClientId = clientId;
            ClientName = clientName;
            SAVId = sAVId;
            SAVName = sAVName;
        }
    }
}