using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DSharpPlus.Entities;
using NLog;
using RoleRewardBot.Objects;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.Mod;
using Torch.Mod.Messages;

namespace RoleRewardBot.Utils
{
    public sealed class PayManager
    {
        private static MainConfig Config => RoleRewardBot.Instance.Config;
        private Logger Log = LogManager.GetLogger("Rewards Bot => PayManager");
        /// <summary>
        /// Runs through all the Rewards setup and checks if any registered user qualifies to receive it.
        /// </summary>
        /// <param name="payAll">False: Only qualifying registered users will receive rewards.  True: ALL registered users will receive rewards.</param>
        public async Task Payout(int rewardID = 0, bool payAll = false)
        {
            if (!RoleRewardBot.DiscordBot.IsBotOnline())
            {
                Log.Warn("Unable to process rewards while the Discord bot is offline.");
                return;
            }
            
            List<MyPlayer> _onlineUsers = new List<MyPlayer>();
            Dictionary<ulong, MyPlayer> OnlinePlayers = new Dictionary<ulong, MyPlayer>();
            
            if (RoleRewardBot.Instance.WorldOnline)
            {
                // Grab online players to display announcements.
                if(RoleRewardBot.Instance.WorldOnline)
                    _onlineUsers = Sync.Players.GetOnlinePlayers().ToList();

                foreach (MyPlayer onlineUser in _onlineUsers)
                {
                    // This shouldn't be needed as the player list is reset on every run, but for somebody it seems to throw because of duplicate keys.
                    if (!OnlinePlayers.ContainsKey(onlineUser.Id.SteamId))
                        OnlinePlayers.Add(onlineUser.Id.SteamId, onlineUser);  
                }
            }

            // Report rewards issued!
            StringBuilder payoutReport = new StringBuilder();

            if (!payAll)
            {
                for (int userIndex = Config.RegisteredUsers.Count - 1; userIndex >= 0; userIndex--)
                {
                    RegisteredUsers registeredUser = Config.RegisteredUsers[userIndex];

                    // see if member already received scheduled payout for today
                    if (registeredUser.LastPayout.Day == DateTime.Now.Day) continue;
                    
                    DiscordUser member = await RoleRewardBot.DiscordBot.Client.GetUserAsync(Config.RegisteredUsers[userIndex].DiscordId);
                    bool registerRewarded = false; // if user received a payout, update their last reward day.

                    int rewardCounter = 0;
                    for (int rewardIndex = Config.Rewards.Count - 1; rewardIndex >= 0; rewardIndex--)
                    {
                        Reward reward = Config.Rewards[rewardIndex];
                        string[] rewardPayoutDays = reward.DaysToPay.Split(',');
                        
                        bool payhimhisdues = false;
                        foreach (string intPayDays in rewardPayoutDays)
                        {
                            if (!int.TryParse(intPayDays, out int intPayDay)) continue;
                            if (intPayDay != DateTime.Now.Day) continue;
                            payhimhisdues = true;
                            break;
                        }
                        
                        if (!payhimhisdues) continue;
                        
                        // Only pay users with the appropriate role...
                        foreach (KeyValuePair<ulong, DiscordRole> memberRole in RoleRewardBot.DiscordBot.ServerData.roles)
                        {
                            if (memberRole.Value.Name != reward.CommandRole) continue;
                            
                            // Payday!!
                            string startCommand = reward.Command.Replace("{SteamID}", registeredUser.IngameSteamId.ToString());
                            string finishCommand = startCommand.Replace("{Username}", registeredUser.IngameName);
                            Payout payTheMan = new Payout
                            {
                                ID = RoleRewardBot.DiscordBot.ID_Manager.GetNewPayoutID(),
                                DiscordId = member.Id,
                                DiscordName = member.Username,
                                IngameName = registeredUser.IngameName,
                                SteamID = registeredUser.IngameSteamId,
                                ExpiryDate = DateTime.Now + TimeSpan.FromDays(reward.ExpiresInDays),
                                RewardName = reward.Name,
                                PaymentDate = DateTime.Now,
                                Command = finishCommand
                            };

                            payoutReport.AppendLine("*   Payout Report   *");
                            payoutReport.AppendLine($"ID           -> {payTheMan.ID}");
                            payoutReport.AppendLine($"Reward Name  -> {payTheMan.RewardName}");
                            payoutReport.AppendLine($"Discord Name -> {payTheMan.DiscordName}");
                            payoutReport.AppendLine($"Discord ID   -> {payTheMan.DiscordId}");
                            payoutReport.AppendLine($"In-Game Name -> {payTheMan.IngameName}");
                            payoutReport.AppendLine($"SteamID      -> {payTheMan.SteamID}");
                            payoutReport.AppendLine($"Command      -> {payTheMan.Command}");
                            payoutReport.AppendLine($"Expires      -> [{payTheMan.DaysUntilExpired} days] {payTheMan.ExpiryDate.ToShortDateString()}");
                            payoutReport.AppendLine("--------------------------------------------------");

                            rewardCounter++;
                            Config.Payouts.Add(payTheMan);
                            registerRewarded = true;
                            continue;
                        }
                    }

                    if (!registerRewarded) continue;

                    registeredUser.LastPayout = DateTime.Now; 
                    if (OnlinePlayers.ContainsKey(registeredUser.IngameSteamId))
                    {
                        // Announce to player in game.
                        ModCommunication.SendMessageTo(new DialogMessage($"Reward Bot", null, null, $"You have {rewardCounter} new reward(s) to claim", "Understood!"), registeredUser.IngameSteamId);
                    }
                    else
                    {
                        if (!RoleRewardBot.DiscordBot.IsBotOnline()) continue;
                        // Announce to player on discord.
                        DiscordMember user = await RoleRewardBot.DiscordBot.ServerData.guild.GetMemberAsync(registeredUser.DiscordId);
                        try
                        {
                            await RoleRewardBot.DiscordBot.DMSender.SendDirectMessage( user ,$"You have {rewardCounter} new reward(s) to claim.");
                        }
                        catch (Exception e)
                        {
                            Log.Warn(e.ToString());
                        }
                    }
                }

                if (!string.IsNullOrEmpty(payoutReport.ToString()))
                {
                    Log.Info(payoutReport);
                    await RoleRewardBot.Instance.Save();
                }
                return;
            }
            
            // PAY ALL!!!
            if (rewardID == 0) // ID start at 1 on purpose, no selection sends 0 as default.
            {
                MessageBox.Show("An attempt to force-pay all members a reward has failed, invalid reward selected.","Error",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            payoutReport.AppendLine("** THIS IS A PAYALL REQUEST **");

            if (Config.RegisteredUsers.Count == 0)
            {
                MessageBox.Show("No players to receive payout.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            if (Config.Rewards.Count == 0)
            {
                MessageBox.Show("No rewards to issue.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            if (!RoleRewardBot.DiscordBot.IsBotOnline())
            {
                Log.Warn("Unable to process rewards while the Discord bot is offline.");
                return;
            }

            try
            {
                // Get Selected Reward
                Reward selectedReward = null;
                foreach (Reward reward in Config.Rewards)
                {
                    if (reward.ID != rewardID) continue;
                    selectedReward = reward;
                    break;
                } 
                
                if (selectedReward is null)
                    return;
                
                for (int userIndex = Config.RegisteredUsers.Count - 1; userIndex >= 0; userIndex--)
                {
                    RegisteredUsers registeredUser = Config.RegisteredUsers[userIndex];
                    if (registeredUser == null || registeredUser.DiscordId == 0) continue;
                    DiscordMember _user = await RoleRewardBot.DiscordBot.ServerData.guild.GetMemberAsync(registeredUser.DiscordId);
                    int rewardCounter = 0;

                    // Only pay users with the appropriate role...
                    foreach (var role in _user.Roles)
                    {
                        if (role.Name != selectedReward.CommandRole) continue;
                        // Payday!!
                        string startCommand = selectedReward.Command.Replace("{SteamID}", registeredUser.IngameSteamId.ToString());
                        string finishCommand = startCommand.Replace("{Username}", registeredUser.IngameName);
                        Payout payTheMan = new Payout
                        {
                            ID = RoleRewardBot.DiscordBot.ID_Manager.GetNewPayoutID(),
                            DiscordId = _user.Id,
                            DiscordName = _user.Username,
                            IngameName = registeredUser.IngameName,
                            SteamID = registeredUser.IngameSteamId,
                            ExpiryDate = DateTime.Now + TimeSpan.FromDays(selectedReward.ExpiresInDays),
                            RewardName = selectedReward.Name,
                            PaymentDate = DateTime.Now,
                            Command = finishCommand
                        };
                            
                        payoutReport.AppendLine($"ID           -> {payTheMan.ID}");
                        payoutReport.AppendLine($"Reward Name  -> {payTheMan.RewardName}");
                        payoutReport.AppendLine($"Discord Name -> {payTheMan.DiscordName}");
                        payoutReport.AppendLine($"Discord ID   -> {payTheMan.DiscordId}");
                        payoutReport.AppendLine($"In-Game Name -> {payTheMan.IngameName}");
                        payoutReport.AppendLine($"SteamID      -> {payTheMan.SteamID}");
                        payoutReport.AppendLine($"Command      -> {payTheMan.Command}");
                        payoutReport.AppendLine($"Expires      -> [{payTheMan.DaysUntilExpired} days] {payTheMan.ExpiryDate.ToShortDateString()}");
                        payoutReport.AppendLine($"--------------------------------------------------");

                        Config.Payouts.Add(payTheMan);
                        rewardCounter++;
                    }
                    
                    if (rewardCounter == 0) continue;
                    
                    if (OnlinePlayers.ContainsKey(registeredUser.IngameSteamId))
                    {
                        // Announce to player in game.
                        ModCommunication.SendMessageTo(new DialogMessage($"Reward Bot", null, null, $"You have {rewardCounter} new reward(s) to claim", "Understood!"), registeredUser.IngameSteamId);
                    }
                    else
                    {
                        if (!RoleRewardBot.DiscordBot.IsBotOnline()) continue;
                        // Announce to player on discord.
                        DiscordMember user = await RoleRewardBot.DiscordBot.ServerData.guild.GetMemberAsync(registeredUser.DiscordId);
                        try
                        {
                            await RoleRewardBot.DiscordBot.DMSender.SendDirectMessage(user, $"You have {rewardCounter} new reward(s) to claim.");
                        }
                        catch (Exception e)
                        {
                            Log.Warn(e.ToString());
                        }
                    }
                }
            } catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            await RoleRewardBot.Instance.Save();
            Log.Warn(payoutReport);
        }

        public async Task ManualPayout
            (
                ulong discordID,
                string discordName,
                string inGameName,
                ulong steamID,
                string command,
                int expiresInDays
            )
        {
            StringBuilder payoutReport = new StringBuilder();
            payoutReport.AppendLine("*   Manual Payout Created   *");
            
            string startCommand = command.Replace("{SteamID}", steamID.ToString());
            string finishCommand = startCommand.Replace("{Username}", inGameName);
            Payout payTheMan = new Payout
            {
                ID = RoleRewardBot.DiscordBot.ID_Manager.GetNewPayoutID(),
                DiscordId = discordID,
                DiscordName = discordName,
                IngameName = inGameName,
                SteamID = steamID,
                ExpiryDate = DateTime.Now + TimeSpan.FromDays(expiresInDays),
                RewardName = "Manual Reward",
                PaymentDate = DateTime.Now,
                Command = finishCommand
            };
            
            payoutReport.AppendLine($"ID           -> {payTheMan.ID}");
            payoutReport.AppendLine($"Reward Name  -> {payTheMan.RewardName}");
            payoutReport.AppendLine($"Discord Name -> {payTheMan.DiscordName}");
            payoutReport.AppendLine($"Discord ID   -> {payTheMan.DiscordId}");
            payoutReport.AppendLine($"In-Game Name -> {payTheMan.IngameName}");
            payoutReport.AppendLine($"SteamID      -> {payTheMan.SteamID}");
            payoutReport.AppendLine($"Command      -> {payTheMan.Command}");
            payoutReport.AppendLine($"Expires      -> [{payTheMan.DaysUntilExpired} days] {payTheMan.ExpiryDate.ToShortDateString()}");
            payoutReport.AppendLine($"--------------------------------------------------");

            Log.Warn(payoutReport);
            Config.Payouts.Add(payTheMan);
            await RoleRewardBot.Instance.Save();
        }
    }
}