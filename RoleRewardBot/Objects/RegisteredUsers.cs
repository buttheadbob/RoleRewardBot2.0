using System;
using RoleRewardBot.Utils;

namespace RoleRewardBot.Objects
{
    public sealed class RegisteredUsers
    {
        public string IngameName { get; set; }
        public ulong IngameSteamId { get; set; }
        public string DiscordUsername { get; set; }
        public ulong DiscordId { get; set; }
        public DateTime Registered { get; set; }
        public SerializableDictionary<int, DateTime> LastPayouts { get; set; } = new SerializableDictionary<int, DateTime>();
    }
}