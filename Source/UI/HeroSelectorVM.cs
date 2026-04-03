using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace HeroHack.UI
{
    public class HeroSelectorItemVM : ViewModel
    {
        private readonly Action<HeroSelectorItemVM> _onSelect;
        private string _name = string.Empty;
        private bool _isSelected;

        public HeroSelectorItemVM(Hero hero, Action<HeroSelectorItemVM> onSelect)
        {
            Hero = hero;
            _onSelect = onSelect;
            Name = hero.Name.ToString();
        }

        public Hero Hero { get; }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, nameof(Name));
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                }
            }
        }

        public void ExecuteSelect() => _onSelect(this);
    }
}
