using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities
{
    public class User
    {
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [Phone]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(250)]
        public string Address { get; set; }

        [Required]
        [EmailAddress] 
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MinLength(8)] 
        public string Password { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public UserRole Role { get; set; }

        public User() { }

        public User(long id, string firstName, string lastName, string phoneNumber, string address, string email, string password, UserRole role)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            Address = address;
            Email = email;
            Password = password;
            IsActive = true;
            Role = role;
        }
    }
}