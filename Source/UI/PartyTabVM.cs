using System;
using System.Linq;
using System.Collections.Generic;
using HeroHack.Cheats;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace HeroHack.UI
{
    public class PartyTabVM : ViewModel
    {
        private readonly Action<string> _onStatusUpdate;

        // Display fields — Bug 10: no game API calls in constructor
        private string _moraleText   = "--";
        private string _foodText     = "--";
        private string _woundedText  = "--";
        private string _partySizeText = "--";
        private string _addTroopCountText = "50";
        private string _addFoodCountText  = "200";
        private string _addEliteTroopCountText = "50";
        private List<CharacterObject> _eliteTroops = new List<CharacterObject>();
        private int _eliteTroopIndex = 0;
        private string _eliteTroopName = "—";

        private List<string> _factions = new List<string> { "Any", "Empire", "Vlandia", "Battania", "Khuzait", "Aserai", "Sturgia" };
        private List<string> _classes = new List<string> { "Any", "Infantry", "Ranged", "Cavalry", "Horse Archer" };
        private List<string> _tiers = new List<string> { "Any", "Tier 2", "Tier 3", "Tier 4", "Tier 5", "Tier 6" };
        private int _factionFilterIndex = 0;
        private int _classFilterIndex = 0;
        private int _tierFilterIndex = 0;
        
        private List<string> _basicCultures = new List<string> { "Player", "Empire", "Vlandia", "Battania", "Khuzait", "Aserai", "Sturgia" };
        private int _basicCultureIndex = 0;
        
        public PartyTabVM(Action<string> onStatusUpdate)
        {
            _onStatusUpdate = onStatusUpdate;
            // Intentionally empty — display refreshed via RefreshDisplay()
        }

        // ── Display properties ─────────────────────────────────────────────

        [DataSourceProperty]
        public string MoraleText
        {
            get => _moraleText;
            set { if (_moraleText != value) { _moraleText = value; OnPropertyChangedWithValue(value, nameof(MoraleText)); } }
        }

        [DataSourceProperty]
        public string FoodText
        {
            get => _foodText;
            set { if (_foodText != value) { _foodText = value; OnPropertyChangedWithValue(value, nameof(FoodText)); } }
        }

        [DataSourceProperty]
        public string WoundedText
        {
            get => _woundedText;
            set { if (_woundedText != value) { _woundedText = value; OnPropertyChangedWithValue(value, nameof(WoundedText)); } }
        }

        [DataSourceProperty]
        public string PartySizeText
        {
            get => _partySizeText;
            set { if (_partySizeText != value) { _partySizeText = value; OnPropertyChangedWithValue(value, nameof(PartySizeText)); } }
        }

        [DataSourceProperty]
        public string AddTroopCountText
        {
            get => _addTroopCountText;
            set { if (_addTroopCountText != value) { _addTroopCountText = value; OnPropertyChangedWithValue(value, nameof(AddTroopCountText)); } }
        }

        [DataSourceProperty]
        public string AddFoodCountText
        {
            get => _addFoodCountText;
            set { if (_addFoodCountText != value) { _addFoodCountText = value; OnPropertyChangedWithValue(value, nameof(AddFoodCountText)); } }
        }

        [DataSourceProperty]
        public string AddEliteTroopCountText
        {
            get => _addEliteTroopCountText;
            set { if (_addEliteTroopCountText != value) { _addEliteTroopCountText = value; OnPropertyChangedWithValue(value, nameof(AddEliteTroopCountText)); } }
        }

        [DataSourceProperty]
        public string EliteTroopName
        {
            get => _eliteTroopName;
            set { if (_eliteTroopName != value) { _eliteTroopName = value; OnPropertyChangedWithValue(value, nameof(EliteTroopName)); } }
        }

        [DataSourceProperty] public string FactionFilterText => _factions[_factionFilterIndex];
        [DataSourceProperty] public string ClassFilterText => _classes[_classFilterIndex];
        [DataSourceProperty] public string TierFilterText => _tiers[_tierFilterIndex];

        // ── Refresh ────────────────────────────────────────────────────────

        // Bug 9: called from HeroHackPanelVM.ExecuteSelectTab1() so display is always fresh
        public void RefreshDisplay()
        {
            RefreshEliteTroops();
            try
            {
                var party = MobileParty.MainParty;
                if (party == null)
                {
                    MoraleText = PartySizeText = FoodText = WoundedText = "--";
                    return;
                }
                MoraleText    = ((int)party.Morale).ToString();
                FoodText      = ((int)party.Food).ToString();
                WoundedText   = party.MemberRoster.TotalWounded.ToString();
                PartySizeText = party.MemberRoster.TotalManCount.ToString();
            }
            catch (Exception ex)
            {
                _onStatusUpdate($"Refresh error: {ex.Message}");
            }
        }

        // ── Execute methods ────────────────────────────────────────────────

        public void ExecuteBoostMorale()
        {
            try { _onStatusUpdate(PartyCheats.BoostMorale()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteAddFood()
        {
            try
            {
                if (!int.TryParse(_addFoodCountText, out int amt) || amt <= 0) amt = 200;
                _onStatusUpdate(PartyCheats.AddFood(amt));
                RefreshDisplay();
            }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteHealAll()
        {
            try { _onStatusUpdate(PartyCheats.HealAllWounded()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        [DataSourceProperty]
        public string BasicCultureText => _basicCultures[_basicCultureIndex];

        public void ExecutePrevBasicCulture() { _basicCultureIndex = (_basicCultureIndex - 1 + _basicCultures.Count) % _basicCultures.Count; OnPropertyChanged("BasicCultureText"); }
        public void ExecuteNextBasicCulture() { _basicCultureIndex = (_basicCultureIndex + 1) % _basicCultures.Count; OnPropertyChanged("BasicCultureText"); }

        // Culture selection implemented
        public void ExecuteAddTroops()
        {
            try
            {
                if (!int.TryParse(_addTroopCountText, out int amt) || amt <= 0) amt = 50;
                string selectedCulture = _basicCultures[_basicCultureIndex];
                string targetCulture = selectedCulture == "Player" ? null : selectedCulture;
                _onStatusUpdate(PartyCheats.AddTroops(amt, targetCulture));
                RefreshDisplay();
            }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecutePrevFactionFilter() { _factionFilterIndex = (_factionFilterIndex - 1 + _factions.Count) % _factions.Count; OnPropertyChanged("FactionFilterText"); RefreshEliteTroops(true); }
        public void ExecuteNextFactionFilter() { _factionFilterIndex = (_factionFilterIndex + 1) % _factions.Count; OnPropertyChanged("FactionFilterText"); RefreshEliteTroops(true); }

        public void ExecutePrevClassFilter() { _classFilterIndex = (_classFilterIndex - 1 + _classes.Count) % _classes.Count; OnPropertyChanged("ClassFilterText"); RefreshEliteTroops(true); }
        public void ExecuteNextClassFilter() { _classFilterIndex = (_classFilterIndex + 1) % _classes.Count; OnPropertyChanged("ClassFilterText"); RefreshEliteTroops(true); }

        public void ExecutePrevTierFilter() { _tierFilterIndex = (_tierFilterIndex - 1 + _tiers.Count) % _tiers.Count; OnPropertyChanged("TierFilterText"); RefreshEliteTroops(true); }
        public void ExecuteNextTierFilter() { _tierFilterIndex = (_tierFilterIndex + 1) % _tiers.Count; OnPropertyChanged("TierFilterText"); RefreshEliteTroops(true); }

        public void ExecutePrevEliteTroop()
        {
            if (_eliteTroops.Count <= 0) return;
            _eliteTroopIndex = (_eliteTroopIndex - 1 + _eliteTroops.Count) % _eliteTroops.Count;
            EliteTroopName = _eliteTroops[_eliteTroopIndex].Name?.ToString() ?? "?";
        }

        public void ExecuteNextEliteTroop()
        {
            if (_eliteTroops.Count <= 0) return;
            _eliteTroopIndex = (_eliteTroopIndex + 1) % _eliteTroops.Count;
            EliteTroopName = _eliteTroops[_eliteTroopIndex].Name?.ToString() ?? "?";
        }

        private void RefreshEliteTroops(bool force = false)
        {
            if (!force && _eliteTroops.Count > 0) return; 
            if (TaleWorlds.Core.Game.Current?.ObjectManager == null) return;

            var allChars = TaleWorlds.Core.Game.Current.ObjectManager.GetObjectTypeList<CharacterObject>();
            if (allChars == null) return;

            string fac = _factions[_factionFilterIndex].ToLower();
            string cls = _classes[_classFilterIndex].ToLower();
            string tierStr = _tiers[_tierFilterIndex].ToLower();

            _eliteTroops.Clear();

            foreach (var t in allChars)
            {
                if (!t.IsSoldier || t.IsHero) continue;

                // Faction check
                if (fac != "any" && t.Culture?.StringId?.ToLower() != fac) continue;

                // Tier check
                int targetTier = -1;
                if (tierStr != "any" && int.TryParse(tierStr.Replace("tier ", ""), out targetTier))
                {
                    if (t.Tier != targetTier) continue;
                }

                // Class check
                if (cls != "any")
                {
                    bool isCav = t.HasMount();
                    bool isRanged = t.IsRanged;
                    
                    if (cls == "infantry" && (isCav || isRanged)) continue;
                    if (cls == "ranged" && (isCav || !isRanged)) continue;
                    if (cls == "cavalry" && (!isCav || isRanged)) continue;
                    if (cls == "horse archer" && (!isCav || !isRanged)) continue;
                }

                _eliteTroops.Add(t);
            }

            _eliteTroops = _eliteTroops
                .OrderByDescending(t => t.Tier)
                .ThenBy(t => t.Culture?.Name?.ToString() ?? "")
                .ThenBy(t => t.Name?.ToString() ?? "")
                .ToList();

            if (_eliteTroops.Count > 0)
            {
                _eliteTroopIndex = 0;
                EliteTroopName = _eliteTroops[_eliteTroopIndex].Name?.ToString() ?? "?";
            }
            else
            {
                EliteTroopName = "No Match Found";
            }
        }

        public void ExecuteAddEliteTroops()
        {
            try
            {
                if (!int.TryParse(_addEliteTroopCountText, out int amt) || amt <= 0) amt = 50;
                CharacterObject target = _eliteTroops.Count > 0 ? _eliteTroops[_eliteTroopIndex] : null;
                _onStatusUpdate(PartyCheats.AddEliteTroops(amt, target));
                RefreshDisplay();
            }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteProvideUpgradeMounts()
        {
            try
            {
                _onStatusUpdate(PartyCheats.ProvideUpgradeMounts());
                RefreshDisplay();
            }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteAutoPromote()
        {
            try { _onStatusUpdate(PartyCheats.AutoPromote()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteRecruitPrisoners()
        {
            try { _onStatusUpdate(PartyCheats.RecruitPrisoners()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteMountHoarder()
        {
            try { _onStatusUpdate(PartyCheats.MountHoarder()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }
    }
}
