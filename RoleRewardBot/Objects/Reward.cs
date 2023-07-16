using System;

namespace RoleRewardBot.Objects
{
    public sealed class Reward
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public string CommandRole { get; set; }
        public int ExpiresInDays { get; set; }
        public string DaysToPay { get; set; }
        public DateTime LastRun { get; set; }
    }
}