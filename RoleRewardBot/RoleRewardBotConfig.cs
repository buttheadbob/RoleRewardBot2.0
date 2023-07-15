using System;
using System.Collections.Generic;
using Torch;

namespace RoleRewardBot
{
    public class RoleRewardBotConfig : ViewModel
    {

        private string _StringProperty = "root";
        private int _IntProperty = 0;
        private bool _BoolProperty = true;

        public string StringProperty { get => _StringProperty; set => SetValue(ref _StringProperty, value); }
        public int IntProperty { get => _IntProperty; set => SetValue(ref _IntProperty, value); }
        public bool BoolProperty { get => _BoolProperty; set => SetValue(ref _BoolProperty, value); }
    }
}
