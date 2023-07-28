using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NLog;
using RoleRewardBot.Objects;

namespace RoleRewardBot.Utils
{
    public static class OldRewardBotConfig
    {
        private const string fileName = "RewardsBotConfig.cfg";
        private static string StoragePath = RoleRewardBot.Instance.StoragePath;
        private static Logger Log = LogManager.GetLogger("Role Rewards Bot => Config Importer");
        private static MainConfig Config => RoleRewardBot.Instance.Config;
        
        public static async Task Import()
        {
            if (!File.Exists(Path.Combine(StoragePath, fileName)))
                return;
            
            RewardsBotConfig oldConfig;
            
            XmlSerializer XmlSerialization = new XmlSerializer(typeof(RewardsBotConfig));
            using (StreamReader reader = new StreamReader(Path.Combine(StoragePath, fileName)))
            {
                oldConfig = XmlSerialization.Deserialize(reader) as RewardsBotConfig;
            }

            if (oldConfig is null)
            {
                Log.Warn("Old config is null, nothing to do.");
            }
            
            StringBuilder logger = new StringBuilder();
            logger.AppendLine("Importing old config...");
            
            if (!string.IsNullOrWhiteSpace(oldConfig.BotKey))
            {
                Config.Token = oldConfig.BotKey;
                logger.AppendLine("Imported BotKey");
            }
            else
            {
                logger.AppendLine("Failed to import BotKey");
            }
            
            if (!string.IsNullOrWhiteSpace(oldConfig.BotName))
            {
                Config.DiscordBotName = oldConfig.BotName;
                logger.AppendLine("Imported BotName");
            }
            else
            {
                logger.AppendLine("Failed to import BotName");
            }
            
            if (!string.IsNullOrWhiteSpace(oldConfig.BotStatusMessage))
            {
                Config.StatusMessage = oldConfig.BotStatusMessage;
                logger.AppendLine("Imported BotStatusMessage");
            }
            else
            {
                logger.AppendLine("Failed to import BotStatusMessage");
            }
            
            if (oldConfig.EnabledOffline)
            {
                Config.EnabledOnAppStart = oldConfig.EnabledOffline;
                logger.AppendLine("Imported EnabledOffline");
            }
            else
            {
                logger.AppendLine("Failed to import EnabledOffline");
            }
            
            if (oldConfig.EnabledOnline)
            {
                Config.EnabledOnGameStart = oldConfig.EnabledOnline;
                logger.AppendLine("Imported EnabledOnline");
            }
            else
            {
                logger.AppendLine("Failed to import EnabledOnline");
            }
            
            if (oldConfig.RemoveBannedUsersFromRegistry)
            {
                Config.RemoveOnBannedUser = oldConfig.RemoveBannedUsersFromRegistry;
                logger.AppendLine("Imported RemoveBannedUsersFromRegistry");
            }
            else
            {
                logger.AppendLine("Failed to import RemoveBannedUsersFromRegistry");
            }
            
            if (oldConfig.LinkRequests != null)
            {
                Config.LinkRequests.AddRange(oldConfig.LinkRequests);
                logger.AppendLine("Imported LinkRequests");
            }
            else
            {
                logger.AppendLine("Failed to import LinkRequests");
            }
            
            if (oldConfig.RegisteredUsers != null)
            {
                Config.RegisteredUsers.AddRange(oldConfig.RegisteredUsers);
                logger.AppendLine("Imported RegisteredUsers");
            }
            else
            {
                logger.AppendLine("Failed to import RegisteredUsers");
            }
            
            if (oldConfig.Rewards != null)
            {
                Config.Rewards.AddRange(oldConfig.Rewards);
                logger.AppendLine("Imported Rewards");
            }
            else
            {
                logger.AppendLine("Failed to import Rewards");
            }
            
            if (oldConfig.Payouts != null)
            {
                Config.Payouts.AddRange(oldConfig.Payouts);
                logger.AppendLine("Imported Payouts");
            }
            else
            {
                logger.AppendLine("Failed to import Payouts");
            }
            
            Config.LastPayoutId = oldConfig.LastPayoutId;
            Config.LastRewardId = oldConfig.LastRewardId;
            logger.AppendLine("Imported Control ID's");

            Log.Info(logger.ToString());

            await FinishedWithOldConfig(Path.Combine(StoragePath, fileName));
        }
        
        private static Task FinishedWithOldConfig(string path_fileName)
        {
            File.Move(path_fileName, path_fileName + ".old");
            Log.Info("Renamed old config file to {0}", path_fileName + ".old");
            return Task.CompletedTask;
        }
    }
    
    
    
    public class RewardsBotConfig
    {
        public string BotName;
        public string BotKey;
        public bool EnabledOffline;
        public bool EnabledOnline;
        public string BotStatusMessage;
        public bool RemoveBannedUsersFromRegistry;
        public List<LinkRequest> LinkRequests;
        public List<RegisteredUsers> RegisteredUsers;
        public List<Reward> Rewards;
        public List<Payout> Payouts;
        public int LastPayoutId;
        public int LastRewardId;
    }
}