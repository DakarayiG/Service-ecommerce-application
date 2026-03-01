using System;
using System.ComponentModel.DataAnnotations;

namespace phpMVC.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int ServiceId { get; set; }

        public int CustomerId { get; set; }

        public int ProviderId { get; set; }

        public DateTime BookingDate { get; set; }

        public string CustomerNotes { get; set; }

        // NEW: Customer's proposed price
        public decimal ProposedPrice { get; set; }

        // NEW: Final agreed price (set when provider accepts)
        public decimal? AgreedPrice { get; set; }

        public string Status { get; set; } // "pending", "accepted", "rejected", "completed"

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public decimal ServicePrice { get; set; }
        public string ServiceImage { get; set; }

        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }

        public string ProviderName { get; set; }
        public string ProviderEmail { get; set; }
        public string ProviderPhone { get; set; }
    }

    public class CreateBookingViewModel
    {
        public int ServiceId { get; set; }

        //[Required(ErrorMessage = "Please select a booking date")]
        //[Display(Name = "Booking Date")]
        //[DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        //[Required(ErrorMessage = "Please describe what you need")]
        //[Display(Name = "Description of Work Needed")]
        //[StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string CustomerNotes { get; set; }

        //// NEW: Customer's proposed price
        //[Required(ErrorMessage = "Please enter your proposed price")]
        //[Display(Name = "Your Proposed Price")]
        //[Range(0.01, 100000, ErrorMessage = "Price must be between $0.01 and $100,000")]
        public decimal ProposedPrice { get; set; }

        // Service details for display
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public decimal ServicePrice { get; set; }
        public string ServiceImage { get; set; }
        public string ProviderName { get; set; }
        public string Location { get; set; }
    }
}
//using System;
 //using System.ComponentModel.DataAnnotations;

//namespace phpMVC.Models
//{
//    public class Booking
//    {
//        public int Id { get; set; }

//        public int ServiceId { get; set; }

//        public int CustomerId { get; set; }

//        public int ProviderId { get; set; }

//        public DateTime BookingDate { get; set; }

//        public string CustomerNotes { get; set; }

//        public string Status { get; set; } // "pending", "accepted", "rejected", "completed"

//        public DateTime CreatedAt { get; set; }

//        public DateTime? UpdatedAt { get; set; }

//        // Navigation properties
//        public string ServiceName { get; set; }
//        public string ServiceDescription { get; set; }
//        public decimal ServicePrice { get; set; }
//        public string ServiceImage { get; set; }

//        public string CustomerName { get; set; }
//        public string CustomerEmail { get; set; }
//        public string CustomerPhone { get; set; }

//        public string ProviderName { get; set; }
//        public string ProviderEmail { get; set; }
//        public string ProviderPhone { get; set; }
//    }

//    public class CreateBookingViewModel
//    {
//        public int ServiceId { get; set; }

//        [Required(ErrorMessage = "Please select a booking date")]
//        [Display(Name = "Booking Date")]
//        [DataType(DataType.Date)]
//        public DateTime BookingDate { get; set; }

//        [Required(ErrorMessage = "Please describe what you need")]
//        [Display(Name = "Description of Work Needed")]
//        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
//        public string CustomerNotes { get; set; }

//        // Service details for display
//        public string ServiceName { get; set; }
//        public string ServiceDescription { get; set; }
//        public decimal ServicePrice { get; set; }
//        public string ServiceImage { get; set; }
//        public string ProviderName { get; set; }
//        public string Location { get; set; }
//    }
//}