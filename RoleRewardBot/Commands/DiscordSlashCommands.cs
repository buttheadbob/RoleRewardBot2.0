using System;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using RoleRewardBot.Objects;

namespace RoleRewardBot
{
    public class DiscordSlashCommands : ApplicationCommandModule
    {
        private MainConfig Config => RoleRewardBot.Instance.Config;
        
        [SlashCommand("link", "Link your Discord account to your Steam account.")]
        public async Task Link(InteractionContext ctx)
        {
            // Check if already registered
            for (int index = Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                if (Config.RegisteredUsers[index].DiscordId !=  ctx.User.Id) continue;
                await ctx.CreateResponseAsync($"You are already registered.", true);
                return;
            }
            
            bool repeatCode = false;
            bool endLoop = false;
            string code = string.Empty;
            
            while (!endLoop)
            {
                Random generator = new Random(Guid.NewGuid().GetHashCode()); // Yeah.. this amuses me too :)
                code = generator.Next(1000,9999).ToString("D6");
            
                // Doubtful but check if code already used.
                for (int index = Config.LinkRequests.Count - 1; index >= 0; index--)
                {
                    LinkRequest linkRequest = Config.LinkRequests[index];
                    if (linkRequest.Code == code)
                        repeatCode = true;
                }

                if (!repeatCode)
                    endLoop = true;
            }
            
            for (int index = Config.LinkRequests.Count - 1; index >= 0; index--)
            {
                LinkRequest request = Config.LinkRequests[index];
                if (request.DiscordId != ctx.User.Id) continue;
                await ctx.CreateResponseAsync($"You have already received a code. This is valid for up to 24 hours. Please go into the game and type in chat -> !RewardBot Link {request.Code}", ephemeral:true);
                return;
            }

            Config.LinkRequests.Add(new LinkRequest
            {
                Created = DateTime.Now,
                Code = code,
                DiscordId = ctx.User.Id,
                DiscordUsername = ctx.User.Username
            });

            await RoleRewardBot.Instance.Save();
            await ctx.CreateResponseAsync($"Your link code is {code}. Go into the game and in type the following in chat -> !RewardBot Link {code}", ephemeral:true);
        }
        
        [SlashCommand("unlink", "Unlink your Discord account from your Steam account.  You will no longer receive rewards from the bot!")]
        public async Task Unlink(InteractionContext ctx)
        {
            for (int index = Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                if (Config.RegisteredUsers[index].DiscordId != ctx.User.Id) continue;
                Config.RegisteredUsers.RemoveAt(index);
                await RoleRewardBot.Instance.Save();
                await ctx.CreateResponseAsync($"You have been unlinked.", true);
                return;
            }
            
            await ctx.CreateResponseAsync($"You are not registered.", true);
        }
        
        [SlashCommand("rewards", "View your current rewards.")]
        public async Task Rewards(InteractionContext ctx)
        {
            StringBuilder rewards = new StringBuilder();
            rewards.AppendLine("   *** AVAILABLE REWARDS ***");
            RegisteredUsers user = null;
            for (int index = Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                if (Config.RegisteredUsers[index].DiscordId != ctx.User.Id) continue;
                user = Config.RegisteredUsers[index];
                break;
            }

            if (user == null)
            {
                await ctx.CreateResponseAsync("Unable to locate you in the linked player registry.  Have you registered?", ephemeral: true);
                return;
            }

            int count = 0;
            for (int index = Config.Payouts.Count - 1; index >= 0; index--)
            {
                if (Config.Payouts[index].DiscordId != ctx.User.Id) continue;
                Payout reward = Config.Payouts[index];
                rewards.AppendLine($"**ID:** {reward.ID}    **Name:** {reward.RewardName}    *Expires in {reward.DaysUntilExpired} days*");
                count++;
            }
            
            if (count == 0)
                await ctx.CreateResponseAsync("No rewards available.", ephemeral: true);
            else
            {
                rewards.AppendLine($"{count} rewards available.");
                await ctx.CreateResponseAsync(rewards.ToString(), ephemeral: true);
            }
        }
    }
}