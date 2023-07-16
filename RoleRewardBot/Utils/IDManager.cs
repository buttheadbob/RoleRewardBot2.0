namespace RoleRewardBot.Utils
{
    public class IDManager
    {
        private static object lastIDLOCK = new object();
        private static object lastRewardLOCK = new object();
        private MainConfig Config => RoleRewardBot.Instance.Config;

        public int GetNewPayoutID()
        {
            lock (lastIDLOCK)
            {
                return Config.LastPayoutId++;
            }
        }

        public int GetLastPayoutID()
        {
            lock (lastIDLOCK)
            {
                return Config.LastPayoutId;
            }
        }

        public int GetNewRewardID()
        {
            lock (lastRewardLOCK)
            {
                return Config.LastRewardId++;
            }
        }

        public int GetLastRewardID()
        {
            lock (lastRewardLOCK)
            {
                return Config.LastRewardId;
            }
        }
    }
}