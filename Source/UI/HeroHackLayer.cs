using System;
using System.IO;
using System.Text;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace HeroHack.UI
{
    public class HeroHackLayer : GauntletLayer
    {
        private readonly HeroHackPanelVM _panelVM;
        private bool _dumpRequested;
        private int _dumpFrameDelay;

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
            if (_panelVM.IsOpen)
            {
                _dumpRequested = true;
                _dumpFrameDelay = 5; // wait 5 frames for layout to settle
            }
        }

        protected override void Tick(float dt)
        {
            base.Tick(dt);
            _panelVM.Tick(dt);

            if (_dumpRequested)
            {
                _dumpFrameDelay--;
                if (_dumpFrameDelay <= 0)
                {
                    _dumpRequested = false;
                    DumpWidgetTree();
                }
            }
        }

        private void DumpWidgetTree()
        {
            try
            {
                var root = UIContext?.Root;
                if (root == null)
                {
                    D("DUMP: UIContext.Root is null");
                    return;
                }
                var sb = new StringBuilder();
                sb.AppendLine("=== WIDGET TREE DUMP ===");
                WalkWidget(root, 0, sb);
                sb.AppendLine("=== END DUMP ===");

                var dumpPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "HeroHack", "widget_dump.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(dumpPath)!);
                File.WriteAllText(dumpPath, sb.ToString());
                D($"Widget tree dumped to {dumpPath}");
            }
            catch (Exception ex)
            {
                D($"DUMP ERROR: {ex.Message}");
            }
        }

        private static void WalkWidget(Widget w, int depth, StringBuilder sb)
        {
            if (depth > 12) return; // safety limit

            var indent = new string(' ', depth * 2);
            var typeName = w.GetType().Name;
            var vis = w.IsVisible ? "V" : "H";
            var posX = w.Left;
            var posY = w.Top;
            var szW = w.Right - w.Left;
            var szH = w.Bottom - w.Top;

            var extra = "";
            if (w is TextWidget tw && !string.IsNullOrEmpty(tw.Text))
                extra = $" Text=\"{Truncate(tw.Text, 30)}\"";
            else if (w is EditableTextWidget etw && !string.IsNullOrEmpty(etw.Text))
                extra = $" Text=\"{Truncate(etw.Text, 30)}\"";

            var id = string.IsNullOrEmpty(w.Id) ? "" : $" Id={w.Id}";

            sb.AppendLine($"{indent}[{vis}] {typeName}{id} Y={posY:F0} X={posX:F0} W={szW:F0} H={szH:F0}{extra}");

            for (int i = 0; i < w.ChildCount; i++)
            {
                WalkWidget(w.GetChild(i), depth + 1, sb);
            }
        }

        private static string Truncate(string s, int max)
        {
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}
