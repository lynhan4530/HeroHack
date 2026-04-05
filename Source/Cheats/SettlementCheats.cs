using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace HeroHack.Cheats
{
    public static class SettlementCheats
    {
        private const int TownProsperityCap = 8000;   // Bug 13: safe engine cap
        private const int CastleProsperityCap = 3000;

        // ── Guard helper ───────────────────────────────────────────────────
        /// <summary>Returns null + writes a status into 'error' if prerequisites fail.</summary>
        private static Town? GetTown(out string error)
        {
            // Bug 1: null when not in a settlement
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) { error = "Enter a settlement first."; return null; }
            // Bug 2: villages have no Town component
            if (!settlement.IsTown && !settlement.IsCastle) { error = "Only works in towns and castles."; return null; }
            error = string.Empty;
            return settlement.Town;
        }

        // ── Prosperity ─────────────────────────────────────────────────────
        public static string MaxProsperity()
        {
            var town = GetTown(out var err);
            if (town == null) return err;
            int cap = town.Settlement.IsTown ? TownProsperityCap : CastleProsperityCap;
            town.Prosperity = (float)cap;   // Bug 12: explicit cast
            return $"Prosperity set to {cap}.";
        }

        public static string SetProsperity(int value)
        {
            var town = GetTown(out var err);
            if (town == null) return err;
            int cap = town.Settlement.IsTown ? TownProsperityCap : CastleProsperityCap;
            value = Math.Max(0, Math.Min(value, cap));
            town.Prosperity = (float)value;
            return $"Prosperity set to {value}.";
        }

        // ── Loyalty ────────────────────────────────────────────────────────
        public static string MaxLoyalty()
        {
            var town = GetTown(out var err);
            if (town == null) return err;
            town.Loyalty = 100f;
            return "Loyalty set to 100.";
        }

        // ── Security ───────────────────────────────────────────────────────
        public static string MaxSecurity()
        {
            var town = GetTown(out var err);
            if (town == null) return err;
            town.Security = 100f;
            return "Security set to 100.";
        }

        // ── Garrison ───────────────────────────────────────────────────────
        /// <summary>
        /// Fills the garrison AND restocks food so troops don't desert from starvation (Bug 15).
        /// </summary>
        public static string FillGarrison(int count = 200)
        {
            var town = GetTown(out var err);
            if (town == null) return err;

            // Bug 3: garrison party may be null
            var garrison = town.GarrisonParty;
            if (garrison == null) return "No garrison party exists for this settlement.";

            // Bug 4: BasicTroop may be null in modded cultures
            var troop = Hero.MainHero?.Culture?.BasicTroop;
            if (troop == null) return "No basic troop available for this culture.";

            garrison.MemberRoster.AddToCounts(troop, count);

            // Bug 15: Must restock food or garrison starves and deserts
            town.FoodStocks = town.FoodStocksUpperLimit();

            return $"Garrison +{count}x {troop.Name}, food restocked.";
        }

        // ── Food stocks ────────────────────────────────────────────────────
        public static string MaxFoodStocks()
        {
            var town = GetTown(out var err);
            if (town == null) return err;
            town.FoodStocks = town.FoodStocksUpperLimit();
            return $"Food stocks set to {(int)town.FoodStocksUpperLimit()}.";
        }

        // ── Snapshot for display ───────────────────────────────────────────
        public static (string Name, string Prosperity, string Loyalty, string Security, string Food, string Garrison) GetSnapshot()
        {
            var s = Settlement.CurrentSettlement;
            if (s == null)
                return ("—", "—", "—", "—", "—", "—");
            if (!s.IsTown && !s.IsCastle)
                return (s.Name?.ToString() ?? "?", "N/A", "N/A", "N/A", "N/A", "N/A");

            var t = s.Town;
            string garrison = t.GarrisonParty != null
                ? t.GarrisonParty.MemberRoster.TotalManCount.ToString()
                : "—";
            return (
                s.Name?.ToString() ?? "?",
                ((int)t.Prosperity).ToString(),
                ((int)t.Loyalty).ToString(),
                ((int)t.Security).ToString(),
                ((int)t.FoodStocks).ToString(),
                garrison
            );
        }
    }
}
