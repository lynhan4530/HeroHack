using System;
using TaleWorlds.Library;

namespace HeroHack.UI
{
    public class HeroHackHudVM : ViewModel
    {
        private readonly Action _onToggle;

        public HeroHackHudVM(Action onToggle)
        {
            _onToggle = onToggle;
        }

        public void ExecuteTogglePanel()
        {
            _onToggle();
        }
    }
}
