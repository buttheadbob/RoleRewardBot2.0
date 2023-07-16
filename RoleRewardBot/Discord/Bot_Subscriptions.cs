using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using static RoleRewardBot.RoleRewardBot;

namespace RoleRewardBot.Discord
{
    public sealed class Bot_Subscriptions
    {
        private Bot m_bot => DiscordBot;
        private List<DiscordGuild> m_guilds = new List<DiscordGuild>();
        
        public async Task Client_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs args)
        {
            // Make sure we have all Guild(s) data!
            foreach (KeyValuePair<ulong,DiscordGuild> discordGuild in m_bot.Client.Guilds)
            {
                m_guilds.Add(discordGuild.Value);
            }
            
            m_bot.ServerData.guild = m_guilds[0];

            DiscordBot.BotUser = await DiscordBot.Client.GetUserAsync(DiscordBot.Client.CurrentUser.Id);
            m_bot.ServerData.roles = m_guilds[0].Roles;
            
            DiscordBot.Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            DiscordBot.Client.GuildMemberAdded += Client_GuildMemberAdded;
            DiscordBot.Client.GuildMemberRemoved += Client_GuildMemberRemoved;
            DiscordBot.Client.GuildBanAdded += Client_GuildBanAdded;
            DiscordBot.Client.GuildRoleCreated += Client_GuildRoleCreated;
            DiscordBot.Client.GuildRoleDeleted += Client_GuildRoleDeleted;
            DiscordBot.Client.GuildRoleUpdated += Client_GuildRoleUpdated;
            DiscordBot.Client.Ready += Client_Ready;
            DiscordBot.Client.SocketClosed += Client_SocketClosed;
        }

        private Task Client_SocketClosed(DiscordClient sender, SocketCloseEventArgs args)
        {
            DiscordBot.botStatus = Bot.BotStatus.Offline;
            DiscordBot.IsConnected = false;
            return Task.CompletedTask;
        }

        private async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            DiscordBot.botStatus = Bot.BotStatus.Online;
            switch (Instance.WorldOnline)
            {
                case true:
                    await DiscordBot.Client.UpdateStatusAsync(new DiscordActivity(Instance.Config.StatusMessage, ActivityType.Playing), UserStatus.Online);
                    break;
                case false:
                    await DiscordBot.Client.UpdateStatusAsync(new DiscordActivity(Instance.Config.StatusMessage, ActivityType.Watching), UserStatus.Idle);
                    break;
            }
        }

        private Task Client_GuildBanAdded(DiscordClient sender, GuildBanAddEventArgs args)
        {
            if (!Instance.Config.RemoveOnBannedUser) return Task.CompletedTask;
            
            for (int index = 0; index < DiscordBot.ServerData.DiscordMembers.Count; index++)
            {
                if (DiscordBot.ServerData.DiscordMembers[index].Id != args.Member.Id) continue;
                DiscordBot.ServerData.DiscordMembers.Remove(DiscordBot.ServerData.DiscordMembers[index]);
                break;
            }
            return Task.CompletedTask;
        }

        private Task Client_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs args)
        {
            if (DiscordBot.ServerData.DiscordMembers.Contains(args.Member))
                DiscordBot.ServerData.DiscordMembers.Remove(args.Member);
            return Task.CompletedTask;
        }

        private Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            DiscordBot.ServerData.DiscordMembers.Add(args.Member);
            return Task.CompletedTask;
        }

        private Task Client_GuildRoleUpdated(DiscordClient sender, GuildRoleUpdateEventArgs args)
        {
            DiscordBot.ServerData.roles = DiscordBot.ServerData.guild.Roles;
            return Task.CompletedTask;
        }

        private Task Client_GuildRoleDeleted(DiscordClient sender, GuildRoleDeleteEventArgs args)
        {
            DiscordBot.ServerData.roles = DiscordBot.ServerData.guild.Roles;
            return Task.CompletedTask;
        }

        private Task Client_GuildRoleCreated(DiscordClient sender, GuildRoleCreateEventArgs args)
        {
            DiscordBot.ServerData.roles = args.Guild.Roles;
            return Task.CompletedTask;
        }

        private Task Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            DiscordBot.ServerData.DiscordMembers.Add(args.MemberAfter);
            if (DiscordBot.ServerData.DiscordMembers.Contains(args.MemberBefore))
                DiscordBot.ServerData.DiscordMembers.Remove(args.MemberBefore);
            return Task.CompletedTask;
        }
    }
}