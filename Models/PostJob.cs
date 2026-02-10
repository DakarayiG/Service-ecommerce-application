using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace phpMVC.Models
{
    public class PostJobViewModel
    {
        [Required(ErrorMessage = "Service title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        [Display(Name = "Service Title")]
        public string ServiceTitle { get; set; }

        [Required(ErrorMessage = "Service type is required")]
        [StringLength(50, ErrorMessage = "Service type cannot exceed 50 characters")]
        [Display(Name = "Service Type")]
        public string ServiceType { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Price type is required")]
        [Display(Name = "Price Type")]
        public string PriceType { get; set; } = "hourly";

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        [Display(Name = "Location")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [StringLength(50, ErrorMessage = "Duration cannot exceed 50 characters")]
        [Display(Name = "Duration")]
        public string Duration { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string JobDescription { get; set; }

        [StringLength(100, ErrorMessage = "Availability cannot exceed 100 characters")]
        [Display(Name = "Availability")]
        public string Availability { get; set; }

        [Display(Name = "Rating")]
        public decimal Rating { get; set; } = 0;

        [Display(Name = "Review Count")]
        public int ReviewCount { get; set; } = 0;

        // NEW: Service Image Upload
        [Display(Name = "Service Image")]
        [DataType(DataType.Upload)]
        public IFormFile ServiceImage { get; set; }

        // For displaying the uploaded image path
        public string ServiceImagePath { get; set; }
    }
}