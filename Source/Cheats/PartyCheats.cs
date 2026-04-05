using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace HeroHack.Cheats
{
    public static class PartyCheats
    {
        // ── Morale ─────────────────────────────────────────────────────────
        /// <summary>
        /// Adds +100 to recent-events morale. This decays naturally over days —
        /// there is NO permanent morale setter on MobileParty (Bug 14 fix).
        /// </summary>
        public static string BoostMorale()
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";
            party.RecentEventsMorale = 100f;
            return $"Morale boosted (fades naturally). Current: {(int)party.Morale}";
        }

        // ── Food ───────────────────────────────────────────────────────────
        /// <summary>Adds food items to party inventory. Uses grain or any food as fallback (Bug 6 fix).</summary>
        public static string AddFood(int amount = 200)
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";

            var grain = GetFoodItem();
            if (grain == null) return "No food item found (modded game?).";

            party.ItemRoster.AddToCounts(grain, amount);
            return $"Added {amount}x {grain.Name} to party.";
        }

        // ── Heal ───────────────────────────────────────────────────────────
        /// <summary>
        /// Heals all wounded troops.
        /// Uses index-based AddToCountsAtIndex — NOT foreach (Bug 7 fix).
        /// </summary>
        public static string HealAllWounded()
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";

            var roster = party.MemberRoster;
            int totalHealed = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                int wounded = roster.GetElementWoundedNumber(i);
                if (wounded > 0)
                {
                    roster.AddToCountsAtIndex(i, 0, -wounded, 0, false);
                    totalHealed += wounded;
                }
            }
            return totalHealed > 0 ? $"Healed {totalHealed} wounded troops." : "No wounded troops.";
        }

        // ── Add Troops ─────────────────────────────────────────────────────
        /// <summary>Adds player-culture basic recruits up to the requested count (Bug 4, 5 fix).</summary>
        public static string AddTroops(int count = 50)
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";

            var troop = Hero.MainHero?.Culture?.BasicTroop;
            if (troop == null) return "No basic troop available for this culture.";

            party.MemberRoster.AddToCounts(troop, count);
            return $"Added {count}x {troop.Name} to party.";
        }

        // ── Internal ───────────────────────────────────────────────────────
        private static ItemObject? GetFoodItem()
        {
            // Try standard grain ID first
            var item = Game.Current?.ObjectManager?.GetObject<ItemObject>("grain");
            if (item != null) return item;
            // Fallback: any food item (handles modded grain IDs — Bug 6)
            return Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>()
                       .FirstOrDefault(i => i.IsFood);
        }
    }
}
