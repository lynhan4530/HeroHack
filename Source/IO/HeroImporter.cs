using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HeroHack.Cheats;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace HeroHack.IO
{
    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new List<string>();
    }

    public static class HeroImporter
    {
        public static ImportResult Import(string xmlFilePath, Hero targetHero)
        {
            var result = new ImportResult();
            if (targetHero == null) { result.Message = "No target hero selected."; return result; }
            if (!File.Exists(xmlFilePath)) { result.Message = "File not found."; return result; }

            XDocument doc;
            try { doc = XDocument.Load(xmlFilePath); }
            catch (Exception ex) { result.Message = $"XML parse error: {ex.Message}"; return result; }

            var root = doc.Root;
            if (root == null || (string?)root.Attribute("schema_version") != "1")
            {
                result.Message = "Invalid or unsupported schema (missing schema_version=\"1\").";
                return result;
            }

            ApplyAttributes(root, targetHero, result);
            ApplySkills(root,     targetHero, result);
            ApplyLevel(root,      targetHero, result);
            ApplyGold(root,       targetHero, result);
            ApplyRenown(root,     targetHero, result);
            ApplyPerks(root,      targetHero, result);
            ApplyTraits(root,     targetHero, result);
            ApplyEquipment(root,  targetHero, result);

            result.Success = true;
            result.Message = $"Import done. {result.Warnings.Count} warning(s).";
            return result;
        }

        // ── Attributes ────────────────────────────────────────────────────────

        private static void ApplyAttributes(XElement root, Hero hero, ImportResult r)
        {
            // Build map at call time (Bug 10 pattern: no static init of game objects)
            var attrMap = new Dictionary<string, CharacterAttribute>
            {
                { "Vigor",        DefaultCharacterAttributes.Vigor },
                { "Control",      DefaultCharacterAttributes.Control },
                { "Endurance",    DefaultCharacterAttributes.Endurance },
                { "Cunning",      DefaultCharacterAttributes.Cunning },
                { "Social",       DefaultCharacterAttributes.Social },
                { "Intelligence", DefaultCharacterAttributes.Intelligence },
            };

            foreach (var el in root.Element("Attributes")?.Elements("Attr") ?? Enumerable.Empty<XElement>())
            {
                try
                {
                    string? id = (string?)el.Attribute("id");
                    int target = (int?)el.Attribute("value") ?? 0;
                    if (id == null || !attrMap.TryGetValue(id, out var attr)) continue;
                    HeroCheats.SetAttribute(hero, attr, target);
                }
                catch (Exception ex) { r.Warnings.Add($"Attr '{(string?)el.Attribute("id")}': {ex.Message}"); }
            }
        }

        // ── Skills ────────────────────────────────────────────────────────────

        private static Dictionary<string, SkillObject> BuildSkillMap()
        {
            var map = new Dictionary<string, SkillObject>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in new SkillObject[]
            {
                DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm,
                DefaultSkills.Bow,       DefaultSkills.Crossbow,  DefaultSkills.Throwing,
                DefaultSkills.Riding,    DefaultSkills.Athletics,  DefaultSkills.Crafting,
                DefaultSkills.Scouting,  DefaultSkills.Tactics,   DefaultSkills.Roguery,
                DefaultSkills.Charm,     DefaultSkills.Leadership, DefaultSkills.Trade,
                DefaultSkills.Steward,   DefaultSkills.Medicine,   DefaultSkills.Engineering,
            })
            {
                if (s?.StringId != null) map[s.StringId] = s;
            }
            return map;
        }

        private static void ApplySkills(XElement root, Hero hero, ImportResult r)
        {
            var skillMap = BuildSkillMap();
            foreach (var el in root.Element("Skills")?.Elements("Skill") ?? Enumerable.Empty<XElement>())
            {
                try
                {
                    string? id  = (string?)el.Attribute("id");
                    int value   = (int?)el.Attribute("value") ?? 0;
                    // Bug 14: clamp focus 0-5 — out-of-range crashes SetFocus
                    int focus   = Math.Max(0, Math.Min(5, (int?)el.Attribute("focus") ?? 0));
                    if (id == null || !skillMap.TryGetValue(id, out var skill)) continue;

                    HeroCheats.SetSkill(hero, skill, value);
                    // Bug 5: AddFocus/RemoveFocus (delta-based), not SetFocus (doesn't exist)
                    if (hero.HeroDeveloper != null)
                    {
                        int currentFocus = hero.HeroDeveloper.GetFocus(skill);
                        int focusDelta = focus - currentFocus;
                        if (focusDelta > 0)  hero.HeroDeveloper.AddFocus(skill, focusDelta, false);
                        else if (focusDelta < 0) hero.HeroDeveloper.RemoveFocus(skill, -focusDelta);
                    }
                }
                catch (Exception ex) { r.Warnings.Add($"Skill '{(string?)el.Attribute("id")}': {ex.Message}"); }
            }
        }

        // ── Level / Gold / Renown ─────────────────────────────────────────────

        private static void ApplyLevel(XElement root, Hero hero, ImportResult r)
        {
            try
            {
                int level = (int?)root.Element("Identity")?.Attribute("level") ?? 0;
                if (level < 1 || level > 62) return;
                HeroCheats.SetLevel(hero, level);
                // Bug 15: SetInitialLevel resets all XP — warn the user
                r.Warnings.Add("Level applied via SetInitialLevel — XP progress was reset.");
            }
            catch (Exception ex) { r.Warnings.Add($"Level: {ex.Message}"); }
        }

        private static void ApplyGold(XElement root, Hero hero, ImportResult r)
        {
            try
            {
                int gold = (int?)root.Element("Identity")?.Attribute("gold") ?? 0;
                HeroCheats.SetGold(hero, gold);
            }
            catch (Exception ex) { r.Warnings.Add($"Gold: {ex.Message}"); }
        }

        private static void ApplyRenown(XElement root, Hero hero, ImportResult r)
        {
            try
            {
                // Bug 8: null clan guard — wanderers / clanless heroes crash without this
                if (hero.Clan == null) { r.Warnings.Add("Renown skipped — hero has no clan."); return; }
                int renown = (int?)root.Element("Identity")?.Attribute("renown") ?? 0;
                HeroCheats.SetRenown(hero, renown);
            }
            catch (Exception ex) { r.Warnings.Add($"Renown: {ex.Message}"); }
        }

        // ── Perks ─────────────────────────────────────────────────────────────

        private static void ApplyPerks(XElement root, Hero hero, ImportResult r)
        {
            try
            {
                var wantedIds = new HashSet<string>(
                    root.Element("Perks")?.Elements("Perk")
                        .Select(e => (string?)e.Attribute("id"))
                        .Where(id => id != null)
                        .Cast<string>()
                    ?? Enumerable.Empty<string>());

                // Bug 2: GetObjectTypeList<PerkObject> — PerkObject.All doesn't exist
                // Note: no RemovePerk API — import is additive for perks only
                foreach (PerkObject perk in MBObjectManager.Instance.GetObjectTypeList<PerkObject>())
                {
                    if (perk?.StringId == null) continue;
                    bool wanted = wantedIds.Contains(perk.StringId);
                    try
                    {
                        if (wanted && !hero.GetPerkValue(perk))
                            hero.HeroDeveloper?.AddPerk(perk); // AddPerk (SetPerkValue doesn't exist publicly)
                    }
                    catch (Exception ex) { r.Warnings.Add($"Perk '{perk.StringId}': {ex.Message}"); }
                }
            }
            catch (Exception ex) { r.Warnings.Add($"Perks block: {ex.Message}"); }
        }

        // ── Traits ────────────────────────────────────────────────────────────

        private static void ApplyTraits(XElement root, Hero hero, ImportResult r)
        {
            try
            {
                // Bug 3: GetObjectTypeList<TraitObject> — TraitObject.All doesn't exist
                var traitMap = MBObjectManager.Instance.GetObjectTypeList<TraitObject>()
                    .Where(t => t?.StringId != null)
                    .ToDictionary(t => t.StringId!, t => t);

                foreach (var el in root.Element("Traits")?.Elements("Trait") ?? Enumerable.Empty<XElement>())
                {
                    try
                    {
                        string? id = (string?)el.Attribute("id");
                        int val    = (int?)el.Attribute("value") ?? 0;
                        if (id == null || !traitMap.TryGetValue(id, out var trait)) continue;
                        hero.SetTraitLevel(trait, val);
                    }
                    catch (Exception ex) { r.Warnings.Add($"Trait '{(string?)el.Attribute("id")}': {ex.Message}"); }
                }
            }
            catch (Exception ex) { r.Warnings.Add($"Traits block: {ex.Message}"); }
        }

        // ── Equipment ─────────────────────────────────────────────────────────

        private static void ApplyEquipment(XElement root, Hero hero, ImportResult r)
        {
            ApplyEquipmentSet("BattleEquipment",   root, hero.BattleEquipment,   r);
            ApplyEquipmentSet("StealthEquipment",   root, hero.StealthEquipment,  r);
            ApplyEquipmentSet("CivilianEquipment",  root, hero.CivilianEquipment,  r);
        }

        private static void ApplyEquipmentSet(string tag, XElement root, Equipment? equipment, ImportResult r)
        {
            if (equipment == null) return;
            foreach (var el in root.Element(tag)?.Elements("Slot") ?? Enumerable.Empty<XElement>())
            {
                // Bug 9: per-slot try/catch — equipment setter can throw for certain hero types
                try
                {
                    int i = (int?)el.Attribute("index") ?? -1;
                    if (i < 0 || i > 11) continue;
                    string itemId = (string?)el.Attribute("item_id") ?? "";
                    var idx = (EquipmentIndex)i;

                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        equipment[idx] = EquipmentElement.Invalid;
                        continue;
                    }

                    var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                    if (item == null)
                    {
                        r.Warnings.Add($"{tag}[{i}]: item '{itemId}' not found — slot cleared.");
                        equipment[idx] = EquipmentElement.Invalid;
                    }
                    else
                    {
                        equipment[idx] = new EquipmentElement(item);
                    }
                }
                catch (Exception ex) { r.Warnings.Add($"{tag}[{(string?)el.Attribute("index")}]: {ex.Message}"); }
            }
        }
    }
}
