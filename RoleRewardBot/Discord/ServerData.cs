using System.Collections.Generic;
using DSharpPlus.Entities;

namespace RoleRewardBot.Discord
{
    public sealed class ServerData
    {
        public IReadOnlyDictionary<ulong, DiscordRole> roles {get; set; }
        public DiscordGuild guild { get; set; }
    }
}