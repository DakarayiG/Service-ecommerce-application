// Controllers/PostJobsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using phpMVC.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace phpMVC.Controllers
{
    public class PostJobsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public PostJobsController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        // GET: /PostJobs/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new PostJobViewModel());
        }

        // POST: /PostJobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Handle image upload
                string imagePath = await HandleImageUpload(model.ServiceImage);

                // Save to database
                SaveServiceToDatabase(model, imagePath);

                TempData["SuccessMessage"] = "Service posted successfully!";
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error posting service: {ex.Message}");
                return View(model);
            }
        }

        // GET: /PostJobs/Success
        public IActionResult Success()
        {
            if (TempData["SuccessMessage"] == null)
            {
                return RedirectToAction("Create");
            }
            return View();
        }

        private async Task<string> HandleImageUpload(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return null;
            }

            // Validate file size (5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                throw new Exception("File size exceeds 5MB limit.");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("Invalid file type. Allowed: JPG, PNG, GIF.");
            }

            // Create uploads directory
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "UploadedImages", "services");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"UploadedImages/services/{uniqueFileName}";
        }

        private void SaveServiceToDatabase(PostJobViewModel model, string imagePath)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                INSERT INTO service (
                    Name, Description, IsActive, 
                    location, duration, availability,
                    rating, reviewcount, price, serviceImages
                ) VALUES (
                    @Name, @Description, @IsActive,
                    @Location, @Duration, @Availability,
                    @Rating, @ReviewCount, @Price, @ServiceImages
                )";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    // Truncate if needed (matching your database schema)
                    string serviceTitle = model.ServiceTitle ?? "";
                    if (serviceTitle.Length > 5) serviceTitle = serviceTitle.Substring(0, 5);

                    string description = model.JobDescription ?? "";
                    if (description.Length > 2) description = description.Substring(0, 2);

                    string location = model.Location ?? "";
                    if (location.Length > 1) location = location.Substring(0, 1);

                    string duration = model.Duration ?? "";
                    if (duration.Length > 1) duration = duration.Substring(0, 1);

                    string availability = model.Availability ?? "";
                    if (availability.Length > 1) availability = availability.Substring(0, 1);

                    cmd.Parameters.AddWithValue("@Name", serviceTitle);
                    cmd.Parameters.AddWithValue("@Description", description);
                    cmd.Parameters.AddWithValue("@IsActive", 1);
                    cmd.Parameters.AddWithValue("@Location", location);
                    cmd.Parameters.AddWithValue("@Duration", duration);
                    cmd.Parameters.AddWithValue("@Availability", availability);
                    cmd.Parameters.AddWithValue("@Rating", model.Rating);
                    cmd.Parameters.AddWithValue("@ReviewCount", model.ReviewCount);
                    cmd.Parameters.AddWithValue("@Price", model.Price);
                    cmd.Parameters.AddWithValue("@ServiceImages", imagePath ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}