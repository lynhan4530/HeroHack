using System;
using HeroHack.Cheats;
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

        // ── Refresh ────────────────────────────────────────────────────────

        // Bug 9: called from HeroHackPanelVM.ExecuteSelectTab1() so display is always fresh
        public void RefreshDisplay()
        {
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

        public void ExecuteAddTroops()
        {
            try
            {
                if (!int.TryParse(_addTroopCountText, out int amt) || amt <= 0) amt = 50;
                _onStatusUpdate(PartyCheats.AddTroops(amt));
                RefreshDisplay();
            }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }
    }
}
