using System;
using HeroHack.Cheats;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HeroHack.UI
{
    public class HeroTabVM : ViewModel
    {
        private readonly Action<string> _onStatusUpdate;
        private HeroSelectorItemVM? _selectedHero;

        // ── Hero selector ────────────────────────────────────────────────────
        private MBBindingList<HeroSelectorItemVM> _availableHeroes = new();

        // ── Resources ────────────────────────────────────────────────────────
        private string _goldText = "0";
        private string _renownText = "0";
        private string _influenceText = "0";
        private string _heroLevelText = "1";

        // ── Attributes ───────────────────────────────────────────────────────
        private string _vigorText = "1";
        private string _controlText = "1";
        private string _enduranceText = "1";
        private string _cunningText = "1";
        private string _socialText = "1";
        private string _intelligenceText = "1";

        // ── Skills ───────────────────────────────────────────────────────────
        private string _skillOneHandedText = "0";
        private string _skillTwoHandedText = "0";
        private string _skillPolearmText = "0";
        private string _skillBowText = "0";
        private string _skillCrossbowText = "0";
        private string _skillThrowingText = "0";
        private string _skillRidingText = "0";
        private string _skillAthleticsText = "0";
        private string _skillCraftingText = "0";
        private string _skillScoutingText = "0";
        private string _skillTacticsText = "0";
        private string _skillRogueryText = "0";
        private string _skillCharmText = "0";
        private string _skillLeadershipText = "0";
        private string _skillTradeText = "0";
        private string _skillStewardText = "0";
        private string _skillMedicineText = "0";
        private string _skillEngineeringText = "0";

        // ── Toggles ──────────────────────────────────────────────────────────
        private bool _isInvulnerable;
        private bool _isImmortal;
        private bool _isPersuasionAutoWin;

        // ── Hero info display ─────────────────────────────────────────────────
        private string _selectedHeroName = "None";
        private int _currentHeroIndex = 0;
        private string _heroCountText = "1 / 1";

        public HeroTabVM(Action<string> onStatusUpdate)
        {
            _onStatusUpdate = onStatusUpdate;
            RefreshHeroList();
        }

        // =====================================================================
        // Hero selection
        // =====================================================================

        [DataSourceProperty]
        public MBBindingList<HeroSelectorItemVM> AvailableHeroes
        {
            get => _availableHeroes;
            set
            {
                if (_availableHeroes != value)
                {
                    _availableHeroes = value;
                    OnPropertyChangedWithValue(value, nameof(AvailableHeroes));
                }
            }
        }

        [DataSourceProperty]
        public string SelectedHeroName
        {
            get => _selectedHeroName;
            set
            {
                if (_selectedHeroName != value)
                {
                    _selectedHeroName = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedHeroName));
                }
            }
        }

        [DataSourceProperty]
        public string HeroCountText
        {
            get => _heroCountText;
            set
            {
                if (_heroCountText != value)
                {
                    _heroCountText = value;
                    OnPropertyChangedWithValue(value, nameof(HeroCountText));
                }
            }
        }

        private void OnHeroSelected(HeroSelectorItemVM item)
        {
            if (_selectedHero != null)
                _selectedHero.IsSelected = false;
            _selectedHero = item;
            _selectedHero.IsSelected = true;
            SelectedHeroName = item.Name;
            RefreshStatsFromHero(item.Hero);
            int idx = _availableHeroes.IndexOf(item);
            if (idx >= 0) _currentHeroIndex = idx;
            HeroCountText = $"{_currentHeroIndex + 1} / {_availableHeroes.Count}";
        }

        public void ExecuteRefreshHeroes()
        {
            RefreshHeroList();
            _onStatusUpdate("Hero list refreshed.");
        }

        public void ExecutePrevHero()
        {
            if (_availableHeroes.Count == 0) return;
            _currentHeroIndex = (_currentHeroIndex - 1 + _availableHeroes.Count) % _availableHeroes.Count;
            OnHeroSelected(_availableHeroes[_currentHeroIndex]);
        }

        public void ExecuteNextHero()
        {
            if (_availableHeroes.Count == 0) return;
            _currentHeroIndex = (_currentHeroIndex + 1) % _availableHeroes.Count;
            OnHeroSelected(_availableHeroes[_currentHeroIndex]);
        }

        private void RefreshHeroList()
        {
            _availableHeroes.Clear();

            if (Campaign.Current == null) return;
            if (Hero.MainHero == null) return;

            var playerItem = new HeroSelectorItemVM(Hero.MainHero, OnHeroSelected);
            _availableHeroes.Add(playerItem);

            if (MobileParty.MainParty?.MemberRoster != null)
            {
                foreach (var element in MobileParty.MainParty.MemberRoster.GetTroopRoster())
                {
                    Hero? companion = element.Character?.HeroObject;
                    if (companion != null && companion != Hero.MainHero
                        && companion.IsActive && companion.IsPlayerCompanion)
                    {
                        _availableHeroes.Add(new HeroSelectorItemVM(companion, OnHeroSelected));
                    }
                }
            }

            // Auto-select player hero
            _currentHeroIndex = 0;
            if (_availableHeroes.Count > 0)
                OnHeroSelected(_availableHeroes[0]);

            OnPropertyChanged(nameof(AvailableHeroes));
        }

        private void RefreshStatsFromHero(Hero hero)
        {
            GoldText = hero.Gold.ToString("N0");
            RenownText = ((int)(hero.Clan?.Renown ?? 0f)).ToString();
            InfluenceText = Clan.PlayerClan != null ? ((int)Clan.PlayerClan.Influence).ToString() : "0";
            HeroLevelText = hero.CharacterObject?.Level.ToString() ?? "1";

            VigorText = hero.GetAttributeValue(DefaultCharacterAttributes.Vigor).ToString();
            ControlText = hero.GetAttributeValue(DefaultCharacterAttributes.Control).ToString();
            EnduranceText = hero.GetAttributeValue(DefaultCharacterAttributes.Endurance).ToString();
            CunningText = hero.GetAttributeValue(DefaultCharacterAttributes.Cunning).ToString();
            SocialText = hero.GetAttributeValue(DefaultCharacterAttributes.Social).ToString();
            IntelligenceText = hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence).ToString();

            SkillOneHandedText = hero.GetSkillValue(DefaultSkills.OneHanded).ToString();
            SkillTwoHandedText = hero.GetSkillValue(DefaultSkills.TwoHanded).ToString();
            SkillPolearmText = hero.GetSkillValue(DefaultSkills.Polearm).ToString();
            SkillBowText = hero.GetSkillValue(DefaultSkills.Bow).ToString();
            SkillCrossbowText = hero.GetSkillValue(DefaultSkills.Crossbow).ToString();
            SkillThrowingText = hero.GetSkillValue(DefaultSkills.Throwing).ToString();
            SkillRidingText = hero.GetSkillValue(DefaultSkills.Riding).ToString();
            SkillAthleticsText = hero.GetSkillValue(DefaultSkills.Athletics).ToString();
            SkillCraftingText = hero.GetSkillValue(DefaultSkills.Crafting).ToString();
            SkillScoutingText = hero.GetSkillValue(DefaultSkills.Scouting).ToString();
            SkillTacticsText = hero.GetSkillValue(DefaultSkills.Tactics).ToString();
            SkillRogueryText = hero.GetSkillValue(DefaultSkills.Roguery).ToString();
            SkillCharmText = hero.GetSkillValue(DefaultSkills.Charm).ToString();
            SkillLeadershipText = hero.GetSkillValue(DefaultSkills.Leadership).ToString();
            SkillTradeText = hero.GetSkillValue(DefaultSkills.Trade).ToString();
            SkillStewardText = hero.GetSkillValue(DefaultSkills.Steward).ToString();
            SkillMedicineText = hero.GetSkillValue(DefaultSkills.Medicine).ToString();
            SkillEngineeringText = hero.GetSkillValue(DefaultSkills.Engineering).ToString();
        }

        // =====================================================================
        // Resources
        // =====================================================================

        [DataSourceProperty]
        public string GoldText
        {
            get => _goldText;
            set { if (_goldText != value) { _goldText = value; OnPropertyChangedWithValue(value, nameof(GoldText)); } }
        }

        [DataSourceProperty]
        public string RenownText
        {
            get => _renownText;
            set { if (_renownText != value) { _renownText = value; OnPropertyChangedWithValue(value, nameof(RenownText)); } }
        }

        [DataSourceProperty]
        public string InfluenceText
        {
            get => _influenceText;
            set { if (_influenceText != value) { _influenceText = value; OnPropertyChangedWithValue(value, nameof(InfluenceText)); } }
        }

        [DataSourceProperty]
        public string HeroLevelText
        {
            get => _heroLevelText;
            set { if (_heroLevelText != value) { _heroLevelText = value; OnPropertyChangedWithValue(value, nameof(HeroLevelText)); } }
        }

        public void ExecuteApplyGold()
        {
            Hero? hero = _selectedHero?.Hero;
            if (hero == null) { _onStatusUpdate("No hero selected."); return; }
            if (!int.TryParse(_goldText.Replace(",", ""), out int val)) { _onStatusUpdate("Invalid gold value."); return; }
            val = Math.Max(0, Math.Min(9_999_999, val));
            HeroCheats.SetGold(hero, val);
            _onStatusUpdate($"Gold set to {val:N0} for {hero.Name}.");
            GoldText = hero.Gold.ToString("N0");
        }

        public void ExecuteApplyRenown()
        {
            Hero? hero = _selectedHero?.Hero;
            if (hero == null) { _onStatusUpdate("No hero selected."); return; }
            if (!float.TryParse(_renownText, out float val)) { _onStatusUpdate("Invalid renown value."); return; }
            val = Math.Max(0, Math.Min(10_000f, val));
            HeroCheats.SetRenown(hero, val);
            _onStatusUpdate($"Renown set to {val:N0} for {hero.Name}.");
        }

        public void ExecuteApplyInfluence()
        {
            if (!float.TryParse(_influenceText, out float val)) { _onStatusUpdate("Invalid influence value."); return; }
            val = Math.Max(0, Math.Min(10_000f, val));
            HeroCheats.SetInfluence(val);
            _onStatusUpdate($"Influence set to {val:N0}.");
        }

        public void ExecuteApplyLevel()
        {
            Hero? hero = _selectedHero?.Hero;
            if (hero == null) { _onStatusUpdate("No hero selected."); return; }
            if (!int.TryParse(_heroLevelText, out int val)) { _onStatusUpdate("Invalid level value."); return; }
            val = Math.Max(1, Math.Min(62, val));
            HeroCheats.SetLevel(hero, val);
            _onStatusUpdate($"Level set to {val} for {hero.Name}.");
        }

        // =====================================================================
        // Attributes
        // =====================================================================

        [DataSourceProperty] public string VigorText { get => _vigorText; set { if (_vigorText != value) { _vigorText = value; OnPropertyChangedWithValue(value, nameof(VigorText)); } } }
        [DataSourceProperty] public string ControlText { get => _controlText; set { if (_controlText != value) { _controlText = value; OnPropertyChangedWithValue(value, nameof(ControlText)); } } }
        [DataSourceProperty] public string EnduranceText { get => _enduranceText; set { if (_enduranceText != value) { _enduranceText = value; OnPropertyChangedWithValue(value, nameof(EnduranceText)); } } }
        [DataSourceProperty] public string CunningText { get => _cunningText; set { if (_cunningText != value) { _cunningText = value; OnPropertyChangedWithValue(value, nameof(CunningText)); } } }
        [DataSourceProperty] public string SocialText { get => _socialText; set { if (_socialText != value) { _socialText = value; OnPropertyChangedWithValue(value, nameof(SocialText)); } } }
        [DataSourceProperty] public string IntelligenceText { get => _intelligenceText; set { if (_intelligenceText != value) { _intelligenceText = value; OnPropertyChangedWithValue(value, nameof(IntelligenceText)); } } }

        public void ExecuteApplyAttributes()
        {
            Hero? hero = _selectedHero?.Hero;
            if (hero == null) { _onStatusUpdate("No hero selected."); return; }

            bool ok = true;
            ok &= TrySetAttr(hero, DefaultCharacterAttributes.Vigor, _vigorText);
            ok &= TrySetAttr(hero, DefaultCharacterAttributes.Control, _controlText);
            ok &= TrySetAttr(hero, DefaultCharacterAttributes.Endurance, _enduranceText);
            ok &= TrySetAttr(hero, DefaultCharacterAttributes.Cunning, _cunningText);
            ok &= TrySetAttr(hero, DefaultCharacterAttributes.Social, _socialText);
            ok &= TrySetAttr(hero, DefaultCharacterAttributes.Intelligence, _intelligenceText);

            _onStatusUpdate(ok
                ? $"Attributes applied to {hero.Name}."
                : "Some attribute values were invalid — check inputs.");
        }

        private bool TrySetAttr(Hero hero, CharacterAttribute attr, string text)
        {
            if (!int.TryParse(text, out int val)) return false;
            val = Math.Max(1, Math.Min(10, val));
            HeroCheats.SetAttribute(hero, attr, val);
            return true;
        }

        public void ExecuteAddAttributePoint()
        {
            Hero? hero = _selectedHero?.Hero;
            if (hero == null) { _onStatusUpdate("No hero selected."); return; }
            HeroCheats.AddAttributePoint(hero);
            _onStatusUpdate($"+1 attribute point granted to {hero.Name}.");
        }

        public void ExecuteAddFocusPoint()
        {
            Hero? hero = _selectedHero?.Hero;
            if (hero == null) { _onStatusUpdate("No hero selected."); return; }
            HeroCheats.AddFocusPoint(hero);
            _onStatusUpdate($"+1 focus point granted to {hero.Name}.");
        }

        // =====================================================================
        // Skills
        // =====================================================================

        [DataSourceProperty] public string SkillOneHandedText { get => _skillOneHandedText; set { if (_skillOneHandedText != value) { _skillOneHandedText = value; OnPropertyChangedWithValue(value, nameof(SkillOneHandedText)); } } }
        [DataSourceProperty] public string SkillTwoHandedText { get => _skillTwoHandedText; set { if (_skillTwoHandedText != value) { _skillTwoHandedText = value; OnPropertyChangedWithValue(value, nameof(SkillTwoHandedText)); } } }
        [DataSourceProperty] public string SkillPolearmText { get => _skillPolearmText; set { if (_skillPolearmText != value) { _skillPolearmText = value; OnPropertyChangedWithValue(value, nameof(SkillPolearmText)); } } }
        [DataSourceProperty] public string SkillBowText { get => _skillBowText; set { if (_skillBowText != value) { _skillBowText = value; OnPropertyChangedWithValue(value, nameof(SkillBowText)); } } }
        [DataSourceProperty] public string SkillCrossbowText { get => _skillCrossbowText; set { if (_skillCrossbowText != value) { _skillCrossbowText = value; OnPropertyChangedWithValue(value, nameof(SkillCrossbowText)); } } }
        [DataSourceProperty] public string SkillThrowingText { get => _skillThrowingText; set { if (_skillThrowingText != value) { _skillThrowingText = value; OnPropertyChangedWithValue(value, nameof(SkillThrowingText)); } } }
        [DataSourceProperty] public string SkillRidingText { get => _skillRidingText; set { if (_skillRidingText != value) { _skillRidingText = value; OnPropertyChangedWithValue(value, nameof(SkillRidingText)); } } }
        [DataSourceProperty] public string SkillAthleticsText { get => _skillAthleticsText; set { if (_skillAthleticsText != value) { _skillAthleticsText = value; OnPropertyChangedWithValue(value, nameof(SkillAthleticsText)); } } }
        [DataSourceProperty] public string SkillCraftingText { get => _skillCraftingText; set { if (_skillCraftingText != value) { _skillCraftingText = value; OnPropertyChangedWithValue(value, nameof(SkillCraftingText)); } } }
        [DataSourceProperty] public string SkillScoutingText { get => _skillScoutingText; set { if (_skillScoutingText != value) { _skillScoutingText = value; OnPropertyChangedWithValue(value, nameof(SkillScoutingText)); } } }
        [DataSourceProperty] public string SkillTacticsText { get => _skillTacticsText; set { if (_skillTacticsText != value) { _skillTacticsText = value; OnPropertyChangedWithValue(value, nameof(SkillTacticsText)); } } }
        [DataSourceProperty] public string SkillRogueryText { get => _skillRogueryText; set { if (_skillRogueryText != value) { _skillRogueryText = value; OnPropertyChangedWithValue(value, nameof(SkillRogueryText)); } } }
        [DataSourceProperty] public string SkillCharmText { get => _skillCharmText; set { if (_skillCharmText != value) { _skillCharmText = value; OnPropertyChangedWithValue(value, nameof(SkillCharmText)); } } }
        [DataSourceProperty] public string SkillLeadershipText { get => _skillLeadershipText; set { if (_skillLeadershipText != value) { _skillLeadershipText = value; OnPropertyChangedWithValue(value, nameof(SkillLeadershipText)); } } }
        [DataSourceProperty] public string SkillTradeText { get => _skillTradeText; set { if (_skillTradeText != value) { _skillTradeText = value; OnPropertyChangedWithValue(value, nameof(SkillTradeText)); } } }
        [DataSourceProperty] public string SkillStewardText { get => _skillStewardText; set { if (_skillStewardText != value) { _skillStewardText = value; OnPropertyChangedWithValue(value, nameof(SkillStewardText)); } } }
        [DataSourceProperty] public string SkillMedicineText { get => _skillMedicineText; set { if (_skillMedicineText != value) { _skillMedicineText = value; OnPropertyChangedWithValue(value, nameof(SkillMedicineText)); } } }
        [DataSourceProperty] public string SkillEngineeringText { get => _skillEngineeringText; set { if (_skillEngineeringText != value) { _skillEngineeringText = value; OnPropertyChangedWithValue(value, nameof(SkillEngineeringText)); } } }

        public void ExecuteApplySkills()
        {
            Hero? hero = _selectedHero?.Hero;
            if (hero == null) { _onStatusUpdate("No hero selected."); return; }

            bool ok = true;
            ok &= TrySetSkill(hero, DefaultSkills.OneHanded, _skillOneHandedText);
            ok &= TrySetSkill(hero, DefaultSkills.TwoHanded, _skillTwoHandedText);
            ok &= TrySetSkill(hero, DefaultSkills.Polearm, _skillPolearmText);
            ok &= TrySetSkill(hero, DefaultSkills.Bow, _skillBowText);
            ok &= TrySetSkill(hero, DefaultSkills.Crossbow, _skillCrossbowText);
            ok &= TrySetSkill(hero, DefaultSkills.Throwing, _skillThrowingText);
            ok &= TrySetSkill(hero, DefaultSkills.Riding, _skillRidingText);
            ok &= TrySetSkill(hero, DefaultSkills.Athletics, _skillAthleticsText);
            ok &= TrySetSkill(hero, DefaultSkills.Crafting, _skillCraftingText);
            ok &= TrySetSkill(hero, DefaultSkills.Scouting, _skillScoutingText);
            ok &= TrySetSkill(hero, DefaultSkills.Tactics, _skillTacticsText);
            ok &= TrySetSkill(hero, DefaultSkills.Roguery, _skillRogueryText);
            ok &= TrySetSkill(hero, DefaultSkills.Charm, _skillCharmText);
            ok &= TrySetSkill(hero, DefaultSkills.Leadership, _skillLeadershipText);
            ok &= TrySetSkill(hero, DefaultSkills.Trade, _skillTradeText);
            ok &= TrySetSkill(hero, DefaultSkills.Steward, _skillStewardText);
            ok &= TrySetSkill(hero, DefaultSkills.Medicine, _skillMedicineText);
            ok &= TrySetSkill(hero, DefaultSkills.Engineering, _skillEngineeringText);

            _onStatusUpdate(ok
                ? $"Skills applied to {hero.Name}."
                : "Some skill values were invalid — check inputs.");
        }

        private bool TrySetSkill(Hero hero, SkillObject skill, string text)
        {
            if (!int.TryParse(text, out int val)) return false;
            HeroCheats.SetSkill(hero, skill, val);
            return true;
        }

        // =====================================================================
        // Toggles (CombatCheats — patches active in Phase 3)
        // =====================================================================

        [DataSourceProperty]
        public bool IsInvulnerable
        {
            get => _isInvulnerable;
            set
            {
                if (_isInvulnerable != value)
                {
                    _isInvulnerable = value;
                    OnPropertyChangedWithValue(value, nameof(IsInvulnerable));
                }
            }
        }

        [DataSourceProperty]
        public bool IsImmortal
        {
            get => _isImmortal;
            set
            {
                if (_isImmortal != value)
                {
                    _isImmortal = value;
                    OnPropertyChangedWithValue(value, nameof(IsImmortal));
                }
            }
        }

        [DataSourceProperty]
        public bool IsPersuasionAutoWin
        {
            get => _isPersuasionAutoWin;
            set
            {
                if (_isPersuasionAutoWin != value)
                {
                    _isPersuasionAutoWin = value;
                    OnPropertyChangedWithValue(value, nameof(IsPersuasionAutoWin));
                }
            }
        }

        public void ExecuteToggleInvulnerable()
        {
            IsInvulnerable = !IsInvulnerable;
            CombatCheats.IsPlayerInvulnerable = IsInvulnerable;
            _onStatusUpdate($"Invulnerable: {(IsInvulnerable ? "ON" : "OFF")}");
        }

        public void ExecuteToggleImmortal()
        {
            IsImmortal = !IsImmortal;
            CombatCheats.IsPlayerImmortal = IsImmortal;
            _onStatusUpdate($"Immortal: {(IsImmortal ? "ON" : "OFF")}");
        }

        public void ExecuteTogglePersuasion()
        {
            IsPersuasionAutoWin = !IsPersuasionAutoWin;
            CombatCheats.IsPersuasionAutoWin = IsPersuasionAutoWin;
            _onStatusUpdate($"Persuasion Auto-Win: {(IsPersuasionAutoWin ? "ON" : "OFF")}");
        }
    }
}
