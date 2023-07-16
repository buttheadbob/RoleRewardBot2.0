using System.Windows;
using System.Windows.Controls;

namespace RoleRewardBot
{
    public partial class RoleRewardBotControl : UserControl
    {

        private RoleRewardBot Plugin { get; }

        private RoleRewardBotControl()
        {
            InitializeComponent();
        }

        public RoleRewardBotControl(RoleRewardBot plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }
    }
}
