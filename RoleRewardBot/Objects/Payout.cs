using System;

namespace RoleRewardBot.Objects
{
    public sealed class Payout
    {
        public int ID { get; set; }
        public string RewardName { get; set; }
        public ulong SteamID { get; set; }
        public string IngameName { get; set; }
        public string DiscordName { get; set; }
        public string Command { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public ulong DiscordId { get; set; }
        public string DaysUntilExpired => (ExpiryDate - PaymentDate).Days.ToString();

        public bool ChangeDaysUntilExpire(int days, out string error)
        {
            error = "";
            if (days < 1)
            {
                error = "Cannot use 0 for expiry value, this would expire the instance it was created and be deleted on next cleanup run!!.";
                return false;
            }
            
            ExpiryDate = DateTime.Now.AddDays(days);

            return true;
        } 
    }
}