using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace phpMVC.Models
{
    public class RegisterViewModel
    {
        //[Required(ErrorMessage = "First name is required")]
        //[Display(Name = "First Name")]
        //[StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; }

        //[Required(ErrorMessage = "Last name is required")]
        //[Display(Name = "Last Name")]
        //[StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; }

        //[Required(ErrorMessage = "Email address is required")]
        //[EmailAddress(ErrorMessage = "Invalid email address")]
        //[Display(Name = "Email")]
        //[StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }

        //[Required(ErrorMessage = "User type is required")]
        //[Display(Name = "I am a")]
        public string UserType { get; set; } // "customer" or "provider"

        //[Display(Name = "Phone Number")]
        //[Phone(ErrorMessage = "Invalid phone number")]
        //[StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; }

        //[Display(Name = "WhatsApp Number")]
        //[Phone(ErrorMessage = "Invalid WhatsApp number")]
        //[StringLength(20, ErrorMessage = "WhatsApp number cannot exceed 20 characters")]
        public string WhatsApp { get; set; }

        //[Display(Name = "Address")]
        //[StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        //[Display(Name = "Profile Picture")]
        public IFormFile ProviderImage { get; set; }

        //[Required(ErrorMessage = "Password is required")]
        //[DataType(DataType.Password)]
        //[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        //[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        //    ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; }

        //[Required(ErrorMessage = "Please confirm your password")]
        //[DataType(DataType.Password)]
        //[Display(Name = "Confirm Password")]
        //[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}