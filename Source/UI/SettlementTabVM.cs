using System;
using HeroHack.Cheats;
using TaleWorlds.Library;

namespace HeroHack.UI
{
    public class SettlementTabVM : ViewModel
    {
        private readonly Action<string> _onStatusUpdate;

        // Display fields — Bug 10: no game API in constructor
        private string _settlementName   = "—";
        private string _prosperityText   = "—";
        private string _loyaltyText      = "—";
        private string _securityText     = "—";
        private string _foodText         = "—";
        private string _garrisonText     = "—";
        private string _setProsperityText = "8000";
        private string _addGarrisonText   = "200";

        public SettlementTabVM(Action<string> onStatusUpdate)
        {
            _onStatusUpdate = onStatusUpdate;
            // Bug 10: intentionally no game API here
        }

        // ── Display properties ─────────────────────────────────────────────

        [DataSourceProperty]
        public string SettlementName
        {
            get => _settlementName;
            set { if (_settlementName != value) { _settlementName = value; OnPropertyChangedWithValue(value, nameof(SettlementName)); } }
        }

        [DataSourceProperty]
        public string ProsperityText
        {
            get => _prosperityText;
            set { if (_prosperityText != value) { _prosperityText = value; OnPropertyChangedWithValue(value, nameof(ProsperityText)); } }
        }

        [DataSourceProperty]
        public string LoyaltyText
        {
            get => _loyaltyText;
            set { if (_loyaltyText != value) { _loyaltyText = value; OnPropertyChangedWithValue(value, nameof(LoyaltyText)); } }
        }

        [DataSourceProperty]
        public string SecurityText
        {
            get => _securityText;
            set { if (_securityText != value) { _securityText = value; OnPropertyChangedWithValue(value, nameof(SecurityText)); } }
        }

        [DataSourceProperty]
        public string FoodText
        {
            get => _foodText;
            set { if (_foodText != value) { _foodText = value; OnPropertyChangedWithValue(value, nameof(FoodText)); } }
        }

        [DataSourceProperty]
        public string GarrisonText
        {
            get => _garrisonText;
            set { if (_garrisonText != value) { _garrisonText = value; OnPropertyChangedWithValue(value, nameof(GarrisonText)); } }
        }

        [DataSourceProperty]
        public string SetProsperityText
        {
            get => _setProsperityText;
            set { if (_setProsperityText != value) { _setProsperityText = value; OnPropertyChangedWithValue(value, nameof(SetProsperityText)); } }
        }

        [DataSourceProperty]
        public string AddGarrisonText
        {
            get => _addGarrisonText;
            set { if (_addGarrisonText != value) { _addGarrisonText = value; OnPropertyChangedWithValue(value, nameof(AddGarrisonText)); } }
        }

        // ── Refresh ────────────────────────────────────────────────────────

        // Bug 9: called from HeroHackPanelVM.ExecuteSelectTab2()
        public void RefreshDisplay()
        {
            try
            {
                var (name, prosperity, loyalty, security, food, garrison) = SettlementCheats.GetSnapshot();
                SettlementName = name;
                ProsperityText = prosperity;
                LoyaltyText    = loyalty;
                SecurityText   = security;
                FoodText       = food;
                GarrisonText   = garrison;
            }
            catch (Exception ex)
            {
                _onStatusUpdate($"Refresh error: {ex.Message}");
            }
        }

        // ── Execute methods ────────────────────────────────────────────────

        public void ExecuteMaxProsperity()
        {
            try { _onStatusUpdate(SettlementCheats.MaxProsperity()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteSetProsperity()
        {
            try
            {
                if (!int.TryParse(_setProsperityText, out int val) || val < 0) val = 8000;
                _onStatusUpdate(SettlementCheats.SetProsperity(val));
                RefreshDisplay();
            }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteMaxLoyalty()
        {
            try { _onStatusUpdate(SettlementCheats.MaxLoyalty()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteMaxSecurity()
        {
            try { _onStatusUpdate(SettlementCheats.MaxSecurity()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteFillGarrison()
        {
            try
            {
                if (!int.TryParse(_addGarrisonText, out int amt) || amt <= 0) amt = 200;
                _onStatusUpdate(SettlementCheats.FillGarrison(amt));
                RefreshDisplay();
            }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }

        public void ExecuteMaxFoodStocks()
        {
            try { _onStatusUpdate(SettlementCheats.MaxFoodStocks()); RefreshDisplay(); }
            catch (Exception ex) { _onStatusUpdate($"Error: {ex.Message}"); }
        }
    }
}
