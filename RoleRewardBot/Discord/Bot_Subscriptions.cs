using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NLog;
using RoleRewardBot.Objects;
using static RoleRewardBot.RoleRewardBot;

namespace RoleRewardBot.Discord
{
    public sealed class Bot_Subscriptions
    {
        private Bot m_bot => DiscordBot;
        private List<DiscordGuild> m_guilds = new List<DiscordGuild>();
        private Logger Log = LogManager.GetLogger("Rewards Discord Bot => Subscriptions");
        IReadOnlyCollection<DiscordMember> m_members;

        public async Task Client_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs args)
        {
            Log.Info("Guild Download Completed");
            // Make sure we have all Guild(s) data!
            foreach (KeyValuePair<ulong,DiscordGuild> discordGuild in m_bot.Client.Guilds)
            {
                m_guilds.Add(discordGuild.Value);
            }
            
            // Get bot user
            DiscordBot.BotUser = await DiscordBot.Client.GetUserAsync(DiscordBot.Client.CurrentUser.Id, true);
            
            
            // Pointer to guild data, this is not a sharded bot!
            m_bot.ServerData.guild = m_guilds[0];
            Log.Info("Guild Data Retrieved for " + m_guilds[0].Name + " (" + m_guilds[0].Id + ")");

            // Get all members
            m_members = await m_guilds[0].GetAllMembersAsync();
            Log.Info($"{m_members.Count} members retrieved.");
            
            m_bot.ServerData.roles = m_guilds[0].Roles;
            
            DiscordBot.Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            DiscordBot.Client.GuildMemberAdded += Client_GuildMemberAdded;
            DiscordBot.Client.GuildMemberRemoved += Client_GuildMemberRemoved;
            DiscordBot.Client.GuildBanAdded += Client_GuildBanAdded;
            DiscordBot.Client.GuildRoleCreated += Client_GuildRoleCreated;
            DiscordBot.Client.GuildRoleDeleted += Client_GuildRoleDeleted;
            DiscordBot.Client.GuildRoleUpdated += Client_GuildRoleUpdated;
            DiscordBot.Client.SocketClosed += Client_SocketClosed;
            
            DiscordBot.ServerData.DiscordMembers.AddRange(m_members);
            
            // Get all roles
            List<DiscordRole> tempRoles = new List<DiscordRole>();
            foreach (var role in m_guilds[0].Roles)
            {
                tempRoles.Add(role.Value);
            }
            // Add the roles as a group from the guild data instead of individually
            // This is to prevent UI update on each individual add
            DiscordBot.ServerData.DiscordRoles.AddRange(tempRoles);
            
            // Verify correct players have the register role if enabled and role id available
            if (Instance.Config.ManageRegisteredRole )
            {
                if (ulong.TryParse(Instance.Config.RegisteredRoleId, out ulong roleId))
                {
                    try
                    {
                        DiscordRole role = m_guilds[0].GetRole(roleId);
                        
                        // Assign role to registered members that do not already have it.
                        foreach (RegisteredUsers registeredUser in Instance.Config.RegisteredUsers)
                        {
                            if (registeredUser.DiscordId == 0) continue;
                            DiscordMember member = await m_guilds[0].GetMemberAsync(registeredUser.DiscordId);
                            if (member == null) continue;
                            if (member.Roles.Contains(role)) continue;
                            await member.GrantRoleAsync(role);
                        }
                        
                        // Remove role from members that are not registered.
                        foreach (DiscordMember member in m_members)
                        {
                            if (Instance.Config.RegisteredUsers.Any(x => x.DiscordId == member.Id)) continue;
                            if (!member.Roles.Contains(role)) continue;
                            await member.RevokeRoleAsync(role);
                        }
                    } catch (Exception e)
                    {
                        Log.Error(e, "Managed Role ID provided is invalid.");
                    }
                }
            }
            
            // Set Bot Name, Status
            if (Instance.Config.DiscordBotName != DiscordBot.BotUser.Username)
            {
                await DiscordBot.Client.UpdateCurrentUserAsync(Instance.Config.DiscordBotName);
                await DiscordBot.ServerData.guild.CurrentMember.ModifyAsync(mdl => mdl.Nickname = Instance.Config.DiscordBotName);
            }
            
            if (Instance.WorldOnline)
                await DiscordBot.Client.UpdateStatusAsync(new DiscordActivity(Instance.Config.StatusMessage, ActivityType.Playing), UserStatus.Online);
            else
                await DiscordBot.Client.UpdateStatusAsync(new DiscordActivity("for the server to come online...", ActivityType.Watching), UserStatus.DoNotDisturb);
        }

        private Task Client_SocketClosed(DiscordClient sender, SocketCloseEventArgs args)
        {
            DiscordBot.IsConnected = false;
            return Task.CompletedTask;
        }

        public Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            Instance.Config.BotStatus = "Connected";
            Log.Info("Connected.");
            DiscordBot.IsConnected = true;
            
            /* more for debugging
            // Check Intents
            DiscordIntents enabledIntents = DiscordBot.Client.Intents;
            // Iterate through all possible intents
            foreach (DiscordIntents intent in Enum.GetValues(typeof(DiscordIntents)))
            {
                Log.Info(enabledIntents.HasFlag(intent)
                    ? $"Bot has enabled intent: {intent}"
                    : $"Bot does not have enabled intent: {intent}");
            }
            
            Log.Info($"Enabled Intents: {enabledIntents}");*/
            
            return Task.CompletedTask;
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