using TaleWorlds.Library;

namespace HeroHack.UI
{
    public class HeroHackPanelVM : ViewModel
    {
        private bool _isOpen;
        private int _activeTabIndex;
        private string _statusMessage = string.Empty;
        private float _statusTimer;
        private HeroTabVM _heroTab = null!;
        private PartyTabVM _partyTab = null!;          // Bug 8
        private SettlementTabVM _settlementTab = null!; // Bug 8
        private IOTabVM _ioTab = null!;
        public HeroHackPanelVM()
        {
            _activeTabIndex = 0;
            HeroTab = new HeroTabVM(SetStatus);
            PartyTab = new PartyTabVM(SetStatus);           // Bug 8, 10, 11
            SettlementTab = new SettlementTabVM(SetStatus); // Bug 8, 10, 11
            IOTab = new IOTabVM(SetStatus);                 // Bug 6: empty ctor, data loaded on tab select
        }

        [DataSourceProperty]
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen != value)
                {
                    _isOpen = value;
                    OnPropertyChangedWithValue(value, nameof(IsOpen));
                }
            }
        }

        [DataSourceProperty]
        public int ActiveTabIndex
        {
            get => _activeTabIndex;
            set
            {
                if (_activeTabIndex != value)
                {
                    _activeTabIndex = value;
                    OnPropertyChangedWithValue(value, nameof(ActiveTabIndex));
                    OnPropertyChanged(nameof(IsTab0Active));
                    OnPropertyChanged(nameof(IsTab1Active));
                    OnPropertyChanged(nameof(IsTab2Active));
                    OnPropertyChanged(nameof(IsTab3Active));
                }
            }
        }

        [DataSourceProperty]
        public bool IsTab0Active => _activeTabIndex == 0;

        [DataSourceProperty]
        public bool IsTab1Active => _activeTabIndex == 1;

        [DataSourceProperty]
        public bool IsTab2Active => _activeTabIndex == 2;

        [DataSourceProperty]
        public bool IsTab3Active => _activeTabIndex == 3;

        [DataSourceProperty]
        public HeroTabVM HeroTab
        {
            get => _heroTab;
            set
            {
                if (_heroTab != value)
                {
                    _heroTab = value;
                    OnPropertyChangedWithValue(value, nameof(HeroTab));
                }
            }
        }

        [DataSourceProperty]
        public PartyTabVM PartyTab
        {
            get => _partyTab;
            set
            {
                if (_partyTab != value)
                {
                    _partyTab = value;
                    OnPropertyChangedWithValue(value, nameof(PartyTab));
                }
            }
        }

        [DataSourceProperty]
        public SettlementTabVM SettlementTab
        {
            get => _settlementTab;
            set
            {
                if (_settlementTab != value)
                {
                    _settlementTab = value;
                    OnPropertyChangedWithValue(value, nameof(SettlementTab));
                }
            }
        }

        [DataSourceProperty]
        public IOTabVM IOTab
        {
            get => _ioTab;
            set
            {
                if (_ioTab != value)
                {
                    _ioTab = value;
                    OnPropertyChangedWithValue(value, nameof(IOTab));
                }
            }
        }

        [DataSourceProperty]
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChangedWithValue(value, nameof(StatusMessage));
                }
            }
        }

        public void SetStatus(string msg)
        {
            StatusMessage = msg;
            _statusTimer = 3f;
        }

        public void Tick(float dt)
        {
            if (_statusTimer > 0f)
            {
                _statusTimer -= dt;
                if (_statusTimer <= 0f)
                {
                    _statusTimer = 0f;
                    StatusMessage = string.Empty;
                }
            }
        }

        public void ExecuteClose()
        {
            IsOpen = false;
        }

        public void ExecuteSelectTab0() => ActiveTabIndex = 0;

        public void ExecuteSelectTab1() { ActiveTabIndex = 1; PartyTab.RefreshDisplay(); }      // Bug 9

        public void ExecuteSelectTab2() { ActiveTabIndex = 2; SettlementTab.RefreshDisplay(); } // Bug 9

        public void ExecuteSelectTab3() { ActiveTabIndex = 3; IOTab.RefreshDisplay(); }
    }
}
