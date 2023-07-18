using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging.Abstractions;
using NLog;
using RoleRewardBot.Discord.Utils;
using RoleRewardBot.Utils;

namespace RoleRewardBot.Discord
{
    public sealed class Bot
    {
        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        private CommandsNextExtension Commands { get; set; }
        private Logger Log = LogManager.GetLogger("Rewards Discord Bot");
        public enum BotStatus { Online, Offline, Connecting, Disconnecting }
        public BotStatus botStatus = BotStatus.Offline;
        private Bot_Subscriptions m_subscriptions = new Bot_Subscriptions();
        public ServerData ServerData = new ServerData();
        public DiscordUser BotUser { get; set; }
        public readonly SendDM DMSender = new SendDM();
        public readonly IDManager ID_Manager = new IDManager();
        public readonly PayManager Pay_Manager = new PayManager();
        public bool IsConnected { get; set; }
        private bool Inited; 

        public bool IsBotOnline()
        {
            return botStatus == BotStatus.Online;
        }

        public async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(RoleRewardBot.Instance.Config.Token))
            {
                Log.Error("Invalid Bot Token, please set the token in the settings tab.");
                return;
            }

            if (!Inited)
            {
                if (!await InitAsync()) return; // Init failed, dont proceed.
                Log.Info("Valid Token, Connecting...");
                await Client.ConnectAsync();
                return;
            }
            Log.Info("Init already completed, Connecting...");
            await Client.ConnectAsync(); // Already Init'd, just connect.
        }

        private Task<bool> InitAsync()
        {
            if (string.IsNullOrWhiteSpace(RoleRewardBot.Instance.Config.Token))
            {
                Log.Error("Invalid Bot Token, please set the token in the settings tab.");
                return Task.FromResult(false);
            }
            
            Log.Info("Valid Token, Initing...");
            // Setup the client configuration
            DiscordConfiguration d_config = new DiscordConfiguration()
            {
                Token = RoleRewardBot.Instance.Config.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                Intents = DiscordIntents.All,
                HttpTimeout = TimeSpan.FromSeconds(30),
                LoggerFactory = new DSharpPlusNLogAdapter( LogLevel.Trace),
            };

            Client = new DiscordClient(d_config);
            
            // Setup the interactivity module
            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(1)
            });
            
            // Setup the commands module
            CommandsNextConfiguration c_config = new CommandsNextConfiguration()
            {
                StringPrefixes = RoleRewardBot.Instance.Config.Prefixes,
                EnableDms = true,
                EnableDefaultHelp = false,
                EnableMentionPrefix = true
            };
            
            Commands = Client.UseCommandsNext(c_config);
            SlashCommandsExtension slashCommandsConfig = Client.UseSlashCommands();
            
            // Prefix Commands
              // I may add some later, but for now, I don't think I need them.
            
            // Slash Commands
            slashCommandsConfig.RegisterCommands<DiscordSlashCommands>(1089078620829536269);
            
            Commands.CommandErrored += Commands_CommandErrored;
            
            Client.GuildDownloadCompleted += m_subscriptions.Client_GuildDownloadCompleted;
            Inited = true;
            return Task.FromResult(true);
        }

        private Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs args)
        {
            Log.Error(args.Exception, "Command Errored!");
            return Task.CompletedTask; 
        }

        public void Dispose()
        {
            Client?.Dispose();
            Interactivity?.Dispose();
            Commands?.Dispose();
        }
    }
}