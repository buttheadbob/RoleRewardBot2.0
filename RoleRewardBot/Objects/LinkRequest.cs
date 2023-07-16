using System;

namespace RoleRewardBot.Objects
{
    public sealed class LinkRequest
    {
        public string Code { get; set; }
        public ulong DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public DateTime Created { get; set; }
    }
}