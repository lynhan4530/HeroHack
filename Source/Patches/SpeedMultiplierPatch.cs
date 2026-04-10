using System;
using HarmonyLib;
using HeroHack.Cheats;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace HeroHack.Patches
{
    /// <summary>
    /// Postfix patch on the campaign map speed model.
    /// Multiplies the final calculated speed of the player's main party
    /// by the user-controlled SpeedMultiplier flag.
    /// Actual signature: ExplainedNumber CalculateFinalSpeed(MobileParty, ExplainedNumber)
    /// </summary>
    public static class SpeedMultiplierPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            var original = typeof(DefaultPartySpeedCalculatingModel)
                .GetMethod("CalculateFinalSpeed",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var postfix = typeof(SpeedMultiplierPatch)
                .GetMethod(nameof(Postfix),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }

        public static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            try
            {
                if (mobileParty == MobileParty.MainParty && PartyCheats.SpeedMultiplier > 1f)
                {
                    __result.AddFactor(PartyCheats.SpeedMultiplier - 1f, null);
                }
            }
            catch (Exception)
            {
                // Never break the game — fall through
            }
        }
    }
}
