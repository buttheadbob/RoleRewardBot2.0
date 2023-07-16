using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace RoleRewardBot.Discord
{
    public sealed class Bot_Subscriptions
    {
        private Bot m_bot => RoleRewardBot.DiscordBot;
        private List<DiscordGuild> m_guilds = new List<DiscordGuild>();
        
        public async Task Client_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs args)
        {
            // Make sure we have all Guild(s) data!
            foreach (KeyValuePair<ulong,DiscordGuild> discordGuild in m_bot.Client.Guilds)
            {
                m_guilds.Add(discordGuild.Value);
            }
            
            m_bot.ServerData.guild = m_guilds[0];

            RoleRewardBot.DiscordBot.BotUser = await RoleRewardBot.DiscordBot.Client.GetUserAsync(RoleRewardBot.DiscordBot.Client.CurrentUser.Id);
            m_bot.ServerData.roles = m_guilds[0].Roles;
        }
    }
}