using System.ComponentModel.DataAnnotations;

namespace phpMVC.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Service title is required")]
        [StringLength(100, ErrorMessage = "Service title cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty; // Initialize with default

        [Required(ErrorMessage = "Service type is required")]
        [StringLength(50, ErrorMessage = "Service type cannot exceed 50 characters")]
        public string ServiceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Please enter a valid price")]
        public decimal Price { get; set; }

        public string PriceType { get; set; } = "hourly"; // Default value

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Duration is required")]
        [StringLength(50, ErrorMessage = "Duration cannot exceed 50 characters")]
        public string Duration { get; set; } = string.Empty;

        public string? Availability { get; set; } // Nullable

        public double Rating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // For display only (not in DB)
        public string? ImageUrl { get; set; }
        public string ProviderName { get; set; } = "Service Provider";
        public string? ProviderImage { get; set; }
        public int ProviderId { get; set; }
        public string? BadgeText { get; set; }
        public string? BadgeClass { get; set; }
    }
}