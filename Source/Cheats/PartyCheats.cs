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

        // ── Add Elite Troops ───────────────────────────────────────────────
        /// <summary>Adds the explicitly selected top-tier noble troop.</summary>
        public static string AddEliteTroops(int count, CharacterObject? targetTroop)
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";

            if (targetTroop == null)
            {
                var culture = Hero.MainHero?.Culture;
                if (culture == null) return "Player has no culture.";

                CharacterObject? noble = culture.EliteBasicTroop ?? culture.BasicTroop;
                if (noble == null) return "No troops available for this culture.";

                targetTroop = noble;
                int safetyLimit = 10;
                while (targetTroop.UpgradeTargets != null && targetTroop.UpgradeTargets.Length > 0 && safetyLimit > 0)
                {
                    targetTroop = targetTroop.UpgradeTargets[0];
                    safetyLimit--;
                }
            }

            party.MemberRoster.AddToCounts(targetTroop, count);
            return $"Added {count}x {targetTroop.Name} to party.";
        }

        // ── Auto-Promote ───────────────────────────────────────────────────
        /// <summary>Smart Promote: Free upgrades for linear troops, max XP for branching troops.</summary>
        public static string AutoPromote()
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";

            var roster = party.MemberRoster;
            int totalUpgraded = 0;
            int totalXpGiven = 0;
            
            // Iterate backwards because we are modifying roster elements directly
            for (int i = roster.Count - 1; i >= 0; i--)
            {
                var element = roster.GetElementCopyAtIndex(i);
                CharacterObject troop = element.Character;
                
                if (troop != null && !troop.IsHero && troop.UpgradeTargets != null && troop.UpgradeTargets.Length > 0)
                {
                    int count = element.Number;
                    if (count > 0)
                    {
                        if (troop.UpgradeTargets.Length == 1)
                        {
                            // Linear upgrade: instantly upgrade them physically for free
                            CharacterObject target = troop.UpgradeTargets[0];
                            roster.AddToCounts(troop, -count);
                            roster.AddToCounts(target, count);
                            totalUpgraded += count;
                        }
                        else
                        {
                            // Branching upgrade: Inject massive XP so player can choose in UI
                            // BUGFIX: TaleWorlds API signature is (index, xpAmount), NOT (xpAmount, index)!
                            roster.AddXpToTroopAtIndex(i, 10000 * count);
                            totalXpGiven += count;
                        }
                    }
                }
            }
            return $"Upgraded {totalUpgraded} linear troops. Injected max XP to {totalXpGiven} branching troops.";
        }

        // ── Provide Upgrade Mounts ────────────────────────────────────────
        /// <summary>Injects necessary horses and warhorses for upgrade-ready troops.</summary>
        public static string ProvideUpgradeMounts()
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";

            var roster = party.MemberRoster;
            int normalHorsesNeeded = 0;
            int warHorsesNeeded = 0;

            for (int i = 0; i < roster.Count; i++)
            {
                var element = roster.GetElementCopyAtIndex(i);
                var troop = element.Character;
                int count = element.Number;

                if (troop != null && !troop.IsHero && troop.UpgradeTargets != null)
                {
                    bool needsNormal = false;
                    bool needsWar = false;

                    // A troop's upgrade requirement applies to traversing TO the target
                    // TaleWorlds API CharacterObject.UpgradeRequiresItemFromCategory evaluates the TARGET's source requirements?
                    // Actually, let's just inspect ALL targets
                    foreach (var target in troop.UpgradeTargets)
                    {
                        // In Bannerlord, CharacterObject.DefaultCharacterObject or something might have the item req.
                        // Or we can just check if target.IsMounted and we don't have a horse?
                        if (!troop.IsMounted && target.IsMounted)
                        {
                            if (target.Tier >= 5) needsWar = true;
                            else needsNormal = true;
                        }
                        
                        if (troop.UpgradeRequiresItemFromCategory != null)
                        {
                            string rc = troop.UpgradeRequiresItemFromCategory.StringId;
                            if (rc == "horse") needsNormal = true;
                            if (rc == "war_horse") needsWar = true;
                        }
                    }

                    if (needsNormal) normalHorsesNeeded += count;
                    else if (needsWar) warHorsesNeeded += count;
                }
            }

            int existingNormal = 0;
            int existingWar = 0;
            for (int i = 0; i < party.ItemRoster.Count; i++)
            {
                var itemElement = party.ItemRoster.GetElementCopyAtIndex(i);
                var itemCat = itemElement.EquipmentElement.Item?.ItemCategory?.StringId;
                if (itemCat == "horse") existingNormal += itemElement.Amount;
                if (itemCat == "war_horse") existingWar += itemElement.Amount;
            }

            int injectNormal = Math.Max(0, normalHorsesNeeded - existingNormal);
            int injectWar = Math.Max(0, warHorsesNeeded - existingWar);

            if (injectNormal > 0 || injectWar > 0)
            {
                var allItems = TaleWorlds.Core.Game.Current.ObjectManager.GetObjectTypeList<TaleWorlds.Core.ItemObject>();
                var sumpter = allItems.FirstOrDefault(x => x.StringId == "sumpter_horse") ?? allItems.FirstOrDefault(x => x.ItemCategory?.StringId == "horse");
                var charger = allItems.FirstOrDefault(x => x.StringId == "imperial_charger") ?? allItems.FirstOrDefault(x => x.ItemCategory?.StringId == "war_horse");

                if (sumpter != null && injectNormal > 0) party.ItemRoster.AddToCounts(sumpter, injectNormal);
                if (charger != null && injectWar > 0) party.ItemRoster.AddToCounts(charger, injectWar);

                return $"Spawned {injectNormal} Horses & {injectWar} Warhorses for upgrades!";
            }
            
            return "Party already has sufficient mounts for queued upgrades.";
        }

        // ── Mass Defection ──────────────────────────────────────────────────
        /// <summary>Instantly converts all regular prisoners into troops.</summary>
        public static string RecruitPrisoners()
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";

            var prisoners = party.PrisonRoster;
            if (prisoners.Count == 0) return "No prisoners to convert.";

            int convertedCount = 0;
            for (int i = prisoners.Count - 1; i >= 0; i--)
            {
                var element = prisoners.GetElementCopyAtIndex(i);
                if (element.Character != null && !element.Character.IsHero)
                {
                    int amount = element.Number;
                    party.MemberRoster.AddToCounts(element.Character, amount);
                    prisoners.AddToCounts(element.Character, -amount);
                    convertedCount += amount;
                }
            }
            return convertedCount > 0 ? $"Converted {convertedCount} regular prisoners." : "Lords cannot be recruited via Mass Defection.";
        }

        // ── Mount Hoarder ──────────────────────────────────────────────────
        /// <summary>Adds safely calculated amount of horses to optimize speed without Herd Penalty.</summary>
        public static string MountHoarder()
        {
            var party = MobileParty.MainParty;
            if (party == null) return "No active party.";

            int footmen = 0;
            for (int i = 0; i < party.MemberRoster.Count; i++)
            {
                var troop = party.MemberRoster.GetElementCopyAtIndex(i).Character;
                if (troop != null && !troop.IsMounted)
                    footmen += party.MemberRoster.GetElementNumber(i);
            }

            int currentHorses = 0;
            for (int i = 0; i < party.ItemRoster.Count; i++)
            {
                var item = party.ItemRoster.GetElementCopyAtIndex(i).EquipmentElement.Item;
                if (item != null && item.ItemType == ItemObject.ItemTypeEnum.Horse)
                {
                    string cat = item.ItemCategory?.StringId;
                    if (cat == "horse" || cat == "war_horse" || cat == "noble_horse")
                    {
                        currentHorses += party.ItemRoster.GetElementNumber(i);
                    }
                }
            }

            int deficit = footmen - currentHorses;
            if (deficit <= 0) return "You have enough mounts! Adding more triggers Herd Penalty.";

            var allItems = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>();
            if (allItems == null) return "Could not find item database.";
            
            var simpleHorse = allItems.FirstOrDefault(i => i.ItemType == ItemObject.ItemTypeEnum.Horse && !i.NotMerchandise);
            if (simpleHorse == null) simpleHorse = allItems.FirstOrDefault(i => i.ItemType == ItemObject.ItemTypeEnum.Horse);
            
            if (simpleHorse == null) return "No valid horse item found in game files.";

            party.ItemRoster.AddToCounts(simpleHorse, deficit);
            return $"Harvested {deficit}x {simpleHorse.Name} for optimal map speed.";
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
