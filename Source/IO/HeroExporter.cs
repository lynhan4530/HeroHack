using System;
using System.IO;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace HeroHack.IO
{
    public static class HeroExporter
    {
        public static string Export(Hero hero)
        {
            if (hero == null) return "No hero selected.";

            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "HeroHack", "exports");
            Directory.CreateDirectory(dir);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
            string heroName = hero.Name != null ? hero.Name.ToString() : "UnknownHero";
            string safeName = SanitiseId(heroName);
            string fileName = $"{safeName}_{timestamp}.xml";
            string filePath = Path.Combine(dir, fileName);

            var doc = BuildXml(hero);
            doc.Save(filePath);
            return $"Exported: {filePath}";
        }

        private static XDocument BuildXml(Hero hero)
        {
            var root = new XElement("HeroExport",
                new XAttribute("schema_version", "1"),
                new XAttribute("export_date", DateTime.UtcNow.ToString("o")));

            // ── Identity ──────────────────────────────────────────────────────
            root.Add(new XElement("Identity",
                new XAttribute("string_id", hero.StringId ?? ""),
                new XAttribute("name",      hero.Name?.ToString() ?? ""),
                new XAttribute("culture",   hero.Culture?.StringId ?? ""),
                new XAttribute("clan",      hero.Clan?.StringId ?? ""),
                new XAttribute("age",       (int)hero.Age),
                new XAttribute("level",     hero.Level),
                new XAttribute("gold",      hero.Gold),
                new XAttribute("renown",    (int)(hero.Clan?.Renown ?? 0f))));

            // ── Attributes — hardcoded 6; avoids Enum.GetValues sentinel values ──
            var attrsEl = new XElement("Attributes");
            var attrEntries = new (string id, CharacterAttribute attr)[]
            {
                ("Vigor",        DefaultCharacterAttributes.Vigor),
                ("Control",      DefaultCharacterAttributes.Control),
                ("Endurance",    DefaultCharacterAttributes.Endurance),
                ("Cunning",      DefaultCharacterAttributes.Cunning),
                ("Social",       DefaultCharacterAttributes.Social),
                ("Intelligence", DefaultCharacterAttributes.Intelligence),
            };
            foreach (var (id, attr) in attrEntries)
            {
                try
                {
                    attrsEl.Add(new XElement("Attr",
                        new XAttribute("id",    id),
                        new XAttribute("value", hero.GetAttributeValue(attr))));
                }
                catch { /* skip if attr not accessible */ }
            }
            root.Add(attrsEl);

            // ── Skills — Bug 1: GetFocus (not GetFocusValue — doesn't exist) ──
            var skillsEl = new XElement("Skills");
            var skills = new SkillObject[]
            {
                DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm,
                DefaultSkills.Bow,       DefaultSkills.Crossbow,  DefaultSkills.Throwing,
                DefaultSkills.Riding,    DefaultSkills.Athletics,  DefaultSkills.Crafting,
                DefaultSkills.Scouting,  DefaultSkills.Tactics,   DefaultSkills.Roguery,
                DefaultSkills.Charm,     DefaultSkills.Leadership, DefaultSkills.Trade,
                DefaultSkills.Steward,   DefaultSkills.Medicine,   DefaultSkills.Engineering,
            };
            foreach (var skill in skills)
            {
                if (skill == null) continue;
                int value = hero.GetSkillValue(skill);
                int focus = 0;
                try { focus = hero.HeroDeveloper?.GetFocus(skill) ?? 0; } catch { }
                skillsEl.Add(new XElement("Skill",
                    new XAttribute("id",    skill.StringId),
                    new XAttribute("value", value),
                    new XAttribute("focus", focus)));
            }
            root.Add(skillsEl);

            // ── Perks — Bug 2: GetObjectTypeList<PerkObject> (PerkObject.All doesn't exist) ──
            var perksEl = new XElement("Perks");
            try
            {
                foreach (PerkObject perk in MBObjectManager.Instance.GetObjectTypeList<PerkObject>())
                {
                    if (perk != null && hero.GetPerkValue(perk))
                        perksEl.Add(new XElement("Perk", new XAttribute("id", perk.StringId)));
                }
            }
            catch { /* skip perks on error */ }
            root.Add(perksEl);

            // ── Traits — Bug 3: GetObjectTypeList<TraitObject> (TraitObject.All doesn't exist) ──
            var traitsEl = new XElement("Traits");
            try
            {
                foreach (TraitObject trait in MBObjectManager.Instance.GetObjectTypeList<TraitObject>())
                {
                    if (trait == null) continue;
                    int val = hero.GetTraitLevel(trait);
                    if (val != 0)
                        traitsEl.Add(new XElement("Trait",
                            new XAttribute("id",    trait.StringId),
                            new XAttribute("value", val)));
                }
            }
            catch { /* skip traits on error */ }
            root.Add(traitsEl);

            // ── Equipment — Bug 4: loop 0-11 with per-slot try/catch ──────────
            root.Add(BuildEquipmentElement("BattleEquipment",   hero.BattleEquipment));
            root.Add(BuildEquipmentElement("StealthEquipment",   hero.StealthEquipment));
            root.Add(BuildEquipmentElement("CivilianEquipment",  hero.CivilianEquipment));

            return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        }

        private static XElement BuildEquipmentElement(string tag, Equipment? equipment)
        {
            var el = new XElement(tag);
            if (equipment == null) return el;
            for (int i = 0; i <= 11; i++)
            {
                try
                {
                    var slot = equipment[(EquipmentIndex)i];
                    el.Add(new XElement("Slot",
                        new XAttribute("index",    i),
                        new XAttribute("item_id",  slot.Item?.StringId ?? ""),
                        new XAttribute("modifier", slot.ItemModifier?.StringId ?? "")));
                }
                catch { /* skip inaccessible slot index */ }
            }
            return el;
        }

        private static string SanitiseId(string? id)
        {
            if (string.IsNullOrEmpty(id)) return "unknown";
            var safe = new System.Text.StringBuilder(id);
            foreach (char c in Path.GetInvalidFileNameChars())
                safe.Replace(c.ToString(), "_");
            return safe.ToString();
        }
    }
}
