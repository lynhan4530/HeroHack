using System;
using TaleWorlds.Engine.GauntletUI;

namespace HeroHack.UI
{
    public class HeroHackHudLayer : GauntletLayer
    {
        private readonly HeroHackHudVM _hudVM;

        public HeroHackHudLayer(Action onToggle) : base("GauntletLayer", 100)
        {
            _hudVM = new HeroHackHudVM(onToggle);
            LoadMovie("HeroHackHud", _hudVM);
        }
    }
}
