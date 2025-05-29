using System;
using System.Collections.Generic;

namespace SimpleFeedReader.ViewModels
{
    public class ChatViewModel
    {
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}