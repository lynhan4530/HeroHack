using System;
using HarmonyLib;
using HeroHack.Cheats;
using TaleWorlds.CampaignSystem.GameComponents;

namespace HeroHack.Patches
{
    /// <summary>
    /// When Persuasion Auto-Win is toggled ON, every persuasion attempt
    /// succeeds with 100 % critical-success chance.
    /// </summary>
    [HarmonyPatch(typeof(DefaultPersuasionModel), nameof(DefaultPersuasionModel.GetChances))]
    public static class PersuasionPatch
    {
        public static void Postfix(
            ref float successChance,
            ref float critSuccessChance,
            ref float critFailChance,
            ref float failChance)
        {
            try
            {
                if (!CombatCheats.IsPersuasionAutoWin)
                    return;

                successChance = 1f;
                critSuccessChance = 1f;
                failChance = 0f;
                critFailChance = 0f;
            }
            catch (Exception)
            {
                // Never break the game
            }
        }
    }
}
