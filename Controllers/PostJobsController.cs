using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using phpMVC.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

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

        // CHECK IF USER IS LOGGED IN AND IS A PROVIDER
        private bool IsProviderLoggedIn()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return false;
            }

            string userType = HttpContext.Session.GetString("UserType");
            return userType == "provider";
        }

        // GET: /PostJobs/Create
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsProviderLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in as a service provider to post a service.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/PostJobs/Create" });
            }

            return View(new PostJobViewModel());
        }

        // POST: /PostJobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostJobViewModel model)
        {
            // CHECK AGAIN ON POST (security)
            if (!IsProviderLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in as a service provider to post a service.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/PostJobs/Create" });
            }

            // 🔴 CLEAR ALL VALIDATION - SKIP ModelState.IsValid CHECK
            ModelState.Clear();

            try
            {
                // Get the current provider's ID from session
                string providerId = HttpContext.Session.GetString("UserId");

                Console.WriteLine("========== POST JOB SUBMISSION ==========");
                Console.WriteLine($"Provider ID: {providerId}");
                Console.WriteLine($"Service Title: {model.ServiceTitle ?? "NULL"}");
                Console.WriteLine($"Service Type: {model.ServiceType ?? "NULL"}");
                Console.WriteLine($"Price: {model.Price}");
                Console.WriteLine($"Location: {model.Location ?? "NULL"}");
                Console.WriteLine($"Duration: {model.Duration ?? "NULL"}");
                Console.WriteLine($"Description: {model.JobDescription ?? "NULL"}");
                Console.WriteLine($"Has Image: {model.ServiceImage != null}");
                Console.WriteLine("========================================");

                // Handle image upload
                string imagePath = await HandleImageUpload(model.ServiceImage);
                Console.WriteLine($"Image Path: {imagePath ?? "NULL"}");

                // Save to database with provider ID
                SaveServiceToDatabase(model, imagePath, providerId);

                Console.WriteLine("✅ Service posted successfully!");

                // SET SUCCESS MESSAGE BUT STAY ON SAME PAGE
                TempData["ShowSuccessMessage"] = true;
                TempData["SuccessMessage"] = "Service posted successfully!";

                // Return the same view with a fresh model
                return View(new PostJobViewModel());
            }
            catch (MySqlException sqlEx)
            {
                Console.WriteLine($"❌ MySQL ERROR: {sqlEx.Message}");
                Console.WriteLine($"Error Code: {sqlEx.Number}");
                Console.WriteLine($"SQL State: {sqlEx.SqlState}");
                Console.WriteLine($"Stack Trace: {sqlEx.StackTrace}");

                TempData["ErrorMessage"] = $"Database error: {sqlEx.Message}";
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GENERAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"Error posting service: {ex.Message}";
                return View(model);
            }
        }

        // GET: /PostJobs/Success
        public IActionResult Success()
        {
            if (!IsProviderLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

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
                Console.WriteLine("No image uploaded");
                return null;
            }

            try
            {
                // Validate file size (5MB)
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    throw new Exception("File size exceeds 5MB limit.");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(imageFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    throw new Exception("Invalid file type. Allowed: JPG, PNG, GIF, WEBP.");
                }

                // Create uploads directory
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "UploadedImages", "services");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                    Console.WriteLine($"Created directory: {uploadsFolder}");
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                Console.WriteLine($"✅ Image saved: {uniqueFileName}");
                return $"/UploadedImages/services/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving image: {ex.Message}");
                throw;
            }
        }

        private void SaveServiceToDatabase(PostJobViewModel model, string imagePath, string providerId)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("✅ Database connection opened");

                // Get the provider's image from h_users table
                string providerImage = null;
                string getProviderImageQuery = "SELECT ProviderImage FROM h_users WHERE Id = @providerId";

                using (var getImgCmd = new MySqlCommand(getProviderImageQuery, connection))
                {
                    getImgCmd.Parameters.AddWithValue("@providerId", providerId);
                    providerImage = getImgCmd.ExecuteScalar()?.ToString();
                    Console.WriteLine($"Provider Image from DB: {providerImage ?? "NULL"}");
                }

                // Insert the service
                string query = @"
                    INSERT INTO service (
                        Name, Description, location, duration, availability,
                        rating, reviewcount, price, serviceImages, ProviderImages,
                        ProviderId, IsActive, created_at, updated_at
                    ) VALUES (
                        @Name, @Description, @location, @duration, @availability,
                        @rating, @reviewcount, @price, @serviceImages, @ProviderImages,
                        @ProviderId, @IsActive, NOW(), NOW()
                    )";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Name", model.ServiceTitle ?? "");
                    cmd.Parameters.AddWithValue("@Description", model.JobDescription ?? "");
                    cmd.Parameters.AddWithValue("@location", model.Location ?? "");
                    cmd.Parameters.AddWithValue("@duration", model.Duration ?? "");
                    cmd.Parameters.AddWithValue("@availability", model.Availability ?? "");
                    cmd.Parameters.AddWithValue("@rating", model.Rating);
                    cmd.Parameters.AddWithValue("@reviewcount", model.ReviewCount);
                    cmd.Parameters.AddWithValue("@price", model.Price);
                    cmd.Parameters.AddWithValue("@serviceImages", imagePath ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProviderImages", providerImage ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProviderId", providerId);
                    cmd.Parameters.AddWithValue("@IsActive", 1);

                    Console.WriteLine("Executing SQL INSERT...");
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        throw new Exception("Failed to insert service into database - 0 rows affected");
                    }

                    Console.WriteLine($"✅ Service inserted successfully. Rows affected: {rowsAffected}");
                }
            }
        }
    }
}