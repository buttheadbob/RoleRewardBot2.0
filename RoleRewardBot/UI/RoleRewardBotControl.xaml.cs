using System;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using DSharpPlus.Entities;
using RoleRewardBot.Objects;
using RoleRewardBot.UI;
using static RoleRewardBot.RoleRewardBot;

namespace RoleRewardBot
{
    public partial class RoleRewardBotControl : UserControl
    {
        private MainConfig Config => Instance.Config;
        
        public MyList<DiscordMember> FilteredDiscordMembers = new MyList<DiscordMember>();
        private static object FilteredDiscordMembers_LOCK = new object();
        
        public MyList<RegisteredUsers> FilteredRegisteredUsers = new MyList<RegisteredUsers>();
        private static object FilteredRegisteredUsers_LOCK = new object();
        
        private int lastSelectedItem = -1; // Used to store last selected payout for the delete payout button.

        private RoleRewardBot Plugin { get; }

        private RoleRewardBotControl()
        {
            InitializeComponent();
            Instance.MainDispatcher = FilteredCount.Dispatcher;
            DataContext = Config;
            FilteredRegisteredUsers.AddRange(Config.RegisteredUsers);
            RegisteredMembersGrid.ItemsSource = FilteredRegisteredUsers;
            DiscordMembersGrid.DataContext = this;
            RegisteredMembersGrid.DataContext = this;
            FilteredCount.DataContext = this;
            StatusLabel.DataContext = Config;
            RewardCommandsList.ItemsSource = Config.Rewards;
            tbRoleComboBox.DataContext = DiscordBot.ServerData;
            tbRoleComboBox.ItemsSource = DiscordBot.ServerData.DiscordRoles;
            ForceSelectedPayoutToAll.DataContext = Config;
            ForceSelectedPayoutToAll.ItemsSource = Config.Rewards;
            DiscordBot.ServerData.DiscordMembers.CollectionChanged += DiscordMembersOnCollectionChanged;
            Config.RegisteredUsers.CollectionChanged += RegisteredUsersOnCollectionChanged;
            RegisteredUsersOnCollectionChanged(null, null);
        } 
        
        private async void ForceBotOnline_OnClick(object sender, RoutedEventArgs e)
        {
            if (DiscordBot.IsConnected)
            {
                Log.Warn("Unable to connect to Discord. Bot is already online.");
                return;
            }
            
            Instance.Config.BotStatus = "Connecting...";
            await DiscordBot.ConnectAsync();
            
            
        }

        private async void ForceBotOffline_OnClick(object sender, RoutedEventArgs e)
        {
            if (!DiscordBot.IsConnected) return;
            Instance.Config.BotStatus = "Disconnecting...";
            await DiscordBot.Client.DisconnectAsync();
            Instance.Config.BotStatus = "Offline";
            DiscordBot.IsConnected = false;
        }

        private void RegisteredUsersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FilteredRegisteredUsers.Clear();
            FilteredRegisteredUsers.AddRange(Config.RegisteredUsers);
            FilteredRegisteredCount.Dispatcher.Invoke(() => { FilteredRegisteredCount.Text = FilteredRegisteredUsers.Count + " / " + Config.RegisteredUsers.Count; }); 
        }

        private void DiscordMembersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DiscordMembersGrid.ItemsSource = null;
            FilteredDiscordMembers.Clear();
            FilteredDiscordMembers.AddRange(DiscordBot.ServerData.DiscordMembers);
            FilteredCount.Dispatcher.InvokeAsync(() =>
            {
                FilteredCount.Text = $"Showing: {FilteredDiscordMembers.Count} of {DiscordBot.ServerData.DiscordMembers.Count}";
            });
            DiscordMembersGrid.ItemsSource = FilteredDiscordMembers;
        }

        public RoleRewardBotControl(RoleRewardBot plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }
        
        private async void ForceBoosterRewardPayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (!DiscordBot.IsConnected)
            {
                MessageBox.Show("Bot is not online.  Please start the bot and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await DiscordBot.Pay_Manager.Payout();
        }
        
        private async void RemoveRegisteredMember_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure, removing this person cannot be undone!", "Remove Registered User", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                return;
            
            Instance.Config.RegisteredUsers.RemoveAt(RegisteredMembersGrid.SelectedIndex);
            await Instance.Save();
        }
        
        private void FilterDiscordMembers_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tempTextBox = (TextBox)sender;
            if (tempTextBox is null)
                return;

            if (string.IsNullOrWhiteSpace(tempTextBox.Text))
            {
                DiscordMembersGrid.ItemsSource = null;
                FilteredDiscordMembers.Clear();
                FilteredDiscordMembers.AddRange(DiscordBot.ServerData.DiscordMembers);
                DiscordMembersGrid.ItemsSource = FilteredDiscordMembers;
                FilteredCount.Text = $"Showing: {FilteredDiscordMembers.Count} of {DiscordBot.ServerData.DiscordMembers.Count}";
                return;
            }
            
            DiscordMembersGrid.ItemsSource = null;
            FilteredDiscordMembers.Clear();

            foreach (DiscordMember member in DiscordBot.ServerData.DiscordMembers)
            {
                if (!string.IsNullOrEmpty(member.Nickname) && member.Nickname.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredDiscordMembers.Add(member);
                    continue;
                }

                if (!string.IsNullOrEmpty(member.Username) && member.Username.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredDiscordMembers.Add(member);
                    continue;
                }

                if (!string.IsNullOrEmpty(member.Id.ToString()) && member.Id.ToString().Contains(tempTextBox.Text))
                {
                    FilteredDiscordMembers.Add(member);
                    continue;
                }
            }
            
            DiscordMembersGrid.ItemsSource = FilteredDiscordMembers;
            FilteredCount.Text = $"Showing: {FilteredDiscordMembers.Count} of {DiscordBot.ServerData.DiscordMembers.Count}";
        }
        
        private void FilterRegisteredMembers_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tempTextBox = (TextBox)sender;
            if (tempTextBox is null)
                return;
            
            if (string.IsNullOrWhiteSpace(tempTextBox.Text))
            {
                RegisteredMembersGrid.ItemsSource = null;
                FilteredRegisteredUsers.Clear();
                FilteredRegisteredUsers.AddRange(Config.RegisteredUsers);
                RegisteredMembersGrid.ItemsSource = FilteredRegisteredUsers;
                FilteredRegisteredCount.Text = $"Showing: {FilteredRegisteredUsers.Count} of {Config.RegisteredUsers.Count}";
                return;
            }
            
            RegisteredMembersGrid.ItemsSource = null;
            FilteredRegisteredUsers.Clear();

            foreach (RegisteredUsers registeredMember in Instance.Config.RegisteredUsers)
            {
                if (!string.IsNullOrEmpty(registeredMember.DiscordUsername) && registeredMember.DiscordUsername.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.DiscordId.ToString()) && registeredMember.DiscordId.ToString().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.IngameSteamId.ToString()) && registeredMember.IngameSteamId.ToString().Contains(tempTextBox.Text))
                {
                    FilteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.IngameName) && registeredMember.IngameName.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredRegisteredUsers.Add(registeredMember);
                    continue;
                }
            }

            FilteredRegisteredCount.Text = $"Showing: {FilteredDiscordMembers.Count} of {Config.RegisteredUsers.Count}";
            RegisteredMembersGrid.ItemsSource = FilteredRegisteredUsers;
        }
        
        private void RewardCommandsList_OnSelected(object sender, RoutedEventArgs e)
        {
            Reward command = RewardCommandsList.SelectedItem as Reward;
            ShowCommand.Text = command?.Command;
        }
        
        private async void NewCommand_OnClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(Expires.Text, out int expiredInDays))
            {
                MessageBox.Show("Invalid Entry: Day(s) until expired", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Reward reward = new Reward
            {
                ID = DiscordBot.ID_Manager.GetNewRewardID(),
                Name = NewCommandName.Text,
                Command = CommandText.Text,
                CommandRole = tbCommandRole.Text,
                DaysToPay = tbDaysToPay.Text,
                ExpiresInDays = expiredInDays
            };

            Instance.Config.Rewards.Add(reward);
            
            StringBuilder logNewCommand = new StringBuilder();
            logNewCommand.AppendLine("New reward command created:");
            logNewCommand.AppendLine($"ID: {reward.ID}");
            logNewCommand.AppendLine($"Name: {reward.Name}");
            logNewCommand.AppendLine($"Command: {reward.Command}");
            logNewCommand.AppendLine("Saving settings...");
            await Instance.Save();
            Log.Info(logNewCommand);
        }
        
        private async void ForceBoosterRewardPayoutAll_OnClick(object sender, RoutedEventArgs e)
        {
            if (ForceSelectedPayoutToAll.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a reward command to run on all players.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            
            if ( MessageBox.Show($"Are you sure you want to run the reward command [{Instance.Config.Rewards[ForceSelectedPayoutToAll.SelectedIndex].Name}] on ALL players, regardless if they have already received their rewards or not?  This will not count towards their scheduled reward payments.", "CAUTION!!!", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel) 
                return;

            if (!DiscordBot.IsConnected)
            {
                MessageBox.Show("Bot is not online.  Please start the bot and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            await DiscordBot.Pay_Manager.Payout(Instance.Config.Rewards[ForceSelectedPayoutToAll.SelectedIndex].ID, true);
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
            e.Handled = true;
        }
        
        private void SendDmToPlayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (DiscordMembersGrid.SelectedIndex == -1) return;
            
            SendDiscordPM SendDMWindow = new SendDiscordPM((DiscordMember)DiscordMembersGrid.SelectedItem);
            SendDMWindow.ShowDialog();
        }

        private void EnableOnlineCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            switch (cb.IsChecked)
            {
                case null:
                    return;
                default:
                    Instance.Config.EnabledOnGameStart = cb.IsChecked.Value;
                    break;
            }
        }
        
        private void EnableOfflineCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            switch (cb.IsChecked)
            {
                case null:
                    return;
                default:
                    Instance.Config.EnabledOnAppStart = cb.IsChecked.Value;
                    break;
            }
        }

        private void RemoveBannedUsersFromRegistryCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            switch (cb.IsChecked)
            {
                case null:
                    return;
                default:
                    Instance.Config.RemoveOnBannedUser = cb.IsChecked.Value;
                    break;
            }
        }
        
        private async void CreateManualPayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ulong.TryParse(tbSteamID.Text, out ulong steamId))
            {
                MessageBox.Show("The SteamID is invalid, try again.  Only numbers are allowed.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!ulong.TryParse(tbDiscordID.Text, out ulong discordId))
            {
                MessageBox.Show("The DiscordID is invalid, try again.  Only numbers are allowed.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(tbManualExpires.Text, out int intManualExpires))
            {
                MessageBox.Show("The expiry is invalid, try again.  Only numbers are allowed.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (intManualExpires <= 0)
            {
                MessageBox.Show("The expiry is invalid, try again.  Cannot be equal to or less than 0.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (intManualExpires > 365)
            {
                MessageBox.Show("The expiry is invalid, a payout cannot last longer than 1 year (365 days).", "Even the Matrix has its limitations Mr.Anderson!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await DiscordBot.Pay_Manager.ManualPayout(discordId, tbDiscordName.Text, tbInGameName.Text, steamId, tbCommand.Text, intManualExpires);
        }
        
        private void TbRoleComboBox_OnSelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            tbCommandRole.Text = ((DiscordRole)tbRoleComboBox.SelectedItem).Name;
        }
        
        private async void EditPayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ulong.TryParse(TbEditSteamId.Text, out ulong updatedSteamID))
            {
                MessageBox.Show("Check your SteamID value.  Cannot convert.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (!ulong.TryParse(TbEditDiscordID.Text, out ulong updatedDiscordID))
            {
                MessageBox.Show("Check your Discord ID value.  Cannot convert.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (!int.TryParse(TbEditExpiry.Text, out int newExpiryInDays))
            {
                MessageBox.Show("Check your Days until expired value.  Cannot convert.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (newExpiryInDays <= 0)
            {
                MessageBox.Show("The expiry is invalid, try again.  Cannot be equal to or less than 0.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            
            if (newExpiryInDays > 365)
            {
                MessageBox.Show("The expiry is invalid, a payout cannot last longer than 1 year (365 days).", "Even the Matrix has its limitations Mr.Anderson!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Payout originalReward = Instance.Config.Payouts[PayoutList.SelectedIndex];

            StringBuilder logReport = new StringBuilder();
            logReport.AppendLine($"Player Reward has been changed!");
            logReport.AppendLine($" ** Original ** ");
            logReport.AppendLine($"ID           -> {originalReward.ID}");
            logReport.AppendLine($"In-Game Name -> {originalReward.IngameName}");
            logReport.AppendLine($"SteamID      -> {originalReward.SteamID}");
            logReport.AppendLine($"Discord Name -> {originalReward.DiscordName}");
            logReport.AppendLine($"Discord ID   -> {originalReward.DiscordId}");
            logReport.AppendLine($"Expiry       -> [{(originalReward.ExpiryDate - DateTime.Now).Days}]{originalReward.ExpiryDate}");
            
            int indexEdit = PayoutList.SelectedIndex;
            if (!Instance.Config.Payouts[indexEdit].ChangeDaysUntilExpire(newExpiryInDays, out string error))
            {
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            Instance.Config.Payouts[indexEdit].IngameName = TbEditInGameName.Text;
            Instance.Config.Payouts[indexEdit].SteamID = updatedSteamID;
            Instance.Config.Payouts[indexEdit].DiscordName = TbEditDiscordName.Text;
            Instance.Config.Payouts[indexEdit].DiscordId = updatedDiscordID;
            Instance.Config.Payouts[indexEdit].Command = tbEditCommand.Text;
            Instance.Config.Payouts[indexEdit].ExpiryDate = DateTime.Now + TimeSpan.FromDays(newExpiryInDays); 
            

            logReport.AppendLine("");
            logReport.AppendLine(" ** Updated **");
            logReport.AppendLine($"ID           -> {originalReward.ID}");
            logReport.AppendLine($"In-Game Name -> {TbEditInGameName.Text}");
            logReport.AppendLine($"SteamID      -> {TbEditSteamId.Text}");
            logReport.AppendLine($"Discord Name -> {TbEditDiscordName.Text}");
            logReport.AppendLine($"Discord ID   -> {updatedDiscordID}");
            logReport.AppendLine($"Expiry       -> [{newExpiryInDays} days]{DateTime.Now + TimeSpan.FromDays(newExpiryInDays)}");
            
            Log.Warn(logReport);
            PayoutList.ItemsSource = null;
            PayoutList.ItemsSource = Instance.Config.Payouts;

            await Instance.Save();
        }
        
        private void PayoutList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Payout editPayout = (Payout)PayoutList.SelectedItem;
            if (editPayout == null) return;
            lastSelectedItem = PayoutList.SelectedIndex;
            
            TbEditInGameName.Text = editPayout.IngameName;
            TbEditSteamId.Text = editPayout.SteamID.ToString();
            TbEditDiscordName.Text = editPayout.DiscordName;
            TbEditDiscordID.Text = editPayout.DiscordId.ToString();
            tbEditCommand.Text = editPayout.Command;
            TbEditExpiry.Text = (editPayout.ExpiryDate - DateTime.Now).Days.ToString();
        }

        private async void DeletePayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (lastSelectedItem == -1) return;
            Payout payout = (Payout) PayoutList.Items.GetItemAt(lastSelectedItem);
            if (payout == null) return;
            if (payout.DiscordId == 0) return;
            
            StringBuilder logDeletePayout = new StringBuilder();
            logDeletePayout.AppendLine("Player Reward Manually Deleted:");
            logDeletePayout.AppendLine($"In-Game Name -> {payout.IngameName}");
            logDeletePayout.AppendLine($"SteamID      -> {payout.SteamID.ToString()}");
            logDeletePayout.AppendLine($"Discord Name -> {payout.DiscordName}");
            logDeletePayout.AppendLine($"Discord ID   -> {payout.DiscordId.ToString()}");
            logDeletePayout.AppendLine($"Command      -> {payout.Command}");
            logDeletePayout.AppendLine($"Expires      -> ({payout.DaysUntilExpired.ToString()} days)  {Instance.Config.Payouts[PayoutList.SelectedIndex].ExpiryDate}");

            Log.Warn(logDeletePayout);
            Instance.Config.Payouts.Remove(payout); 
            await Instance.Save();
        }
        
        private async void DeleteSelectedReward_OnClick(object sender, RoutedEventArgs e)
        {
            Reward reward = (Reward) RewardCommandsList.SelectedItem;
            RewardCommandsList.ItemsSource = null;
            Instance.Config.Rewards.Remove(reward);
            RewardCommandsList.ItemsSource = Instance.Config.Rewards;
            await Instance.Save();
        }
    }
}
