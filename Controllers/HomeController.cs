using Microsoft.AspNetCore.Mvc;
using phpMVC.Models;
using System.Collections.Generic;

namespace phpMVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var popularServices = new List<Service>
            {
                new Service {
                    Id = 1,
                    Name = "Home Services",
                    Description = "Plumbing, electrical, cleaning, and home repairs",
                    ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=80",
                    Price = 50,
                    Location = "Various",
                    Duration = "Varies"
                },
                new Service {
                    Id = 2,
                    Name = "Tech Services",
                    Description = "IT support, programming, web development, tech repairs",
                    ImageUrl = "https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=80",
                    Price = 75,
                    Location = "Various",
                    Duration = "Varies"
                },
                new Service {
                    Id = 3,
                    Name = "Tutoring & Education",
                    Description = "Academic tutoring, music lessons, language teaching",
                    ImageUrl = "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=80",
                    Price = 40,
                    Location = "Various",
                    Duration = "Varies"
                }
            };

            ViewBag.PopularServices = popularServices;
            return View();
        }
    }
}