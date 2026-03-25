using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReclamationService.Domain.Entities
{
    public class Client
    {
        [Key]
        // This ID will come exactly from the UserCreatedEvent (AuthService), so no DatabaseGenerated!
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Client() { }

        public Client(long id, string fullName, string email, string phoneNumber)
        {
            Id = id;
            FullName = fullName;
            Email = email;
            PhoneNumber = phoneNumber;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
