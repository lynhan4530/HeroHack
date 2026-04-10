using System;
using HarmonyLib;
using HeroHack.Cheats;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace HeroHack.Patches
{
    /// <summary>
    /// Postfix patch on the party size limit model.
    /// Forces the player's effective party size cap to at least the override value.
    /// Actual signature: ExplainedNumber GetPartyMemberSizeLimit(PartyBase, bool includeDescriptions)
    /// </summary>
    public static class PartySizeOverridePatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            var original = typeof(DefaultPartySizeLimitModel)
                .GetMethod("GetPartyMemberSizeLimit",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var postfix = typeof(PartySizeOverridePatch)
                .GetMethod(nameof(Postfix),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }

        public static void Postfix(PartyBase party, ref ExplainedNumber __result)
        {
            try
            {
                if (party == MobileParty.MainParty?.Party && PartyCheats.PartySizeOverride > 0)
                {
                    float current = __result.ResultNumber;
                    float target = PartyCheats.PartySizeOverride;
                    if (target > current)
                    {
                        // ExplainedNumber.Add signature: (float value, TextObject description, TextObject variable)
                        __result.Add(target - current, null, null);
                    }
                }
            }
            catch (Exception)
            {
                // Never break the game — fall through
            }
        }
    }
}
