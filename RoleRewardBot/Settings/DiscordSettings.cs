using RoleRewardBot.Discord;

namespace RoleRewardBot
{
    public partial class MainConfig
    {
        private bool m_enabledOnAppStart;
        public bool EnabledOnAppStart { get => m_enabledOnAppStart; set => SetValue(ref m_enabledOnAppStart, value); }
        
        private bool m_enabledOnGameStart;
        public bool EnabledOnGameStart { get => m_enabledOnGameStart; set => SetValue(ref m_enabledOnGameStart, value); }

        private bool m_removeOnBannedUser = true;
        public bool RemoveOnBannedUser { get => m_removeOnBannedUser; set => SetValue(ref m_removeOnBannedUser, value); }

        private string m_token = "";
        public string Token { get => m_token; set => SetValue(ref m_token, value); }
        
        private string[] m_prefixes = new string[] { "#" };
        public string[] Prefixes { get => m_prefixes; set => SetValue(ref m_prefixes, value); }
        
        private string m_discordBotName = "RoleRewardBot";
        public string DiscordBotName { get => m_discordBotName; set => SetValue(ref m_discordBotName, value); }

        private string m_statusMessage = "Rewarding Space Engineers!!";
        public string StatusMessage { get => m_statusMessage; set => SetValue(ref m_statusMessage, value); }

        private string m_BotStatus;
        public string BotStatus { get => m_BotStatus; set => SetValue(ref m_BotStatus, value); }

        private bool m_manageRegisteredRole;
        public bool ManageRegisteredRole { get => m_manageRegisteredRole; set => SetValue(ref m_manageRegisteredRole, value); }

        private string m_registeredRoleId;
        public string RegisteredRoleId { get => m_registeredRoleId; set => SetValue(ref m_registeredRoleId, value); }

        private string m_discordServerId;
        public string DiscordServerId { get => m_discordServerId; set => SetValue(ref m_discordServerId, value); }
    }
}