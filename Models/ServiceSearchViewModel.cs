using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace phpMVC.Models
{
    public class ServiceSearchViewModel
    {
        public string ServiceType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string PriceRange { get; set; } = string.Empty;
        public string SortBy { get; set; } = "rating";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalServices { get; set; }
        public int TotalPages { get; set; }
        public List<Service> Services { get; set; } = new List<Service>();
    }
}