using System;
using HarmonyLib;
using HeroHack.Cheats;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace HeroHack.Patches
{
    /// <summary>
    /// Prevents the player hero from being killed on the campaign map
    /// when Immortal is toggled ON. This covers executions, battle aftermath,
    /// and scripted kills via KillCharacterAction.
    /// </summary>
    [HarmonyPatch(typeof(KillCharacterAction))]
    public static class HeroDeathPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ApplyInternal")]
        public static bool Prefix(Hero victim)
        {
            try
            {
                if (victim != null
                    && victim == Hero.MainHero
                    && CombatCheats.IsPlayerImmortal)
                {
                    return false; // skip the kill entirely
                }
            }
            catch (Exception)
            {
                // Never break the game — fall through to original method
            }

            return true;
        }
    }
}
