using System.Text;
using System.Windows;
using DSharpPlus.Entities;
using NLog;

namespace RoleRewardBot.UI
{
    public partial class SendDiscordPM : Window
    {
        private DiscordMember userToPM;
        private Logger Log = LogManager.GetLogger("Reward Bot => Send DM");
        
        public SendDiscordPM(DiscordMember user)
        {
            InitializeComponent();
            userToPM = user;
        }
        
        private async void Send_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Message.Text))
            {
                MessageBox.Show("Enter a message!", "Spamming blank messages.....", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DiscordUser user = await RoleRewardBot.DiscordBot.ServerData.guild.GetMemberAsync(userToPM.Id);
            string results = await RoleRewardBot.DiscordBot.DMSender.SendDirectMessage(userToPM, Message.Text);
            Close();
            MessageBox.Show(results, "Reply from Discord", MessageBoxButton.OK, MessageBoxImage.Information);

            StringBuilder logMessage = new StringBuilder();
            logMessage.AppendLine($"DIRECT MESSAGE sent to {user.Username}");
            logMessage.AppendLine("———————————————————————————————————————");
            logMessage.AppendLine(Message.Text);
            logMessage.AppendLine("———————————————————————————————————————");
            
            Log.Info(logMessage);
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}