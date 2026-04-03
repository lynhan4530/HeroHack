using System;
using System.IO;
using TaleWorlds.Engine.GauntletUI;

namespace HeroHack.UI
{
    public class HeroHackLayer : GauntletLayer
    {
        private readonly HeroHackPanelVM _panelVM;

        public bool IsOpen => _panelVM.IsOpen;

        private static readonly string DebugLog = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "HeroHack", "debug.txt");

        private static void D(string msg)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DebugLog)!);
                File.AppendAllText(DebugLog, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
            }
            catch { }
        }

        public HeroHackLayer() : base("GauntletLayer", 200)
        {
            D("HeroHackLayer ctor start");
            _panelVM = new HeroHackPanelVM();
            D("PanelVM created OK");
            LoadMovie("HeroHackPanel", _panelVM);
            D("LoadMovie returned OK");
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
