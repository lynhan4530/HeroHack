using TaleWorlds.Engine.GauntletUI;

namespace HeroHack.UI
{
    public class HeroHackLayer : GauntletLayer
    {
        private readonly HeroHackPanelVM _panelVM;

        public bool IsOpen => _panelVM.IsOpen;

        public HeroHackLayer() : base("GauntletLayer", 200)
        {
            _panelVM = new HeroHackPanelVM();
            LoadMovie("HeroHackPanel", _panelVM);
        }

        public void TogglePanel()
        {
            _panelVM.IsOpen = !_panelVM.IsOpen;
        }

        protected override void Tick(float dt)
        {
            base.Tick(dt);
            _panelVM.Tick(dt);
        }
    }
}
