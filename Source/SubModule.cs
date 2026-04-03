using System.Reflection;
using HarmonyLib;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using HeroHack.UI;

namespace HeroHack
{
    public class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "com.herophack.mod";

        private static Harmony _harmony = null!;
        private HeroHackLayer? _heroHackLayer;
        private HeroHackHudLayer? _hudLayer;
        private ScreenBase? _mapScreen;
        private bool _layersInjected;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            InformationManager.DisplayMessage(
                new InformationMessage("HeroHack: Initialized", Color.FromUint(0xFF55FF55)));
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);
            if (game.GameType is Campaign)
            {
                // Scaffold: campaign behaviors can be added here in future phases
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            bool isOnMap = ScreenManager.TopScreen is MapScreen;

            if (isOnMap && !_layersInjected)
            {
                InjectLayers();
            }
            else if (!isOnMap && _layersInjected)
            {
                RemoveLayers();
            }

            if (_layersInjected && Input.IsKeyPressed(InputKey.F10))
            {
                ToggleHeroHackPanel();
            }
        }

        private void InjectLayers()
        {
            if (ScreenManager.TopScreen is not MapScreen) return;

            _mapScreen = ScreenManager.TopScreen;

            _heroHackLayer = new HeroHackLayer();
            _mapScreen.AddLayer(_heroHackLayer);
            // Pass the full toggle (including input restrictions) as the HUD button delegate.
            _hudLayer = new HeroHackHudLayer(ToggleHeroHackPanel);
            _hudLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.Mouse);
            _mapScreen.AddLayer(_hudLayer);

            _layersInjected = true;
        }

        private void ToggleHeroHackPanel()
        {
            if (_heroHackLayer == null) return;
            _heroHackLayer.TogglePanel();
            if (_heroHackLayer.IsOpen)
            {
                _heroHackLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                _heroHackLayer.IsFocusLayer = true;
            }
            else
            {
                _heroHackLayer.InputRestrictions.ResetInputRestrictions();
                _heroHackLayer.IsFocusLayer = false;
            }
        }

        private void RemoveLayers()
        {
            if (_mapScreen != null)
            {
                if (_heroHackLayer != null)
                {
                    _mapScreen.RemoveLayer(_heroHackLayer);
                    _heroHackLayer = null;
                }
                if (_hudLayer != null)
                {
                    _mapScreen.RemoveLayer(_hudLayer);
                    _hudLayer = null;
                }
                _mapScreen = null;
            }
            else
            {
                _heroHackLayer = null;
                _hudLayer = null;
            }
            _layersInjected = false;
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            _harmony?.UnpatchAll(HarmonyId);
            // Layers are cleaned up when screens are destroyed; just null our refs
            _heroHackLayer = null;
            _hudLayer = null;
            _mapScreen = null;
            _layersInjected = false;
        }
    }
}
