using System.Collections.Generic;
using DSharpPlus.Entities;
using RoleRewardBot.Objects;

namespace RoleRewardBot.Discord
{
    public sealed class ServerData
    {
        public IReadOnlyDictionary<ulong, DiscordRole> roles {get; set; }
        public DiscordGuild guild { get; set; }
        public MyList<DiscordMember> DiscordMembers { get; set; } = new MyList<DiscordMember>();
        public MyList<DiscordRole> DiscordRoles { get; set; } = new MyList<DiscordRole>();
    }
}