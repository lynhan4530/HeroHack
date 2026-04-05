using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeroHack.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace HeroHack.UI
{
    public class IOTabVM : ViewModel
    {
        private readonly Action<string> _onStatusUpdate;

        // ── Export hero nav ───────────────────────────────────────────────────
        private List<Hero> _exportHeroes = new List<Hero>();
        private int _exportHeroIndex;
        private string _exportHeroName = "—";
        private string _exportCountText = "0 / 0";

        // ── Import file nav ───────────────────────────────────────────────────
        // Bug 13: full paths stored internally; only filename shown in UI
        private List<string> _importFilePaths = new List<string>();
        private int _importFileIndex;
        private string _importFileName = "— no files —";
        private string _importFileCountText = "0 / 0";

        // ── Import target hero nav ────────────────────────────────────────────
        private List<Hero> _importTargetHeroes = new List<Hero>();
        private int _importTargetIndex;
        private string _importTargetName = "—";
        private string _importTargetCountText = "0 / 0";

        // ── Two-step confirm ──────────────────────────────────────────────────
        private bool _showImportConfirm;
        private string _importConfirmText = string.Empty;

        public IOTabVM(Action<string> onStatusUpdate)
        {
            _onStatusUpdate = onStatusUpdate;
            // Bug 6: no game API calls in constructor
        }

        // ── Properties ───────────────────────────────────────────────────────

        [DataSourceProperty]
        public string ExportHeroName
        {
            get => _exportHeroName;
            set { if (_exportHeroName != value) { _exportHeroName = value; OnPropertyChangedWithValue(value, nameof(ExportHeroName)); } }
        }

        [DataSourceProperty]
        public string ExportCountText
        {
            get => _exportCountText;
            set { if (_exportCountText != value) { _exportCountText = value; OnPropertyChangedWithValue(value, nameof(ExportCountText)); } }
        }

        [DataSourceProperty]
        public string ImportFileName
        {
            // Bug 13: displays Path.GetFileName, never the full path
            get => _importFileName;
            set { if (_importFileName != value) { _importFileName = value; OnPropertyChangedWithValue(value, nameof(ImportFileName)); } }
        }

        [DataSourceProperty]
        public string ImportFileCountText
        {
            get => _importFileCountText;
            set { if (_importFileCountText != value) { _importFileCountText = value; OnPropertyChangedWithValue(value, nameof(ImportFileCountText)); } }
        }

        [DataSourceProperty]
        public string ImportTargetName
        {
            get => _importTargetName;
            set { if (_importTargetName != value) { _importTargetName = value; OnPropertyChangedWithValue(value, nameof(ImportTargetName)); } }
        }

        [DataSourceProperty]
        public string ImportTargetCountText
        {
            get => _importTargetCountText;
            set { if (_importTargetCountText != value) { _importTargetCountText = value; OnPropertyChangedWithValue(value, nameof(ImportTargetCountText)); } }
        }

        [DataSourceProperty]
        public bool ShowImportConfirm
        {
            get => _showImportConfirm;
            set { if (_showImportConfirm != value) { _showImportConfirm = value; OnPropertyChangedWithValue(value, nameof(ShowImportConfirm)); } }
        }

        [DataSourceProperty]
        public string ImportConfirmText
        {
            get => _importConfirmText;
            set { if (_importConfirmText != value) { _importConfirmText = value; OnPropertyChangedWithValue(value, nameof(ImportConfirmText)); } }
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        // Called from HeroHackPanelVM.ExecuteSelectTab3() so data is always fresh
        public void RefreshDisplay()
        {
            RefreshHeroLists();
            RefreshFileList();
            ShowImportConfirm = false;
        }

        private void RefreshHeroLists()
        {
            _exportHeroes.Clear();
            _importTargetHeroes.Clear();

            if (Campaign.Current == null || Hero.MainHero == null) return;

            _exportHeroes.Add(Hero.MainHero);
            _importTargetHeroes.Add(Hero.MainHero);

            if (MobileParty.MainParty?.MemberRoster != null)
            {
                foreach (var element in MobileParty.MainParty.MemberRoster.GetTroopRoster())
                {
                    Hero? companion = element.Character?.HeroObject;
                    if (companion != null && companion != Hero.MainHero
                        && companion.IsActive && companion.IsPlayerCompanion)
                    {
                        _exportHeroes.Add(companion);
                        _importTargetHeroes.Add(companion);
                    }
                }
            }

            _exportHeroIndex    = Math.Min(_exportHeroIndex,    Math.Max(0, _exportHeroes.Count - 1));
            _importTargetIndex  = Math.Min(_importTargetIndex,  Math.Max(0, _importTargetHeroes.Count - 1));
            UpdateExportHeroDisplay();
            UpdateImportTargetDisplay();
        }

        private void RefreshFileList()
        {
            _importFilePaths.Clear();
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "HeroHack", "exports");
            if (Directory.Exists(dir))
                _importFilePaths = Directory.GetFiles(dir, "*.xml").OrderByDescending(f => f).ToList();
            _importFileIndex = 0;
            UpdateFileDisplay();
        }

        // ── Display helpers ───────────────────────────────────────────────────

        private void UpdateExportHeroDisplay()
        {
            if (_exportHeroes.Count == 0)
            {
                ExportHeroName  = "— no heroes —";
                ExportCountText = "0 / 0";
            }
            else
            {
                ExportHeroName  = _exportHeroes[_exportHeroIndex].Name?.ToString() ?? "?";
                ExportCountText = $"{_exportHeroIndex + 1} / {_exportHeroes.Count}";
            }
        }

        private void UpdateImportTargetDisplay()
        {
            if (_importTargetHeroes.Count == 0)
            {
                ImportTargetName       = "— no heroes —";
                ImportTargetCountText  = "0 / 0";
            }
            else
            {
                ImportTargetName       = _importTargetHeroes[_importTargetIndex].Name?.ToString() ?? "?";
                ImportTargetCountText  = $"{_importTargetIndex + 1} / {_importTargetHeroes.Count}";
            }
        }

        private void UpdateFileDisplay()
        {
            if (_importFilePaths.Count == 0)
            {
                ImportFileName      = "— no files —";
                ImportFileCountText = "0 / 0";
            }
            else
            {
                // Bug 13: Path.GetFileName — show filename only, not full path
                ImportFileName      = Path.GetFileName(_importFilePaths[_importFileIndex]);
                ImportFileCountText = $"{_importFileIndex + 1} / {_importFilePaths.Count}";
            }
        }

        // ── Execute: Export ───────────────────────────────────────────────────

        public void ExecutePrevExportHero()
        {
            if (_exportHeroes.Count == 0) return;
            _exportHeroIndex = (_exportHeroIndex - 1 + _exportHeroes.Count) % _exportHeroes.Count;
            UpdateExportHeroDisplay();
        }

        public void ExecuteNextExportHero()
        {
            if (_exportHeroes.Count == 0) return;
            _exportHeroIndex = (_exportHeroIndex + 1) % _exportHeroes.Count;
            UpdateExportHeroDisplay();
        }

        public void ExecuteExport()
        {
            try
            {
                if (_exportHeroes.Count == 0) { _onStatusUpdate("No heroes available."); return; }
                string msg = HeroExporter.Export(_exportHeroes[_exportHeroIndex]);
                _onStatusUpdate(msg);
                RefreshFileList(); // new file shows up immediately in import list
            }
            catch (Exception ex) { _onStatusUpdate($"Export error: {ex.Message}"); }
        }

        // ── Execute: File nav ─────────────────────────────────────────────────

        public void ExecuteRefreshFiles()
        {
            RefreshFileList();
            _onStatusUpdate($"Found {_importFilePaths.Count} export file(s).");
        }

        public void ExecutePrevFile()
        {
            if (_importFilePaths.Count == 0) return;
            _importFileIndex = (_importFileIndex - 1 + _importFilePaths.Count) % _importFilePaths.Count;
            UpdateFileDisplay();
            ShowImportConfirm = false;
        }

        public void ExecuteNextFile()
        {
            if (_importFilePaths.Count == 0) return;
            _importFileIndex = (_importFileIndex + 1) % _importFilePaths.Count;
            UpdateFileDisplay();
            ShowImportConfirm = false;
        }

        // ── Execute: Import target nav ────────────────────────────────────────

        public void ExecutePrevImportTarget()
        {
            if (_importTargetHeroes.Count == 0) return;
            _importTargetIndex = (_importTargetIndex - 1 + _importTargetHeroes.Count) % _importTargetHeroes.Count;
            UpdateImportTargetDisplay();
            ShowImportConfirm = false;
        }

        public void ExecuteNextImportTarget()
        {
            if (_importTargetHeroes.Count == 0) return;
            _importTargetIndex = (_importTargetIndex + 1) % _importTargetHeroes.Count;
            UpdateImportTargetDisplay();
            ShowImportConfirm = false;
        }

        // ── Execute: Import (two-step confirm) ────────────────────────────────

        public void ExecuteImport()
        {
            if (_importFilePaths.Count == 0)  { _onStatusUpdate("No file selected."); return; }
            if (_importTargetHeroes.Count == 0) { _onStatusUpdate("No target hero."); return; }

            string file = Path.GetFileName(_importFilePaths[_importFileIndex]);
            string hero = _importTargetHeroes[_importTargetIndex].Name?.ToString() ?? "?";
            ImportConfirmText = $"Apply '{file}' to {hero}? This overwrites stats/equipment. Click Confirm.";
            ShowImportConfirm = true;
        }

        public void ExecuteImportConfirm()
        {
            ShowImportConfirm = false;
            try
            {
                string path    = _importFilePaths[_importFileIndex];
                Hero target    = _importTargetHeroes[_importTargetIndex];
                var result     = HeroImporter.Import(path, target);
                _onStatusUpdate(result.Message);
            }
            catch (Exception ex) { _onStatusUpdate($"Import error: {ex.Message}"); }
        }

        public void ExecuteImportCancel()
        {
            ShowImportConfirm = false;
            ImportConfirmText = string.Empty;
        }
    }
}
