using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using phpMVC.Models;
using System;
using System.Collections.Generic;
using System.Security.Policy;

namespace phpMVC.Controllers
{
    public class ServicesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly int _pageSize = 9;

        public ServicesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index(ServiceSearchViewModel model)
        {
            if (model.Page < 1) model.Page = 1;

            var services = LoadServices(model);
            model.Services = services;
            model.TotalPages = (int)Math.Ceiling((double)model.TotalServices / _pageSize);

            // Store search criteria in TempData for pagination
            TempData["SearchServiceType"] = model.ServiceType;
            TempData["SearchLocation"] = model.Location;
            TempData["SearchDuration"] = model.Duration;
            TempData["SearchPriceRange"] = model.PriceRange;
            TempData["SortBy"] = model.SortBy;

            return View(model);
        }

        [HttpPost]
        public IActionResult Search(ServiceSearchViewModel model)
        {
            return RedirectToAction("Index", model);
        }

        public IActionResult ApplyCategoryFilter(string category)
        {
            var model = new ServiceSearchViewModel();

            switch (category)
            {
                case "home":
                    model.ServiceType = "Plumbing, Electrical, Cleaning";
                    break;
                case "tech":
                    model.ServiceType = "Web Development, IT Support";
                    break;
                case "education":
                    model.ServiceType = "Tutoring, Teaching";
                    break;
                case "beauty":
                    model.ServiceType = "Hair Styling, Beauty Services";
                    break;
                case "delivery":
                    model.ServiceType = "Delivery Services";
                    break;
                case "automotive":
                    model.ServiceType = "Car Repair, Automotive";
                    break;
            }

            return RedirectToAction("Index", model);
        }

        private List<Service> LoadServices(ServiceSearchViewModel model)
        {
            var services = new List<Service>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Build dynamic WHERE clause
                    var whereClauses = new List<string>();
                    var parameters = new List<MySqlParameter>();

                    whereClauses.Add("s.IsActive = 1");

                    if (!string.IsNullOrEmpty(model.ServiceType))
                    {
                        whereClauses.Add(@"
                    (LOWER(s.Name) LIKE @serviceType 
                     OR LOWER(s.Description) LIKE @serviceType
                     OR LOWER(s.Name) LIKE @serviceTypeStart
                     OR LOWER(s.Description) LIKE @serviceTypeStart)");

                        string searchTermLower = model.ServiceType.ToLower();
                        parameters.Add(new MySqlParameter("@serviceType", "%" + searchTermLower + "%"));
                        parameters.Add(new MySqlParameter("@serviceTypeStart", searchTermLower + "%"));
                    }

                    if (!string.IsNullOrEmpty(model.Location))
                    {
                        whereClauses.Add("LOWER(s.location) LIKE @location");
                        parameters.Add(new MySqlParameter("@location", "%" + model.Location.ToLower() + "%"));
                    }

                    if (!string.IsNullOrEmpty(model.Duration))
                    {
                        whereClauses.Add("LOWER(s.duration) LIKE @duration");
                        parameters.Add(new MySqlParameter("@duration", "%" + model.Duration.ToLower() + "%"));
                    }

                    if (!string.IsNullOrEmpty(model.PriceRange))
                    {
                        (decimal minPrice, decimal maxPrice) = GetPriceRange(model.PriceRange);
                        whereClauses.Add("s.price >= @minPrice AND s.price <= @maxPrice");
                        parameters.Add(new MySqlParameter("@minPrice", minPrice));
                        parameters.Add(new MySqlParameter("@maxPrice", maxPrice));
                    }

                    // Get sort order
                    string sortOrder = GetSortOrder(model.SortBy);

                    // Build WHERE clause
                    string whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                    // Get total count
                    string countQuery = $"SELECT COUNT(*) FROM service s {whereClause}";
                    using (var countCmd = new MySqlCommand(countQuery, connection))
                    {
                        foreach (var param in parameters)
                        {
                            countCmd.Parameters.Add(param);
                        }
                        model.TotalServices = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    // Get paginated data - JOIN with h_users to get provider names
                    int offset = (model.Page - 1) * _pageSize;
                    string dataQuery = $@"
                SELECT s.Id, s.Name, s.Description, s.location, s.duration, s.availability, 
                       s.rating, s.reviewcount, s.price, s.serviceImages, s.providerImages,
                       s.ProviderId, u.FirstName, u.LastName
                FROM service s
                INNER JOIN h_users u ON s.ProviderId = u.Id
                {whereClause} 
                ORDER BY {sortOrder}
                LIMIT {_pageSize} OFFSET {offset}";

                    using (var dataCmd = new MySqlCommand(dataQuery, connection))
                    {
                        foreach (var param in parameters)
                        {
                            dataCmd.Parameters.Add(param);
                        }

                        using (var reader = dataCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var service = new Service
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Location = reader["location"].ToString(),
                                    Duration = reader["duration"].ToString(),
                                    Availability = reader["availability"]?.ToString() ?? string.Empty,
                                    Rating = Convert.ToDouble(reader["rating"]),
                                    ReviewCount = Convert.ToInt32(reader["reviewcount"]),
                                    Price = Convert.ToDecimal(reader["price"]),
                                    ProviderId = Convert.ToInt32(reader["ProviderId"]) // Get provider ID
                                };

                                // Get provider name from h_users table
                                string firstName = reader["FirstName"]?.ToString() ?? "";
                                string lastName = reader["LastName"]?.ToString() ?? "";
                                service.ProviderName = $"{firstName} {lastName}".Trim();

                                // Handle service image
                                string serviceImageStr = reader["serviceImages"]?.ToString();
                                if (!string.IsNullOrEmpty(serviceImageStr))
                                {
                                    string[] images = serviceImageStr.Split(',');
                                    service.ImageUrl = NormalizeImageUrl(images[0]);
                                }
                                else
                                {
                                    service.ImageUrl = GetDefaultServiceImage();
                                }

                                // Handle provider image
                                string providerImage = reader["providerImages"]?.ToString();
                                if (!string.IsNullOrEmpty(providerImage))
                                {
                                    service.ProviderImage = NormalizeImageUrl(providerImage);
                                }
                                else
                                {
                                    service.ProviderImage = GetDefaultProviderImage();
                                }

                                // Set badge
                                if (service.Rating >= 4.5)
                                {
                                    service.BadgeText = "Top Rated";
                                    service.BadgeClass = "verified";
                                }
                                else if (service.ReviewCount > 100)
                                {
                                    service.BadgeText = "Popular";
                                    service.BadgeClass = "new";
                                }
                                else
                                {
                                    service.BadgeText = "";
                                    service.BadgeClass = "";
                                }

                                services.Add(service);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading services: {ex.Message}");
                    services = GetHardcodedServices();
                    model.TotalServices = services.Count;
                }
            }

            return services;
        }
        private (decimal minPrice, decimal maxPrice) GetPriceRange(string priceRange)
        {
            return priceRange switch
            {
                "1" => (0, 50),
                "2" => (50, 100),
                "3" => (100, 200),
                "4" => (200, 500),
                "5" => (500, decimal.MaxValue),
                _ => (0, decimal.MaxValue)
            };
        }

        private string GetSortOrder(string sortBy)
        {
            return sortBy switch
            {
                "price_low" => "price ASC",
                "price_high" => "price DESC",
                "newest" => "Id DESC",
                _ => "rating DESC, reviewcount DESC"
            };
        }

        private string GetDefaultServiceImage()
        {
            return "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=80";
        }

        private string GetDefaultProviderImage()
        {
            return "https://randomuser.me/api/portraits/men/32.jpg";
        }

        private List<Service> GetHardcodedServices()
        {
            return new List<Service>
            {
                new Service {
                    Id = 1,
                    Name = "Emergency Plumbing Services",
                    Description = "24/7 emergency plumbing, pipe repairs, drain cleaning",
                    ImageUrl = "https://images.unsplash.com/photo-1607472586893-edb57bdc0e39?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=80",
                    ProviderName = "John Plumbing Co.",
                    ProviderImage = "https://randomuser.me/api/portraits/men/32.jpg",
                    Location = "Cape Town, South Africa",
                    Duration = "2-4 hours",
                    Availability = "Available Now",
                    Rating = 4.8,
                    ReviewCount = 128,
                    Price = 85,
                    BadgeText = "Verified",
                    BadgeClass = "verified"
                }
            };
        }

        public List<string> GetServiceImages(int serviceId)
        {
            var images = new List<string>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT serviceImages FROM service WHERE Id = @id";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", serviceId);
                        var result = cmd.ExecuteScalar();

                        if (result != null && !string.IsNullOrEmpty(result.ToString()))
                        {
                            string imagePath = result.ToString().Trim();
                            if (!string.IsNullOrEmpty(imagePath))
                            {
                                images.Add(NormalizeImageUrl(imagePath));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting service images: {ex.Message}");
                }
            }

            // If no image in database, return default
            if (images.Count == 0)
            {
                images.Add(GetDefaultServiceImage());
            }

            return images;
        }

        //// New method to get multiple images for a service
        //public List<string> GetServiceImages(int serviceId)
        //{
        //    var images = new List<string>();
        //    var connectionString = _configuration.GetConnectionString("MySqlConnection");

        //    using (var connection = new MySqlConnection(connectionString))
        //    {
        //        try
        //        {
        //            connection.Open();
        //            string query = "SELECT serviceImages FROM service WHERE Id = @id";

        //            using (var cmd = new MySqlCommand(query, connection))
        //            {
        //                cmd.Parameters.AddWithValue("@id", serviceId);

        //                var result = cmd.ExecuteScalar();
        //                if (result != null && !string.IsNullOrEmpty(result.ToString()))
        //                {
        //                    string[] imageArray = result.ToString().Split(',');
        //                    foreach (var img in imageArray)
        //                    {
        //                        if (!string.IsNullOrEmpty(img.Trim()))
        //                        {
        //                            images.Add(img.Trim());
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error getting service images: {ex.Message}");
        //        }
        //    }

        //    // If no images in database, return at least one default
        //    if (images.Count == 0)
        //    {
        //        images.Add(GetDefaultServiceImage());
        //    }

        //    return images;
        //}

        private string NormalizeImageUrl(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return GetDefaultServiceImage();

            imagePath = imagePath.Trim();

            // Absolute URL (external)
            if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return imagePath;
            }

            // Remove leading ~ or / if present
            imagePath = imagePath.TrimStart('~', '/');

            // If DB already contains UploadedImages/
            if (imagePath.StartsWith("UploadedImages/", StringComparison.OrdinalIgnoreCase))
            {
                return "~/" + imagePath; // Return relative path without Url.Content
            }

            // Otherwise assume it's a filename only
            return "~/UploadedImages/" + imagePath;
        }
        //private string NormalizeImageUrl(string imagePath)
        //{
        //    if (string.IsNullOrWhiteSpace(imagePath))
        //        return GetDefaultServiceImage();

        //    imagePath = imagePath.Trim();

        //    // Absolute URL (external)
        //    if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        //        imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return imagePath;
        //    }

        //    // If it's already a full path with UploadedImages/, use it as is
        //    if (imagePath.StartsWith("UploadedImages/", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return Url.Content("~/" + imagePath);
        //    }

        //    // For filenames only, add the UploadedImages/ prefix
        //    return Url.Content("~/UploadedImages/" + imagePath);
        //}

        //private string NormalizeImageUrl(string imagePath)
        //{
        //    if (string.IsNullOrWhiteSpace(imagePath))
        //        return GetDefaultServiceImage();

        //    imagePath = imagePath.Trim();

        //    // Absolute URL (external)
        //    if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        //        imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return imagePath;
        //    }

        //    // Remove leading ~ or /
        //    imagePath = imagePath.TrimStart('~', '/');

        //    // IF DB already contains UploadedImages/
        //    if (imagePath.StartsWith("UploadedImages/", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return Url.Content("~/" + imagePath);
        //    }

        //    // Otherwise assume it's a filename only
        //    return Url.Content("~/UploadedImages/" + imagePath);
        //}


        //private string NormalizeImageUrl(string imagePath)
        //{
        //    if (string.IsNullOrWhiteSpace(imagePath))
        //        return GetDefaultServiceImage();

        //    imagePath = imagePath.Trim();

        //    // Already absolute URL
        //    if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        //        imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return imagePath;
        //    }

        //    // Ensure no leading slash duplication
        //    imagePath = imagePath.TrimStart('/');

        //    // Convert relative path to absolute web path
        //    return Url.Content("~/UploadedImages/" + imagePath);
        //}
    }
}