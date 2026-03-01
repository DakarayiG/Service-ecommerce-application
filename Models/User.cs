using System;

namespace phpMVC.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string UserType { get; set; } // "customer" or "provider"
        public string Phone { get; set; }
        public string WhatsApp { get; set; }
        public string Address { get; set; }
        public string ProviderImage { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        // Full name property
        public string FullName => $"{FirstName} {LastName}";
    }
}