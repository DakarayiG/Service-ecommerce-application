using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using phpMVC.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace phpMVC.Controllers
{
    public class BookingsController : Controller
    {
        private readonly IConfiguration _configuration;

        public BookingsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // CHECK IF USER IS LOGGED IN
        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        // CHECK IF USER IS A CUSTOMER
        private bool IsCustomer()
        {
            string userType = HttpContext.Session.GetString("UserType");
            return userType == "customer";
        }

        // CHECK IF USER IS A PROVIDER
        private bool IsProvider()
        {
            string userType = HttpContext.Session.GetString("UserType");
            return userType == "provider";
        }

        // GET: /Bookings/Book/{serviceId}
        [HttpGet]
        public IActionResult Book(int serviceId)
        {
            if (!IsUserLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to book a service.";
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Bookings/Book/{serviceId}" });
            }

            if (!IsCustomer())
            {
                TempData["ErrorMessage"] = "Only customers can book services.";
                return RedirectToAction("Index", "Services");
            }

            var model = GetServiceDetailsForBooking(serviceId);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("Index", "Services");
            }

            // Set default proposed price to the service's listed price
            model.ProposedPrice = model.ServicePrice;

            ViewBag.MinDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            return View(model);
        }

        // POST: /Bookings/Book
        // POST: /Bookings/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(CreateBookingViewModel model)
        {
            Console.WriteLine("========== BOOKING SUBMISSION ==========");
            Console.WriteLine($"ServiceId: {model.ServiceId}");
            Console.WriteLine($"BookingDate: {model.BookingDate}");
            Console.WriteLine($"ProposedPrice: {model.ProposedPrice}");
            Console.WriteLine($"CustomerNotes: {model.CustomerNotes}");
            Console.WriteLine($"ModelState.IsValid (before clear): {ModelState.IsValid}");

            if (!IsUserLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to book a service.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsCustomer())
            {
                TempData["ErrorMessage"] = "Only customers can book services.";
                return RedirectToAction("Index", "Services");
            }

            // ✅ FIX: Clear ALL model validation errors
            ModelState.Clear();

            // REPOPULATE SERVICE DETAILS
            var serviceDetails = GetServiceDetailsForBooking(model.ServiceId);
            if (serviceDetails != null)
            {
                model.ServiceName = serviceDetails.ServiceName;
                model.ServiceDescription = serviceDetails.ServiceDescription;
                model.ServicePrice = serviceDetails.ServicePrice;
                model.ServiceImage = serviceDetails.ServiceImage;
                model.ProviderName = serviceDetails.ProviderName;
                model.Location = serviceDetails.Location;
            }

            ViewBag.MinDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            // Manual validation
            if (model.BookingDate == DateTime.MinValue || model.BookingDate.Date < DateTime.Now.Date)
            {
                Console.WriteLine("❌ Booking date is in the past");
                ModelState.AddModelError("BookingDate", "Booking date cannot be in the past.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.CustomerNotes))
            {
                Console.WriteLine("❌ Customer notes are empty");
                ModelState.AddModelError("CustomerNotes", "Please describe what you need.");
                return View(model);
            }

            if (model.ProposedPrice <= 0)
            {
                Console.WriteLine("❌ Proposed price is invalid");
                ModelState.AddModelError("ProposedPrice", "Please enter a valid price.");
                return View(model);
            }

            try
            {
                string customerId = HttpContext.Session.GetString("UserId");
                Console.WriteLine($"Customer ID: {customerId}");

                await CreateBooking(model, customerId);

                Console.WriteLine("✅ Booking created successfully!");
                TempData["SuccessMessage"] = "Booking request sent successfully! The provider will review your request and proposed price.";
                return RedirectToAction("BookingSuccess", new { serviceId = model.ServiceId });
            }
            catch (MySqlException sqlEx)
            {
                Console.WriteLine($"❌ MySQL ERROR: {sqlEx.Message}");
                Console.WriteLine($"Error Code: {sqlEx.Number}");

                // Check if it's the "column not found" error
                if (sqlEx.Message.Contains("Unknown column") || sqlEx.Number == 1054)
                {
                    ModelState.AddModelError("", "Database schema error: The 'ProposedPrice' column doesn't exist in the bookings table. Please run the ALTER TABLE script.");
                }
                else
                {
                    ModelState.AddModelError("", $"Database error: {sqlEx.Message}");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GENERAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", "An error occurred while creating your booking. Please try again.");
                return View(model);
            }
        }
        public IActionResult BookingSuccess(int serviceId)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.ServiceId = serviceId;
            return View();
        }

        // GET: /Bookings/Orders (Provider view - pending orders)
        [HttpGet]
        public IActionResult Orders()
        {
            if (!IsUserLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view orders.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsProvider())
            {
                TempData["ErrorMessage"] = "Only service providers can view orders.";
                return RedirectToAction("Index", "Home");
            }

            string providerId = HttpContext.Session.GetString("UserId");
            var orders = GetProviderOrders(providerId, "pending");

            return View(orders);
        }

        // POST: /Bookings/AcceptBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptBooking(int bookingId)
        {
            if (!IsProvider())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                string providerId = HttpContext.Session.GetString("UserId");

                // NEW: When accepting, set the AgreedPrice to the ProposedPrice
                await AcceptBookingWithPrice(bookingId, providerId);

                TempData["SuccessMessage"] = "Booking accepted successfully!";
                return Json(new { success = true, message = "Booking accepted" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting booking: {ex.Message}");
                return Json(new { success = false, message = "Error accepting booking" });
            }
        }

        // POST: /Bookings/RejectBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int bookingId)
        {
            if (!IsProvider())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                string providerId = HttpContext.Session.GetString("UserId");
                await UpdateBookingStatus(bookingId, providerId, "rejected");

                TempData["SuccessMessage"] = "Booking rejected.";
                return Json(new { success = true, message = "Booking rejected" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rejecting booking: {ex.Message}");
                return Json(new { success = false, message = "Error rejecting booking" });
            }
        }

        [HttpGet]
        public IActionResult AcceptedBookings()
        {
            if (!IsUserLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view accepted bookings.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsProvider())
            {
                TempData["ErrorMessage"] = "Only service providers can view accepted bookings.";
                return RedirectToAction("Index", "Home");
            }

            string providerId = HttpContext.Session.GetString("UserId");
            var acceptedBookings = GetProviderOrders(providerId, "accepted");

            return View(acceptedBookings);
        }

        [HttpGet]
        public IActionResult MyBookings()
        {
            if (!IsUserLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view your bookings.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsCustomer())
            {
                TempData["ErrorMessage"] = "Only customers can view their bookings.";
                return RedirectToAction("Index", "Home");
            }

            string customerId = HttpContext.Session.GetString("UserId");
            var bookings = GetCustomerBookings(customerId);

            return View(bookings);
        }

        // HELPER METHODS

        private CreateBookingViewModel GetServiceDetailsForBooking(int serviceId)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                        SELECT s.Id, s.Name, s.Description, s.price, s.location, s.serviceImages,
                               u.FirstName, u.LastName
                        FROM service s
                        INNER JOIN h_users u ON s.ProviderId = u.Id
                        WHERE s.Id = @serviceId AND s.IsActive = 1";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new CreateBookingViewModel
                                {
                                    ServiceId = Convert.ToInt32(reader["Id"]),
                                    ServiceName = reader["Name"].ToString(),
                                    ServiceDescription = reader["Description"].ToString(),
                                    ServicePrice = Convert.ToDecimal(reader["price"]),
                                    Location = reader["location"].ToString(),
                                    ServiceImage = reader["serviceImages"]?.ToString() ?? "",
                                    ProviderName = $"{reader["FirstName"]} {reader["LastName"]}"
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading service details: {ex.Message}");
                }
            }

            return null;
        }

        private async Task CreateBooking(CreateBookingViewModel model, string customerId)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                Console.WriteLine("✅ Database connection opened");

                // Get provider ID from service
                string getProviderQuery = "SELECT ProviderId FROM service WHERE Id = @serviceId";
                int providerId;

                using (var cmd = new MySqlCommand(getProviderQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@serviceId", model.ServiceId);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        throw new Exception($"Service with ID {model.ServiceId} not found");
                    }

                    providerId = Convert.ToInt32(result);
                    Console.WriteLine($"Provider ID: {providerId}");
                }

                // NEW: Insert booking with ProposedPrice
                string insertQuery = @"
                    INSERT INTO bookings 
                    (ServiceId, CustomerId, ProviderId, BookingDate, CustomerNotes, ProposedPrice, Status, CreatedAt)
                    VALUES 
                    (@serviceId, @customerId, @providerId, @bookingDate, @customerNotes, @proposedPrice, 'pending', NOW())";

                using (var cmd = new MySqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@serviceId", model.ServiceId);
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.Parameters.AddWithValue("@providerId", providerId);
                    cmd.Parameters.AddWithValue("@bookingDate", model.BookingDate);
                    cmd.Parameters.AddWithValue("@customerNotes", model.CustomerNotes ?? "");
                    cmd.Parameters.AddWithValue("@proposedPrice", model.ProposedPrice);

                    Console.WriteLine($"Executing INSERT with ProposedPrice: ${model.ProposedPrice}...");
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"✅ Rows affected: {rowsAffected}");

                    if (rowsAffected == 0)
                    {
                        throw new Exception("Failed to insert booking - 0 rows affected");
                    }
                }
            }
        }

        private List<Booking> GetProviderOrders(string providerId, string status)
        {
            var bookings = new List<Booking>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // NEW: Include ProposedPrice and AgreedPrice in query
                    string query = @"
                        SELECT b.Id, b.ServiceId, b.CustomerId, b.ProviderId, b.BookingDate, 
                               b.CustomerNotes, b.ProposedPrice, b.AgreedPrice, b.Status, b.CreatedAt,
                               s.Name AS ServiceName, s.Description AS ServiceDescription, 
                               s.price AS ServicePrice, s.serviceImages AS ServiceImage,
                               c.FirstName AS CustomerFirstName, c.LastName AS CustomerLastName,
                               c.Email AS CustomerEmail, c.Phone AS CustomerPhone
                        FROM bookings b
                        INNER JOIN service s ON b.ServiceId = s.Id
                        INNER JOIN h_users c ON b.CustomerId = c.Id
                        WHERE b.ProviderId = @providerId AND b.Status = @status
                        ORDER BY b.CreatedAt DESC";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@providerId", providerId);
                        cmd.Parameters.AddWithValue("@status", status);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                bookings.Add(new Booking
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    ServiceId = Convert.ToInt32(reader["ServiceId"]),
                                    CustomerId = Convert.ToInt32(reader["CustomerId"]),
                                    ProviderId = Convert.ToInt32(reader["ProviderId"]),
                                    BookingDate = Convert.ToDateTime(reader["BookingDate"]),
                                    CustomerNotes = reader["CustomerNotes"].ToString(),
                                    ProposedPrice = Convert.ToDecimal(reader["ProposedPrice"]),
                                    AgreedPrice = reader["AgreedPrice"] != DBNull.Value ? Convert.ToDecimal(reader["AgreedPrice"]) : (decimal?)null,
                                    Status = reader["Status"].ToString(),
                                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                    ServiceName = reader["ServiceName"].ToString(),
                                    ServiceDescription = reader["ServiceDescription"].ToString(),
                                    ServicePrice = Convert.ToDecimal(reader["ServicePrice"]),
                                    ServiceImage = reader["ServiceImage"]?.ToString() ?? "",
                                    CustomerName = $"{reader["CustomerFirstName"]} {reader["CustomerLastName"]}",
                                    CustomerEmail = reader["CustomerEmail"].ToString(),
                                    CustomerPhone = reader["CustomerPhone"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading provider orders: {ex.Message}");
                }
            }

            return bookings;
        }

        private List<Booking> GetCustomerBookings(string customerId)
        {
            var bookings = new List<Booking>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // NEW: Include ProposedPrice and AgreedPrice in query
                    string query = @"
                        SELECT b.Id, b.ServiceId, b.CustomerId, b.ProviderId, b.BookingDate, 
                               b.CustomerNotes, b.ProposedPrice, b.AgreedPrice, b.Status, b.CreatedAt,
                               s.Name AS ServiceName, s.Description AS ServiceDescription, 
                               s.price AS ServicePrice, s.serviceImages AS ServiceImage,
                               p.FirstName AS ProviderFirstName, p.LastName AS ProviderLastName,
                               p.Email AS ProviderEmail, p.Phone AS ProviderPhone
                        FROM bookings b
                        INNER JOIN service s ON b.ServiceId = s.Id
                        INNER JOIN h_users p ON b.ProviderId = p.Id
                        WHERE b.CustomerId = @customerId
                        ORDER BY b.CreatedAt DESC";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@customerId", customerId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                bookings.Add(new Booking
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    ServiceId = Convert.ToInt32(reader["ServiceId"]),
                                    CustomerId = Convert.ToInt32(reader["CustomerId"]),
                                    ProviderId = Convert.ToInt32(reader["ProviderId"]),
                                    BookingDate = Convert.ToDateTime(reader["BookingDate"]),
                                    CustomerNotes = reader["CustomerNotes"].ToString(),
                                    ProposedPrice = Convert.ToDecimal(reader["ProposedPrice"]),
                                    AgreedPrice = reader["AgreedPrice"] != DBNull.Value ? Convert.ToDecimal(reader["AgreedPrice"]) : (decimal?)null,
                                    Status = reader["Status"].ToString(),
                                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                    ServiceName = reader["ServiceName"].ToString(),
                                    ServiceDescription = reader["ServiceDescription"].ToString(),
                                    ServicePrice = Convert.ToDecimal(reader["ServicePrice"]),
                                    ServiceImage = reader["ServiceImage"]?.ToString() ?? "",
                                    ProviderName = $"{reader["ProviderFirstName"]} {reader["ProviderLastName"]}",
                                    ProviderEmail = reader["ProviderEmail"].ToString(),
                                    ProviderPhone = reader["ProviderPhone"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading customer bookings: {ex.Message}");
                }
            }

            return bookings;
        }

        // NEW: Accept booking and set agreed price
        private async Task AcceptBookingWithPrice(int bookingId, string providerId)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Update status to 'accepted' and set AgreedPrice to ProposedPrice
                string query = @"
                    UPDATE bookings 
                    SET Status = 'accepted', 
                        AgreedPrice = ProposedPrice, 
                        UpdatedAt = NOW() 
                    WHERE Id = @bookingId AND ProviderId = @providerId";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@bookingId", bookingId);
                    cmd.Parameters.AddWithValue("@providerId", providerId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task UpdateBookingStatus(int bookingId, string providerId, string status)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE bookings 
                    SET Status = @status, UpdatedAt = NOW() 
                    WHERE Id = @bookingId AND ProviderId = @providerId";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@bookingId", bookingId);
                    cmd.Parameters.AddWithValue("@providerId", providerId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}//using Microsoft.AspNetCore.Mvc;
 //using Microsoft.Extensions.Configuration;
 //using MySql.Data.MySqlClient;
 //using phpMVC.Models;
 //using System;
 //using System.Collections.Generic;
 //using System.Threading.Tasks;
 //using Microsoft.AspNetCore.Http;

//namespace phpMVC.Controllers
//{
//    public class BookingsController : Controller
//    {
//        private readonly IConfiguration _configuration;

//        public BookingsController(IConfiguration configuration)
//        {
//            _configuration = configuration;
//        }

//        private bool IsUserLoggedIn() =>
//            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));

//        private bool IsCustomer() =>
//            HttpContext.Session.GetString("UserType") == "customer";

//        private bool IsProvider() =>
//            HttpContext.Session.GetString("UserType") == "provider";

//        // GET: /Bookings/Book/{serviceId}
//        [HttpGet]
//        public IActionResult Book(int serviceId)
//        {
//            if (!IsUserLoggedIn())
//            {
//                TempData["ErrorMessage"] = "You must be logged in to book a service.";
//                return RedirectToAction("Login", "Account", new { returnUrl = $"/Bookings/Book/{serviceId}" });
//            }

//            if (!IsCustomer())
//            {
//                TempData["ErrorMessage"] = "Only customers can book services.";
//                return RedirectToAction("Index", "Services");
//            }

//            var model = GetServiceDetailsForBooking(serviceId);

//            if (model == null)
//            {
//                TempData["ErrorMessage"] = "Service not found.";
//                return RedirectToAction("Index", "Services");
//            }

//            // Default booking date to tomorrow
//            model.BookingDate = DateTime.Now.AddDays(1);
//            ViewBag.MinDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

//            return View(model);
//        }

//        // POST: /Bookings/Book
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Book(CreateBookingViewModel model)
//        {
//            if (!IsUserLoggedIn())
//            {
//                TempData["ErrorMessage"] = "You must be logged in to book a service.";
//                return RedirectToAction("Login", "Account");
//            }

//            if (!IsCustomer())
//            {
//                TempData["ErrorMessage"] = "Only customers can book services.";
//                return RedirectToAction("Index", "Services");
//            }

//            // ✅ FIX 1: Skip ModelState validation entirely - it blocks the date field
//            ModelState.Clear();

//            // ✅ FIX 2: Always re-load service details from DB so the page never goes blank
//            var serviceDetails = GetServiceDetailsForBooking(model.ServiceId);
//            if (serviceDetails != null)
//            {
//                model.ServiceName = serviceDetails.ServiceName;
//                model.ServiceDescription = serviceDetails.ServiceDescription;
//                model.ServicePrice = serviceDetails.ServicePrice;
//                model.ProviderName = serviceDetails.ProviderName;
//                model.Location = serviceDetails.Location;
//                model.ServiceImage = serviceDetails.ServiceImage;
//            }

//            ViewBag.MinDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

//            // Manual validation only for what we actually need
//            if (model.BookingDate == DateTime.MinValue || model.BookingDate.Date < DateTime.Now.Date)
//            {
//                TempData["ErrorMessage"] = "Please select a valid booking date (must be today or later).";
//                return View(model);
//            }

//            if (string.IsNullOrWhiteSpace(model.CustomerNotes))
//            {
//                TempData["ErrorMessage"] = "Please describe what you need done.";
//                return View(model);
//            }

//            try
//            {
//                string customerId = HttpContext.Session.GetString("UserId");

//                Console.WriteLine("========== BOOKING SUBMISSION ==========");
//                Console.WriteLine($"ServiceId:   {model.ServiceId}");
//                Console.WriteLine($"CustomerId:  {customerId}");
//                Console.WriteLine($"BookingDate: {model.BookingDate}");
//                Console.WriteLine($"Notes:       {model.CustomerNotes}");
//                Console.WriteLine("========================================");

//                await CreateBooking(model, customerId);

//                TempData["SuccessMessage"] = "Booking request sent! The provider will review your request.";
//                return RedirectToAction("BookingSuccess", new { serviceId = model.ServiceId });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"❌ Error creating booking: {ex.Message}");
//                Console.WriteLine($"   Stack: {ex.StackTrace}");
//                TempData["ErrorMessage"] = $"Error submitting booking: {ex.Message}";
//                return View(model);
//            }
//        }

//        // GET: /Bookings/BookingSuccess
//        public IActionResult BookingSuccess(int serviceId)
//        {
//            if (!IsUserLoggedIn())
//                return RedirectToAction("Login", "Account");

//            ViewBag.ServiceId = serviceId;
//            return View();
//        }

//        // GET: /Bookings/Orders
//        [HttpGet]
//        public IActionResult Orders()
//        {
//            if (!IsUserLoggedIn())
//            {
//                TempData["ErrorMessage"] = "You must be logged in to view orders.";
//                return RedirectToAction("Login", "Account");
//            }

//            if (!IsProvider())
//            {
//                TempData["ErrorMessage"] = "Only service providers can view orders.";
//                return RedirectToAction("Index", "Home");
//            }

//            string providerId = HttpContext.Session.GetString("UserId");
//            var orders = GetProviderOrders(providerId, "pending");
//            return View(orders);
//        }

//        // POST: /Bookings/AcceptBooking
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> AcceptBooking(int bookingId)
//        {
//            if (!IsProvider())
//                return Json(new { success = false, message = "Unauthorized" });

//            try
//            {
//                string providerId = HttpContext.Session.GetString("UserId");
//                await UpdateBookingStatus(bookingId, providerId, "accepted");
//                return Json(new { success = true, message = "Booking accepted" });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error accepting booking: {ex.Message}");
//                return Json(new { success = false, message = "Error accepting booking" });
//            }
//        }

//        // POST: /Bookings/RejectBooking
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> RejectBooking(int bookingId)
//        {
//            if (!IsProvider())
//                return Json(new { success = false, message = "Unauthorized" });

//            try
//            {
//                string providerId = HttpContext.Session.GetString("UserId");
//                await UpdateBookingStatus(bookingId, providerId, "rejected");
//                return Json(new { success = true, message = "Booking rejected" });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error rejecting booking: {ex.Message}");
//                return Json(new { success = false, message = "Error rejecting booking" });
//            }
//        }

//        // GET: /Bookings/AcceptedBookings
//        [HttpGet]
//        public IActionResult AcceptedBookings()
//        {
//            if (!IsUserLoggedIn())
//            {
//                TempData["ErrorMessage"] = "You must be logged in.";
//                return RedirectToAction("Login", "Account");
//            }

//            if (!IsProvider())
//            {
//                TempData["ErrorMessage"] = "Only service providers can view accepted bookings.";
//                return RedirectToAction("Index", "Home");
//            }

//            string providerId = HttpContext.Session.GetString("UserId");
//            var acceptedBookings = GetProviderOrders(providerId, "accepted");
//            return View(acceptedBookings);
//        }

//        // GET: /Bookings/MyBookings
//        [HttpGet]
//        public IActionResult MyBookings()
//        {
//            if (!IsUserLoggedIn())
//            {
//                TempData["ErrorMessage"] = "You must be logged in.";
//                return RedirectToAction("Login", "Account");
//            }

//            if (!IsCustomer())
//            {
//                TempData["ErrorMessage"] = "Only customers can view their bookings.";
//                return RedirectToAction("Index", "Home");
//            }

//            string customerId = HttpContext.Session.GetString("UserId");
//            var bookings = GetCustomerBookings(customerId);
//            return View(bookings);
//        }

//        // ─── HELPERS ────────────────────────────────────────────────────────────

//        private CreateBookingViewModel GetServiceDetailsForBooking(int serviceId)
//        {
//            var connectionString = _configuration.GetConnectionString("MySqlConnection");

//            using (var connection = new MySqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();

//                    string query = @"
//                        SELECT s.Id, s.Name, s.Description, s.price, s.location, s.serviceImages,
//                               u.FirstName, u.LastName
//                        FROM service s
//                        INNER JOIN h_users u ON s.ProviderId = u.Id
//                        WHERE s.Id = @serviceId AND s.IsActive = 1";

//                    using (var cmd = new MySqlCommand(query, connection))
//                    {
//                        cmd.Parameters.AddWithValue("@serviceId", serviceId);

//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            if (reader.Read())
//                            {
//                                return new CreateBookingViewModel
//                                {
//                                    ServiceId = Convert.ToInt32(reader["Id"]),
//                                    ServiceName = reader["Name"]?.ToString() ?? "",
//                                    ServiceDescription = reader["Description"]?.ToString() ?? "",
//                                    ServicePrice = Convert.ToDecimal(reader["price"]),
//                                    Location = reader["location"]?.ToString() ?? "",
//                                    ServiceImage = reader["serviceImages"]?.ToString() ?? "",
//                                    ProviderName = $"{reader["FirstName"]} {reader["LastName"]}"
//                                };
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"❌ Error loading service details: {ex.Message}");
//                }
//            }

//            return null;
//        }

//        private async Task CreateBooking(CreateBookingViewModel model, string customerId)
//        {
//            var connectionString = _configuration.GetConnectionString("MySqlConnection");

//            using (var connection = new MySqlConnection(connectionString))
//            {
//                await connection.OpenAsync();

//                // Get ProviderId from the service table
//                int providerId = 0;
//                string getProviderQuery = "SELECT ProviderId FROM service WHERE Id = @serviceId";

//                using (var cmd = new MySqlCommand(getProviderQuery, connection))
//                {
//                    cmd.Parameters.AddWithValue("@serviceId", model.ServiceId);
//                    var result = await cmd.ExecuteScalarAsync();
//                    providerId = result != null ? Convert.ToInt32(result) : 0;
//                }

//                Console.WriteLine($"   ProviderId from DB: {providerId}");

//                if (providerId == 0)
//                    throw new Exception("Could not find provider for this service.");

//                string insertQuery = @"
//                    INSERT INTO bookings 
//                        (ServiceId, CustomerId, ProviderId, BookingDate, CustomerNotes, Status, CreatedAt)
//                    VALUES 
//                        (@serviceId, @customerId, @providerId, @bookingDate, @customerNotes, 'pending', NOW())";

//                using (var cmd = new MySqlCommand(insertQuery, connection))
//                {
//                    cmd.Parameters.AddWithValue("@serviceId", model.ServiceId);
//                    cmd.Parameters.AddWithValue("@customerId", customerId);
//                    cmd.Parameters.AddWithValue("@providerId", providerId);
//                    // ✅ FIX 3: Send date as string to avoid any culture/format issues
//                    cmd.Parameters.AddWithValue("@bookingDate", model.BookingDate.ToString("yyyy-MM-dd"));
//                    cmd.Parameters.AddWithValue("@customerNotes", model.CustomerNotes ?? "");

//                    int rows = await cmd.ExecuteNonQueryAsync();
//                    Console.WriteLine($"✅ Booking inserted. Rows affected: {rows}");

//                    if (rows == 0)
//                        throw new Exception("Insert ran but 0 rows were affected.");
//                }
//            }
//        }

//        private List<Booking> GetProviderOrders(string providerId, string status)
//        {
//            var bookings = new List<Booking>();
//            var connectionString = _configuration.GetConnectionString("MySqlConnection");

//            using (var connection = new MySqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();

//                    string query = @"
//                        SELECT b.Id, b.ServiceId, b.CustomerId, b.ProviderId, b.BookingDate,
//                               b.CustomerNotes, b.Status, b.CreatedAt,
//                               s.Name AS ServiceName, s.Description AS ServiceDescription,
//                               s.price AS ServicePrice, s.serviceImages AS ServiceImage,
//                               c.FirstName AS CustomerFirstName, c.LastName AS CustomerLastName,
//                               c.Email AS CustomerEmail, c.Phone AS CustomerPhone
//                        FROM bookings b
//                        INNER JOIN service s ON b.ServiceId = s.Id
//                        INNER JOIN h_users c ON b.CustomerId = c.Id
//                        WHERE b.ProviderId = @providerId AND b.Status = @status
//                        ORDER BY b.CreatedAt DESC";

//                    using (var cmd = new MySqlCommand(query, connection))
//                    {
//                        cmd.Parameters.AddWithValue("@providerId", providerId);
//                        cmd.Parameters.AddWithValue("@status", status);

//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                bookings.Add(new Booking
//                                {
//                                    Id = Convert.ToInt32(reader["Id"]),
//                                    ServiceId = Convert.ToInt32(reader["ServiceId"]),
//                                    CustomerId = Convert.ToInt32(reader["CustomerId"]),
//                                    ProviderId = Convert.ToInt32(reader["ProviderId"]),
//                                    BookingDate = Convert.ToDateTime(reader["BookingDate"]),
//                                    CustomerNotes = reader["CustomerNotes"]?.ToString() ?? "",
//                                    Status = reader["Status"]?.ToString() ?? "",
//                                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
//                                    ServiceName = reader["ServiceName"]?.ToString() ?? "",
//                                    ServiceDescription = reader["ServiceDescription"]?.ToString() ?? "",
//                                    ServicePrice = Convert.ToDecimal(reader["ServicePrice"]),
//                                    ServiceImage = reader["ServiceImage"]?.ToString() ?? "",
//                                    CustomerName = $"{reader["CustomerFirstName"]} {reader["CustomerLastName"]}",
//                                    CustomerEmail = reader["CustomerEmail"]?.ToString() ?? "",
//                                    CustomerPhone = reader["CustomerPhone"]?.ToString() ?? ""
//                                });
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"❌ Error loading provider orders: {ex.Message}");
//                }
//            }

//            return bookings;
//        }

//        private List<Booking> GetCustomerBookings(string customerId)
//        {
//            var bookings = new List<Booking>();
//            var connectionString = _configuration.GetConnectionString("MySqlConnection");

//            using (var connection = new MySqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();

//                    string query = @"
//                        SELECT b.Id, b.ServiceId, b.CustomerId, b.ProviderId, b.BookingDate,
//                               b.CustomerNotes, b.Status, b.CreatedAt,
//                               s.Name AS ServiceName, s.Description AS ServiceDescription,
//                               s.price AS ServicePrice, s.serviceImages AS ServiceImage,
//                               p.FirstName AS ProviderFirstName, p.LastName AS ProviderLastName,
//                               p.Email AS ProviderEmail, p.Phone AS ProviderPhone
//                        FROM bookings b
//                        INNER JOIN service s ON b.ServiceId = s.Id
//                        INNER JOIN h_users p ON b.ProviderId = p.Id
//                        WHERE b.CustomerId = @customerId
//                        ORDER BY b.CreatedAt DESC";

//                    using (var cmd = new MySqlCommand(query, connection))
//                    {
//                        cmd.Parameters.AddWithValue("@customerId", customerId);

//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                bookings.Add(new Booking
//                                {
//                                    Id = Convert.ToInt32(reader["Id"]),
//                                    ServiceId = Convert.ToInt32(reader["ServiceId"]),
//                                    CustomerId = Convert.ToInt32(reader["CustomerId"]),
//                                    ProviderId = Convert.ToInt32(reader["ProviderId"]),
//                                    BookingDate = Convert.ToDateTime(reader["BookingDate"]),
//                                    CustomerNotes = reader["CustomerNotes"]?.ToString() ?? "",
//                                    Status = reader["Status"]?.ToString() ?? "",
//                                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
//                                    ServiceName = reader["ServiceName"]?.ToString() ?? "",
//                                    ServiceDescription = reader["ServiceDescription"]?.ToString() ?? "",
//                                    ServicePrice = Convert.ToDecimal(reader["ServicePrice"]),
//                                    ServiceImage = reader["ServiceImage"]?.ToString() ?? "",
//                                    ProviderName = $"{reader["ProviderFirstName"]} {reader["ProviderLastName"]}",
//                                    ProviderEmail = reader["ProviderEmail"]?.ToString() ?? "",
//                                    ProviderPhone = reader["ProviderPhone"]?.ToString() ?? ""
//                                });
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"❌ Error loading customer bookings: {ex.Message}");
//                }
//            }

//            return bookings;
//        }

//        private async Task UpdateBookingStatus(int bookingId, string providerId, string status)
//        {
//            var connectionString = _configuration.GetConnectionString("MySqlConnection");

//            using (var connection = new MySqlConnection(connectionString))
//            {
//                await connection.OpenAsync();

//                string query = @"
//                    UPDATE bookings 
//                    SET Status = @status, UpdatedAt = NOW() 
//                    WHERE Id = @bookingId AND ProviderId = @providerId";

//                using (var cmd = new MySqlCommand(query, connection))
//                {
//                    cmd.Parameters.AddWithValue("@status", status);
//                    cmd.Parameters.AddWithValue("@bookingId", bookingId);
//                    cmd.Parameters.AddWithValue("@providerId", providerId);

//                    await cmd.ExecuteNonQueryAsync();
//                }
//            }
//        }
//    }
//}