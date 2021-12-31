namespace FlarpBot.Bot.Modules.MinecraftModule.Models
{
    internal class MinecraftServerStatus
    {
        public bool ServerUp { get; set; }
        public string Version { get; set; }
        public string Host { get; set; }
        public ushort? Port { get; set; }
        public int? CurrentPlayers { get; set; }
        public int? MaximumPlayers { get; set; }
        public string Motd { get; set; }
    }
}
