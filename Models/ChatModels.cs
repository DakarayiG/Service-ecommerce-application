using System;
using System.Collections.Generic;

namespace phpMVC.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string SenderType { get; set; }
        public string SenderName { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BookingId { get; set; }
    }

    public class ConversationSummary
    {
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserType { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastTime { get; set; }
        public int UnreadCount { get; set; }
        public string Avatar { get; set; }
    }

    public class ChatViewModel
    {
        public int MyUserId { get; set; }
        public string MyName { get; set; }
        public string MyType { get; set; }
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserType { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
        public int? BookingId { get; set; }

    }

    public class AdminConversationSummary
    {
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public string User1Name { get; set; }
        public string User1Type { get; set; }
        public string User2Name { get; set; }
        public string User2Type { get; set; }
        public int MessageCount { get; set; }
        public DateTime LastActivity { get; set; }
        public int UnreadCount { get; set; }
    }

    public class UserBasic
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserType { get; set; }
        public string Avatar { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}