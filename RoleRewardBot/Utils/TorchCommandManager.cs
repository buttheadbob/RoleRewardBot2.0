using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Torch.API.Managers;
using Torch.Commands;

namespace RoleRewardBot.Utils
{
    public class TorchCommandManager
    {
        private CommandManager _manager;
        private Logger Log = LogManager.GetLogger("RoleRewardBot => TorchCommandManager");

        private Task<bool> GetTorchCommandManager()
        {
            if (_manager != null)
                return Task.FromResult(true);
            
            if (RoleRewardBot.Instance.Torch.CurrentSession.Managers.GetManager<CommandManager>() == null)
                return Task.FromResult(false);
            
            _manager = RoleRewardBot.Instance.Torch.CurrentSession.Managers.GetManager<CommandManager>();
            return Task.FromResult(true);
        }
        
        public async Task Run(string command)
        {
            if (await GetTorchCommandManager() == false)
                return;
                
            if (_manager == null)
                Log.Error($"Command Manager unable to run command [{command}].  Torch has no active command manager.");
            
            if (!RoleRewardBot.Instance.WorldOnline)
                Log.Error($"Command Manager unable to run command [{command}].  The server is offline.");
            
            _manager?.HandleCommandFromServer(command);
        }

        public async Task RunSlow(List<string> commands)
        {
            // When a player has a list of 3 or more commands to run, they will run then here.  This prevents
            // and possible server bogging if the commands require heavy processing.  This only blocks the current
            // task in awaitable state, so everything else can still run.
            
            if (await GetTorchCommandManager() == false)
                Log.Error($"Command Manager unable to run (slow) commands.  Torch has no active command manager.");
            
            if (!RoleRewardBot.Instance.WorldOnline)
                Log.Error($"Command Manager unable to run (slow) command.  The server is offline.");

            foreach (string command in commands)
            {
                _manager?.HandleCommandFromServer(command);
                await Task.Delay(5000);
            }
        }
    }
}