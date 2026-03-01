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
    public class ChatController : Controller
    {
        private readonly IConfiguration _configuration;

        public ChatController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        private int CurrentUserId() => int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        private string CurrentUserType() => HttpContext.Session.GetString("UserType") ?? "";
        private bool IsAdmin() => CurrentUserType() == "admin";

        // ─── GET /Chat/Index ─────────────────────────────────────────────────────
        // Main chat inbox — shows all conversations for the logged-in user
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var conversations = GetConversations(CurrentUserId());
            return View(conversations);
        }

        // ─── GET /Chat/Open/{otherUserId} ────────────────────────────────────────
        // Open a specific conversation window
        [HttpGet]
        public IActionResult Open(int id, int? bookingId = null)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");
            int otherUserId = id;  // rename internally

            int myId = CurrentUserId();

            var otherUser = GetUserById(otherUserId);
            if (otherUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            var history = GetMessageHistory(myId, otherUserId);

            // Mark messages from otherUser as read
            MarkMessagesRead(otherUserId, myId);

            var vm = new ChatViewModel
            {
                MyUserId = myId,
                MyName = HttpContext.Session.GetString("UserName"),
                MyType = CurrentUserType(),
                OtherUserId = otherUserId,
                OtherUserName = $"{otherUser.FirstName} {otherUser.LastName}",
                OtherUserType = otherUser.UserType,
                Messages = history,
                BookingId = bookingId
            };

            return View(vm);
        }

        // ─── GET /Chat/UnreadCount ───────────────────────────────────────────────
        // AJAX endpoint — returns unread message count for header badge
        [HttpGet]
        public IActionResult UnreadCount()
        {
            if (!IsLoggedIn())
                return Json(new { count = 0 });

            int count = GetUnreadCount(CurrentUserId());
            return Json(new { count });
        }

        // ─── GET /Chat/Conversations ─────────────────────────────────────────────
        // AJAX endpoint — returns conversation list (for sidebar refresh)
        [HttpGet]
        public IActionResult Conversations()
        {
            if (!IsLoggedIn())
                return Json(new List<object>());

            var convos = GetConversations(CurrentUserId());
            return Json(convos);
        }

        // ─── GET /Chat/History/{otherUserId} ────────────────────────────────────
        // AJAX endpoint — returns message history as JSON
        [HttpGet]
        public IActionResult History(int otherUserId)
        {
            if (!IsLoggedIn())
                return Json(new List<object>());

            int myId = CurrentUserId();
            var messages = GetMessageHistory(myId, otherUserId);
            MarkMessagesRead(otherUserId, myId);
            return Json(messages);
        }

        // ─── GET /Chat/SearchProviders ───────────────────────────────────────────
        // AJAX search for providers (used in header search)
        [HttpGet]
        public IActionResult SearchProviders(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            var results = SearchUsers(q, "provider");
            return Json(results);
        }

        // ─── GET /Chat/SearchUsers ───────────────────────────────────────────────
        // General user search for starting new chats
        [HttpGet]
        public IActionResult SearchUsers(string q)
        {
            if (!IsLoggedIn() || string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            string searchType = CurrentUserType() == "customer" ? "provider" : "customer";
            var results = SearchUsers(q, searchType);
            return Json(results);
        }

        // ─── GET /Chat/AdminDashboard ────────────────────────────────────────────
        [HttpGet]
        public IActionResult AdminDashboard()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var allConvos = GetAllConversationsForAdmin();
            return View(allConvos);
        }

        // ─── GET /Chat/AdminViewChat ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult AdminViewChat(int user1Id, int user2Id)
        {
            if (!IsAdmin())
                return Json(new { error = "Unauthorized" });

            var messages = GetMessageHistory(user1Id, user2Id);
            var user1 = GetUserById(user1Id);
            var user2 = GetUserById(user2Id);

            return Json(new
            {
                messages,
                user1Name = user1 != null ? $"{user1.FirstName} {user1.LastName}" : "Unknown",
                user2Name = user2 != null ? $"{user2.FirstName} {user2.LastName}" : "Unknown"
            });
        }

        // ─── HELPERS ────────────────────────────────────────────────────────────

        private List<ConversationSummary> GetConversations(int userId)
        {
            var list = new List<ConversationSummary>();
            var cs = _configuration.GetConnectionString("MySqlConnection");

            using var conn = new MySqlConnection(cs);
            try
            {
                conn.Open();
                string query = @"
                    SELECT 
                        other_id,
                        other_name,
                        other_type,
                        last_message,
                        last_time,
                        unread_count
                    FROM (
                        SELECT 
                            CASE WHEN SenderId = @uid THEN ReceiverId ELSE SenderId END AS other_id,
                            CASE WHEN SenderId = @uid 
                                 THEN (SELECT CONCAT(FirstName,' ',LastName) FROM h_users WHERE Id = ReceiverId)
                                 ELSE (SELECT CONCAT(FirstName,' ',LastName) FROM h_users WHERE Id = SenderId)
                            END AS other_name,
                            CASE WHEN SenderId = @uid 
                                 THEN (SELECT UserType FROM h_users WHERE Id = ReceiverId)
                                 ELSE (SELECT UserType FROM h_users WHERE Id = SenderId)
                            END AS other_type,
                            Message AS last_message,
                            CreatedAt AS last_time,
                            SUM(CASE WHEN ReceiverId = @uid AND IsRead = 0 THEN 1 ELSE 0 END) 
                                OVER (PARTITION BY CASE WHEN SenderId = @uid THEN ReceiverId ELSE SenderId END) AS unread_count,
                            ROW_NUMBER() OVER (
                                PARTITION BY CASE WHEN SenderId = @uid THEN ReceiverId ELSE SenderId END
                                ORDER BY CreatedAt DESC
                            ) AS rn
                        FROM chat_messages
                        WHERE SenderId = @uid OR ReceiverId = @uid
                    ) t
                    WHERE rn = 1
                    ORDER BY last_time DESC";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ConversationSummary
                    {
                        OtherUserId = Convert.ToInt32(reader["other_id"]),
                        OtherUserName = reader["other_name"].ToString(),
                        OtherUserType = reader["other_type"].ToString(),
                        LastMessage = reader["last_message"].ToString(),
                        LastTime = Convert.ToDateTime(reader["last_time"]),
                        UnreadCount = Convert.ToInt32(reader["unread_count"])
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetConversations error: {ex.Message}");
            }
            return list;
        }

        private List<ChatMessage> GetMessageHistory(int userId1, int userId2)
        {
            var messages = new List<ChatMessage>();
            var cs = _configuration.GetConnectionString("MySqlConnection");

            using var conn = new MySqlConnection(cs);
            try
            {
                conn.Open();
                string query = @"
                    SELECT m.Id, m.SenderId, m.ReceiverId, m.SenderType, m.Message, m.IsRead, m.CreatedAt,
                           CONCAT(u.FirstName,' ',u.LastName) AS SenderName
                    FROM chat_messages m
                    INNER JOIN h_users u ON m.SenderId = u.Id
                    WHERE (m.SenderId = @u1 AND m.ReceiverId = @u2)
                       OR (m.SenderId = @u2 AND m.ReceiverId = @u1)
                    ORDER BY m.CreatedAt ASC";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@u1", userId1);
                cmd.Parameters.AddWithValue("@u2", userId2);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    messages.Add(new ChatMessage
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        SenderId = Convert.ToInt32(reader["SenderId"]),
                        ReceiverId = Convert.ToInt32(reader["ReceiverId"]),
                        SenderType = reader["SenderType"].ToString(),
                        Message = reader["Message"].ToString(),
                        IsRead = Convert.ToBoolean(reader["IsRead"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        SenderName = reader["SenderName"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMessageHistory error: {ex.Message}");
            }
            return messages;
        }

        private int GetUnreadCount(int userId)
        {
            var cs = _configuration.GetConnectionString("MySqlConnection");
            using var conn = new MySqlConnection(cs);
            try
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM chat_messages WHERE ReceiverId = @uid AND IsRead = 0";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { return 0; }
        }

        private void MarkMessagesRead(int fromUserId, int toUserId)
        {
            var cs = _configuration.GetConnectionString("MySqlConnection");
            using var conn = new MySqlConnection(cs);
            try
            {
                conn.Open();
                string query = "UPDATE chat_messages SET IsRead = 1 WHERE SenderId = @from AND ReceiverId = @to AND IsRead = 0";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@from", fromUserId);
                cmd.Parameters.AddWithValue("@to", toUserId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarkMessagesRead error: {ex.Message}");
            }
        }

        private UserBasic GetUserById(int userId)
        {
            var cs = _configuration.GetConnectionString("MySqlConnection");
            using var conn = new MySqlConnection(cs);
            try
            {
                conn.Open();
                string query = "SELECT Id, FirstName, LastName, UserType, ProviderImage FROM h_users WHERE Id = @id";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", userId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new UserBasic
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        FirstName = reader["FirstName"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        UserType = reader["UserType"].ToString(),
                        Avatar = reader["ProviderImage"]?.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserById error: {ex.Message}");
            }
            return null;
        }

        private List<UserBasic> SearchUsers(string query, string userType)
        {
            var results = new List<UserBasic>();
            var cs = _configuration.GetConnectionString("MySqlConnection");
            using var conn = new MySqlConnection(cs);
            try
            {
                conn.Open();
                string sql = @"
                    SELECT Id, FirstName, LastName, UserType, ProviderImage
                    FROM h_users
                    WHERE IsActive = 1
                      AND (@userType = '' OR UserType = @userType)
                      AND (CONCAT(FirstName,' ',LastName) LIKE @q OR Email LIKE @q)
                    LIMIT 10";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@q", "%" + query + "%");
                cmd.Parameters.AddWithValue("@userType", userType ?? "");
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new UserBasic
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        FirstName = reader["FirstName"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        UserType = reader["UserType"].ToString(),
                        Avatar = reader["ProviderImage"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SearchUsers error: {ex.Message}");
            }
            return results;
        }

        private List<AdminConversationSummary> GetAllConversationsForAdmin()
        {
            var list = new List<AdminConversationSummary>();
            var cs = _configuration.GetConnectionString("MySqlConnection");
            using var conn = new MySqlConnection(cs);
            try
            {
                conn.Open();
                string query = @"
                    SELECT 
                        LEAST(m.SenderId, m.ReceiverId) AS user1_id,
                        GREATEST(m.SenderId, m.ReceiverId) AS user2_id,
                        CONCAT(u1.FirstName,' ',u1.LastName) AS user1_name,
                        u1.UserType AS user1_type,
                        CONCAT(u2.FirstName,' ',u2.LastName) AS user2_name,
                        u2.UserType AS user2_type,
                        COUNT(*) AS message_count,
                        MAX(m.CreatedAt) AS last_activity,
                        SUM(CASE WHEN m.IsRead = 0 THEN 1 ELSE 0 END) AS unread_count
                    FROM chat_messages m
                    INNER JOIN h_users u1 ON LEAST(m.SenderId, m.ReceiverId) = u1.Id
                    INNER JOIN h_users u2 ON GREATEST(m.SenderId, m.ReceiverId) = u2.Id
                    GROUP BY user1_id, user2_id, user1_name, user1_type, user2_name, user2_type
                    ORDER BY last_activity DESC";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new AdminConversationSummary
                    {
                        User1Id = Convert.ToInt32(reader["user1_id"]),
                        User2Id = Convert.ToInt32(reader["user2_id"]),
                        User1Name = reader["user1_name"].ToString(),
                        User1Type = reader["user1_type"].ToString(),
                        User2Name = reader["user2_name"].ToString(),
                        User2Type = reader["user2_type"].ToString(),
                        MessageCount = Convert.ToInt32(reader["message_count"]),
                        LastActivity = Convert.ToDateTime(reader["last_activity"]),
                        UnreadCount = Convert.ToInt32(reader["unread_count"])
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllConversationsForAdmin error: {ex.Message}");
            }
            return list;
        }
    }
}