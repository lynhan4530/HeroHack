using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HeroHack
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            InformationManager.DisplayMessage(new InformationMessage("HeroHack: Initialized", Color.FromUint(0xFF55FF55)));
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InformationManager.DisplayMessage(new InformationMessage("HeroHack: Basic Hero Hack Mod Ready", Color.FromUint(0xFF55FF55)));
        }
    }
}
