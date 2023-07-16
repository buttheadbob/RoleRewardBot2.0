using System;

namespace RoleRewardBot.Objects
{
    public sealed class RegisteredUsers
    {
        public string IngameName { get; set; }
        public ulong IngameSteamId { get; set; }
        public string DiscordUsername { get; set; }
        public ulong DiscordId { get; set; }
        public DateTime Registered { get; set; }
        public DateTime LastPayout { get; set; }
    }
}