using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace phpMVC.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IConfiguration _configuration;

        public ChatHub(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Called when a user opens a conversation.
        /// They join a group named "chat-{lowerUserId}-{higherUserId}" so both
        /// sides share the same group, plus an admin group for admin monitoring.
        /// </summary>
        public async Task JoinConversation(int otherUserId)
        {
            var httpContext = Context.GetHttpContext();
            string userIdStr = httpContext?.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr)) return;

            int myId = int.Parse(userIdStr);
            string groupName = GetGroupName(myId, otherUserId);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Admins also join an admin-monitor group
            string userType = httpContext?.Session.GetString("UserType");
            if (userType == "admin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin-monitor");
            }
        }

        /// <summary>
        /// Send a message to another user. Saves to DB and broadcasts to the group.
        /// </summary>
        public async Task SendMessage(int receiverId, string message, int? bookingId = null)
        {
            var httpContext = Context.GetHttpContext();
            string userIdStr = httpContext?.Session.GetString("UserId");
            string senderName = httpContext?.Session.GetString("UserName");
            string senderType = httpContext?.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrWhiteSpace(message))
                return;

            int senderId = int.Parse(userIdStr);
            string groupName = GetGroupName(senderId, receiverId);
            string timestamp = DateTime.Now.ToString("hh:mm tt");

            // Save to database
            int messageId = await SaveMessage(senderId, receiverId, senderType, message, bookingId);

            // Broadcast to the conversation group
            await Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                id = messageId,
                senderId,
                senderName,
                senderType,
                message,
                timestamp,
                isOwn = false   // client JS will flip this for the sender
            });

            // Also notify admin-monitor group (for admin dashboard)
            await Clients.Group("admin-monitor").SendAsync("AdminReceiveMessage", new
            {
                id = messageId,
                senderId,
                receiverId,
                senderName,
                senderType,
                message,
                timestamp,
                groupName
            });
        }

        /// <summary>
        /// Mark all messages from a specific sender as read.
        /// </summary>
        public async Task MarkAsRead(int fromUserId)
        {
            var httpContext = Context.GetHttpContext();
            string userIdStr = httpContext?.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return;

            int myId = int.Parse(userIdStr);
            await MarkMessagesRead(fromUserId, myId);

            // Notify the sender their messages were read
            string groupName = GetGroupName(myId, fromUserId);
            await Clients.Group(groupName).SendAsync("MessagesRead", myId);
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private static string GetGroupName(int userId1, int userId2)
        {
            // Always lower id first so both sides get the same group name
            int lo = Math.Min(userId1, userId2);
            int hi = Math.Max(userId1, userId2);
            return $"chat-{lo}-{hi}";
        }

        private async Task<int> SaveMessage(int senderId, int receiverId, string senderType, string message, int? bookingId)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO chat_messages (BookingId, SenderId, ReceiverId, SenderType, Message, IsRead, CreatedAt)
                VALUES (@bookingId, @senderId, @receiverId, @senderType, @message, 0, NOW());
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@bookingId", (object)bookingId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@senderId", senderId);
            cmd.Parameters.AddWithValue("@receiverId", receiverId);
            cmd.Parameters.AddWithValue("@senderType", senderType ?? "customer");
            cmd.Parameters.AddWithValue("@message", message);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task MarkMessagesRead(int fromUserId, int toUserId)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE chat_messages 
                SET IsRead = 1 
                WHERE SenderId = @fromUserId AND ReceiverId = @toUserId AND IsRead = 0";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@fromUserId", fromUserId);
            cmd.Parameters.AddWithValue("@toUserId", toUserId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}