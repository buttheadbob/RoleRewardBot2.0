using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using NLog;
using RoleRewardBot.Discord.Utils;
using RoleRewardBot.Utils;

namespace RoleRewardBot.Discord
{
    public sealed class Bot
    {
        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public Logger Log = LogManager.GetLogger("Rewards Discord Bot");
        public enum BotStatus { Online, Offline, Connecting, Disconnecting }
        public BotStatus botStatus = BotStatus.Offline;
        private Bot_Subscriptions m_subscriptions = new Bot_Subscriptions();
        public ServerData ServerData = new ServerData();
        public DiscordUser BotUser { get; set; }
        public SendDM DMSender = new SendDM();
        public IDManager ID_Manager = new IDManager();
        public PayManager Pay_Manager = new PayManager();

        public bool IsBotOnline()
        {
            return botStatus == BotStatus.Online;
        }

        public async Task RunAsync()
        {
            // Setup the client configuration
            DiscordConfiguration d_config = new DiscordConfiguration()
            {
                Token = RoleRewardBot.Instance.Config.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                Intents = DiscordIntents.All,
                HttpTimeout = TimeSpan.FromSeconds(30),
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

            if (RoleRewardBot.Instance.Config.EnabledOnAppStart)
                await Client.ConnectAsync();
            
            await Task.Delay(-1);
        }

        private Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs args)
        {
            Log.Error(args.Exception, "Command Errored!");
            return Task.CompletedTask; 
        }
    }
}