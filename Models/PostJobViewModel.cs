using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace phpMVC.Models
{
    public class PostJobViewModel
    {
        [Display(Name = "Service Title")]
        public string ServiceTitle { get; set; }

        [Display(Name = "Service Type")]
        public string ServiceType { get; set; }

        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Price Type")]
        public string PriceType { get; set; } = "hourly";

        [Display(Name = "Location")]
        public string Location { get; set; }

        [Display(Name = "Duration")]
        public string Duration { get; set; }

        [Display(Name = "Description")]
        public string JobDescription { get; set; }

        [Display(Name = "Availability")]
        public string Availability { get; set; }

        [Display(Name = "Rating")]
        public decimal Rating { get; set; } = 0;

        [Display(Name = "Review Count")]
        public int ReviewCount { get; set; } = 0;

        [Display(Name = "Service Image")]
        [DataType(DataType.Upload)]
        public IFormFile ServiceImage { get; set; }

        // For displaying the uploaded image path
        public string ServiceImagePath { get; set; }
    }
}