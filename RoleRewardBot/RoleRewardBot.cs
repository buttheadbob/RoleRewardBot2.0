using NLog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using DSharpPlus.Entities;
using RoleRewardBot.Discord;
using RoleRewardBot.Utils;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;

namespace RoleRewardBot
{
    public class RoleRewardBot : TorchPluginBase, IWpfPlugin
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly string CONFIG_FILE_NAME = "RoleRewardBot_Config.cfg";
        private RoleRewardBotControl _control;
        public UserControl GetControl() => _control ?? (_control = new RoleRewardBotControl(this));
        private Persistent<MainConfig> _config;
        public MainConfig Config => _config?.Data;
        public static RoleRewardBot Instance;
        public Dispatcher MainDispatcher;
        public bool WorldOnline;
        public static readonly TorchCommandManager CommandsManager = new TorchCommandManager();
        public static Bot DiscordBot = new Bot();

        public override async void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;
            SetupConfig();
            //await OldRewardBotConfig.Import();  Uploaded v2 as its own plugin and marked v1 as obsolete but still on torch.  lets not import old config anymore.
            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            await Save();
            
            Config.BotStatus = "Offline";

            if (Config.EnabledOnAppStart)
                await DiscordBot.ConnectAsync();
        }
        
        private async void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    WorldOnline = true;
                    if (DiscordBot.IsConnected)
                        await DiscordBot.Client.UpdateStatusAsync(new DiscordActivity(Instance.Config.StatusMessage, ActivityType.Playing), UserStatus.Online);
                    else
                    {
                        if (Config.EnabledOnGameStart)
                        {
                            await DiscordBot.ConnectAsync();
                        }
                    }
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    WorldOnline = false;
                    if (DiscordBot.IsConnected)
                        await DiscordBot.Client.UpdateStatusAsync(new DiscordActivity("for the server to come online...", ActivityType.Watching), UserStatus.DoNotDisturb);
                    break;
            }
        }

        private void SetupConfig()
        {
            var configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

            try
            {
                _config = Persistent<MainConfig>.Load(configFile);

            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {
                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<MainConfig>(configFile, new MainConfig());
                _config.Save();
            }
        }

        public Task Save()
        {
            try
            {
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            DiscordBot.Dispose();
            base.Dispose();
        }
    }
}
