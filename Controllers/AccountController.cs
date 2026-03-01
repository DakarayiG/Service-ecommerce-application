using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using phpMVC.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace phpMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AccountController(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
        {
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        { // 🔴 TEMPORARILY CLEAR ALL MODEL ERRORS
            ModelState.Clear();

            // OR completely bypass validation check


            
            //    if (ModelState.IsValid)
            //    {
            // Just proceed with registration regardless of ModelState

            var connectionString = _configuration.GetConnectionString("MySqlConnection");

                // Check if email already exists
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check existing email
                    string checkQuery = "SELECT COUNT(*) FROM h_users WHERE Email = @email";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@email", model.Email);
                        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                        if (exists > 0)
                        {
                            ModelState.AddModelError("Email", "This email is already registered.");
                            return View(model);
                        }
                    }

                    // Handle image upload if user is provider and image was uploaded
                    string providerImagePath = null;
                    if (model.UserType == "provider" && model.ProviderImage != null)
                    {
                        providerImagePath = await SaveProviderImage(model.ProviderImage);
                    }

                    // Hash password
                    string hashedPassword = HashPassword(model.Password);

                    // Insert new user
                    string insertQuery = @"
                        INSERT INTO h_users 
                        (FirstName, LastName, Email, Password, UserType, Phone, WhatsApp, Address, ProviderImage, CreatedAt, UpdatedAt, IsActive, EmailConfirmed) 
                        VALUES 
                        (@firstName, @lastName, @email, @password, @userType, @phone, @whatsapp, @address, @providerImage, NOW(), NOW(), 1, 0)";

                    using (var insertCmd = new MySqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@firstName", model.FirstName);
                        insertCmd.Parameters.AddWithValue("@lastName", model.LastName);
                        insertCmd.Parameters.AddWithValue("@email", model.Email);
                        insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                        insertCmd.Parameters.AddWithValue("@userType", model.UserType);
                        insertCmd.Parameters.AddWithValue("@phone", (object)model.Phone ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@whatsapp", (object)model.WhatsApp ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@address", (object)model.Address ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@providerImage", (object)providerImagePath ?? DBNull.Value);

                        await insertCmd.ExecuteNonQueryAsync();
                    }
        //        }

                // Registration successful - redirect to login
                TempData["RegistrationSuccess"] = "Account created successfully! Please log in.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (ModelState.IsValid)
            {
                var connectionString = _configuration.GetConnectionString("MySqlConnection");

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT Id, FirstName, LastName, Email, Password, UserType, IsActive 
                FROM h_users 
                WHERE Email = @email";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@email", model.Email);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string storedHash = reader["Password"].ToString();
                                bool isActive = Convert.ToBoolean(reader["IsActive"]);

                                if (!isActive)
                                {
                                    ModelState.AddModelError(string.Empty, "This account has been deactivated.");
                                    return View(model);
                                }

                                if (VerifyPassword(model.Password, storedHash))
                                {
                                    // ✅ OPTION 1 IMPLEMENTED HERE - Store values before closing reader
                                    var userId = reader["Id"].ToString();
                                    var userEmail = reader["Email"].ToString();
                                    var firstName = reader["FirstName"].ToString();
                                    var lastName = reader["LastName"].ToString();
                                    var userType = reader["UserType"].ToString();

                                    // ✅ Close reader after storing values
                                    reader.Close();

                                    // Update last login - using stored userId
                                    string updateQuery = "UPDATE h_users SET LastLogin = NOW() WHERE Id = @id";
                                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                                    {
                                        updateCmd.Parameters.AddWithValue("@id", userId); // ✅ Using stored variable
                                        await updateCmd.ExecuteNonQueryAsync();
                                    }

                                    // Set session/cookie - using stored variables
                                    HttpContext.Session.SetString("UserId", userId);
                                    HttpContext.Session.SetString("UserEmail", userEmail);
                                    HttpContext.Session.SetString("UserName", $"{firstName} {lastName}");
                                    HttpContext.Session.SetString("UserType", userType);

                                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                                    {
                                        return Redirect(returnUrl);
                                    }
                                    return RedirectToAction("Index", "Home");
                                }
                            }
                        }
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        // POST: /Account/Login
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        //{
        //    ViewBag.ReturnUrl = returnUrl;

        //    if (ModelState.IsValid)
        //    {
        //        var connectionString = _configuration.GetConnectionString("MySqlConnection");

        //        using (var connection = new MySqlConnection(connectionString))
        //        {
        //            await connection.OpenAsync();

        //            string query = @"
        //                SELECT Id, FirstName, LastName, Email, Password, UserType, IsActive 
        //                FROM h_users 
        //                WHERE Email = @email";

        //            using (var cmd = new MySqlCommand(query, connection))
        //            {
        //                cmd.Parameters.AddWithValue("@email", model.Email);

        //                using (var reader = await cmd.ExecuteReaderAsync())
        //                {
        //                    if (await reader.ReadAsync())
        //                    {
        //                        string storedHash = reader["Password"].ToString();
        //                        bool isActive = Convert.ToBoolean(reader["IsActive"]);

        //                        if (!isActive)
        //                        {
        //                            ModelState.AddModelError(string.Empty, "This account has been deactivated.");
        //                            return View(model);
        //                        }

        //                        if (VerifyPassword(model.Password, storedHash))
        //                        {
        //                            // Update last login
        //                            reader.Close();
        //                            string updateQuery = "UPDATE h_users SET LastLogin = NOW() WHERE Id = @id";
        //                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
        //                            {
        //                                updateCmd.Parameters.AddWithValue("@id", reader["Id"]);
        //                                await updateCmd.ExecuteNonQueryAsync();
        //                            }

        //                            // Set session/cookie (you can expand this with actual authentication)
        //                            HttpContext.Session.SetString("UserId", reader["Id"].ToString());
        //                            HttpContext.Session.SetString("UserEmail", reader["Email"].ToString());
        //                            HttpContext.Session.SetString("UserName", $"{reader["FirstName"]} {reader["LastName"]}");
        //                            HttpContext.Session.SetString("UserType", reader["UserType"].ToString());

        //                            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        //                            {
        //                                return Redirect(returnUrl);
        //                            }
        //                            return RedirectToAction("Index", "Home");
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        //    }

        //    return View(model);
        //}

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Helper method to save provider image
        private async Task<string> SaveProviderImage(IFormFile image)
        {
            try
            {
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "UploadedImages", "Providers");

                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Return relative path for database
                return "/UploadedImages/Providers/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error saving image: {ex.Message}");
                return null;
            }
        }

        // Password hashing
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Verify password
        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            string hashedEntered = HashPassword(enteredPassword);
            return hashedEntered == storedHash;
        }
    }
}