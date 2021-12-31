using System;
using System.ComponentModel.DataAnnotations;

namespace FlarpBot.Bot.Models
{
    public class SayInChannelRequest
    {
        [Required]
        public string RequestId { get; set; }
        [Required]
        public string GuildId { get; set; }
        [Required]
        public string ChannelId { get; set; }
        [Required]
        public string Message { get; set; }
    }
}
