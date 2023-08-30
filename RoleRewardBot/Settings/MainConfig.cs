using System;
using RoleRewardBot.Objects;
using Torch;

namespace RoleRewardBot
{
    public partial class MainConfig : ViewModel
    {
        private int m_lastPayoutId = 1;
        public int LastPayoutId { get => m_lastPayoutId; set => SetValue(ref m_lastPayoutId, value); }
        
        private int m_lastRewardId = 1;
        public int LastRewardId { get => m_lastRewardId; set => SetValue(ref m_lastRewardId, value); }
        
        private MyList<RegisteredUsers> m_registeredUsers = new MyList<RegisteredUsers>();
        public MyList<RegisteredUsers> RegisteredUsers { get => m_registeredUsers; set => SetValue(ref m_registeredUsers, value); }
        
        private MyList<Reward> m_rewardCommands = new MyList<Reward>();
        public MyList<Reward> Rewards { get => m_rewardCommands; set => SetValue(ref m_rewardCommands, value); }
        
        private MyList<Payout> m_payouts = new MyList<Payout>();
        public MyList<Payout> Payouts { get => m_payouts; set => SetValue(ref m_payouts, value); }
        
        private MyList<LinkRequest> m_linkRequests = new MyList<LinkRequest>();
        public MyList<LinkRequest> LinkRequests { get => m_linkRequests; set => SetValue(ref m_linkRequests, value); }
        
        private DateTime m_lastScheduledPayoutProcessed = DateTime.Now;
        public DateTime lastScheduledPayoutProcessed { get => m_lastScheduledPayoutProcessed; set => SetValue(ref m_lastScheduledPayoutProcessed, value); }
    }
}
