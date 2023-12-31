﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using DSharpPlus.Entities;
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
        private static CustomLogger.LogManager Log => RoleRewardBot.Log;
        private readonly Timer _payoutTimer = new Timer(60000); // Checks for bot and server status every minute to issue payout, or dispose timer if payout already issued for the day.

        public PayManager()
        {
            _payoutTimer.Elapsed += PayoutTimer_Elapsed;
            _payoutTimer.Start();
        }

        private async void PayoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Double check payouts
            if (Config.lastScheduledPayoutProcessed.Day == DateTime.Now.Day)
            {
                _payoutTimer.Stop();
                _payoutTimer.Dispose();
            }

            // Bot must be online for up to date list of users and their roles!
            if (!RoleRewardBot.DiscordBot.IsConnected) return;
            
            // Server must be online so players who get notified have a world they can join to claim!!
            if (!RoleRewardBot.Instance.WorldOnline) return;
            
            await Payout();
            await RemoveExpiredPayouts();
            
            Config.lastScheduledPayoutProcessed = DateTime.Now;
            await RoleRewardBot.Instance.Save();
            
            _payoutTimer.Stop();
            _payoutTimer.Dispose();
        }

        /// <summary>
        /// Runs through all the Rewards setup and checks if any registered user qualifies to receive it.
        /// </summary>
        /// <param name="rewardId"></param>
        /// <param name="payAll">False: Only qualifying registered users will receive rewards.  True: ALL registered users will receive rewards.</param>
        /// <param name="payUnpaid">For use when user wants to rerun the payout for new members.</param>
        public async Task Payout(int rewardId = 0, bool payAll = false, bool payUnpaid = false)
        {
            if (!RoleRewardBot.DiscordBot.IsConnected)
            {
                await Log.Warn("Unable to process rewards while the Discord bot is offline.");
                return;
            }
            
            List<MyPlayer> onlineUsers = new List<MyPlayer>();
            Dictionary<ulong, MyPlayer> onlinePlayers = new Dictionary<ulong, MyPlayer>();
            
            if (RoleRewardBot.Instance.WorldOnline)
            {
                // Grab online players to display announcements.
                if(RoleRewardBot.Instance.WorldOnline)
                    onlineUsers = Sync.Players.GetOnlinePlayers().ToList();

                foreach (MyPlayer onlineUser in onlineUsers)
                {
                    // This shouldn't be needed as the player list is reset on every run, but for somebody it seems to throw because of duplicate keys.
                    if (!onlinePlayers.ContainsKey(onlineUser.Id.SteamId))
                        onlinePlayers.Add(onlineUser.Id.SteamId, onlineUser);  
                }
            }

            // Report rewards issued!
            StringBuilder payoutReport = new StringBuilder();

            if (!payAll)
            {
                if (Config.lastScheduledPayoutProcessed.Day == DateTime.Now.Day && !payUnpaid)
                {
                    await Log.Info("Scheduled payout already processed today.");
                    return;
                }
                
                for (int userIndex = Config.RegisteredUsers.Count - 1; userIndex >= 0; userIndex--)
                {
                    RegisteredUsers registeredUser = Config.RegisteredUsers[userIndex];
                    
                    DiscordUser member = await RoleRewardBot.DiscordBot.Client.GetUserAsync(Config.RegisteredUsers[userIndex].DiscordId);
                    
                    for (int rewardIndex = Config.Rewards.Count - 1; rewardIndex >= 0; rewardIndex--)
                    {
                        Reward reward = Config.Rewards[rewardIndex];
                        string[] rewardPayoutDays = reward.DaysToPay.Split(',');
                        
                        bool payhimhisdues = false;
                        foreach (string intPayDays in rewardPayoutDays)
                        {
                            if (!int.TryParse(intPayDays, out int intPayDay))
                            {
                                await Log.Warn($"Unable to get integer value pay date for reward {reward.Name} [{intPayDays}]");
                                continue;
                            }
                            if (intPayDay != DateTime.Now.Day) continue;
                            
                            if (registeredUser.LastPayouts.TryGetValue(reward.ID, out DateTime lastPayout))
                                if (lastPayout.Day == DateTime.Now.Day) continue;
                            
                            payhimhisdues = true;
                            break;
                        }
                        
                        if (!payhimhisdues) continue;
                        
                        // Only pay users with the appropriate role...
                        foreach (KeyValuePair<ulong, DiscordRole> memberRole in RoleRewardBot.DiscordBot.ServerData.roles)
                        {
                            if (memberRole.Value.Name != reward.RewardedRole) continue;
                            
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

                            payoutReport.AppendLine();
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

                            Config.Payouts.Add(payTheMan);
                            registeredUser.LastPayouts.TryGetValue(reward.ID, out DateTime lastPayout);
                            registeredUser.LastPayouts[reward.ID] = DateTime.Now;

                            if (onlinePlayers.ContainsKey(registeredUser.IngameSteamId))
                            {
                                // Announce to player in game.
                                ModCommunication.SendMessageTo(new DialogMessage($"Reward Bot", null, null, $"You have a new reward to claim", "Understood!"), registeredUser.IngameSteamId);
                            }
                            else
                            {
                                if (!RoleRewardBot.DiscordBot.IsConnected) continue;
                                // Announce to player on discord.
                                DiscordMember user = await RoleRewardBot.DiscordBot.ServerData.guild.GetMemberAsync(registeredUser.DiscordId);
                                try
                                {
                                    await RoleRewardBot.DiscordBot.DMSender.SendDirectMessage( user ,$"You have a new reward to claim.");
                                }
                                catch (Exception e)
                                {
                                    await Log.Warn(e.ToString());
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(payoutReport.ToString()))
                {
                    if (payUnpaid)
                    {
                        await Log.Info("PayUnpaid was requested but no registered users who are eligible were found to pay.");
                    }
                    return;
                }
                await Log.Info(payoutReport.ToString());
                await RoleRewardBot.Instance.Save();
                return;
            }
            
            // PAY ALL!!!
            if (rewardId < 1) // ID start at 1 on purpose, no selection sends 0 as default.
            {
                MessageBox.Show("An attempt to force-pay all members a reward has failed, invalid reward selected.","Error",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            payoutReport.AppendLine("** THIS IS A PAY-ALL REQUEST **");

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
            
            if (!RoleRewardBot.DiscordBot.IsConnected)
            {
                await Log.Warn("Unable to process rewards while the Discord bot is offline.");
                return;
            }

            try
            {
                // Get Selected Reward
                Reward selectedReward = null;
                for (int index = Config.Rewards.Count - 1; index >= 0; index--)
                {
                    if (Config.Rewards[index].ID != rewardId) continue;
                    selectedReward = Config.Rewards[index];
                    break;
                }

                if (selectedReward is null)
                    return;
                
                for (int userIndex = Config.RegisteredUsers.Count - 1; userIndex >= 0; userIndex--)
                {
                    RegisteredUsers registeredUser = Config.RegisteredUsers[userIndex];
                    if (registeredUser == null || registeredUser.DiscordId == 0) continue;
                    DiscordMember _user = await RoleRewardBot.DiscordBot.ServerData.guild.GetMemberAsync(registeredUser.DiscordId);
                    if (_user is null) continue;

                    // Only pay users with the appropriate role...
                    foreach (DiscordRole role in _user.Roles)
                    {
                        if (role.Name != selectedReward.RewardedRole) continue;
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
                            
                        payoutReport.AppendLine();
                        payoutReport.AppendLine("*   Payout Report   *");
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
                        
                        if (onlinePlayers.ContainsKey(registeredUser.IngameSteamId))
                        {
                            // Announce to player in game.
                            ModCommunication.SendMessageTo(new DialogMessage($"Reward Bot", null, null, $"You have a new reward to claim", "Understood!"), registeredUser.IngameSteamId);
                        }
                        else
                        {
                            if (!RoleRewardBot.DiscordBot.IsConnected) continue;
                            // Announce to player on discord.
                            DiscordMember user = await RoleRewardBot.DiscordBot.ServerData.guild.GetMemberAsync(registeredUser.DiscordId);
                            try
                            {
                                await RoleRewardBot.DiscordBot.DMSender.SendDirectMessage(user, $"You have a new reward(s) to claim.");
                            }
                            catch (Exception e)
                            {
                                await Log.Warn(e.ToString());
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                await Log.Error(e.ToString());
            }

            await RoleRewardBot.Instance.Save();
            await Log.Warn(payoutReport.ToString());
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

            await Log.Warn(payoutReport.ToString());
            Config.Payouts.Add(payTheMan);
            await RoleRewardBot.Instance.Save();
        }

        public async Task RemoveExpiredPayouts()
        {
            for (int index = Config.Payouts.Count - 1; index >= 0; index--)
            {
                if (Config.Payouts[index].ExpiryDate.Date > DateTime.Now.Date) continue;
                await Log.Info($"Removed expired payout for {Config.Payouts[index].IngameName} [{Config.Payouts[index].RewardName}]");
                Config.Payouts.Remove(Config.Payouts[index]);
            }

            await RoleRewardBot.Instance.Save();
        }
    }
}