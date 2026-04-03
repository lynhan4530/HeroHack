using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HeroHack.Cheats
{
    public static class HeroCheats
    {
        // ── Gold ────────────────────────────────────────────────────────────
        public static void SetGold(Hero hero, int targetGold)
        {
            if (hero == null) return;
            int delta = targetGold - hero.Gold;
            if (delta > 0)
                GiveGoldAction.ApplyBetweenCharacters(null, hero, delta, false);
            else if (delta < 0)
                GiveGoldAction.ApplyBetweenCharacters(hero, null, -delta, false);
        }

        // ── Renown ──────────────────────────────────────────────────────────
        public static void SetRenown(Hero hero, float targetRenown)
        {
            if (hero?.Clan == null) return;
            float delta = targetRenown - (hero.Clan.Renown);
            if (Math.Abs(delta) > 0.01f)
                hero.Clan.AddRenown(delta, false);
        }

        // ── Influence ───────────────────────────────────────────────────────
        public static void SetInfluence(float targetInfluence)
        {
            if (Clan.PlayerClan == null) return;
            float delta = targetInfluence - Clan.PlayerClan.Influence;
            ChangeClanInfluenceAction.Apply(Clan.PlayerClan, delta);
        }

        // ── Attributes ──────────────────────────────────────────────────────
        public static void SetAttribute(Hero hero, CharacterAttribute attr, int targetValue)
        {
            if (hero?.HeroDeveloper == null) return;
            int current = hero.GetAttributeValue(attr);
            int delta = targetValue - current;
            if (delta > 0)
                hero.HeroDeveloper.AddAttribute(attr, delta, checkUnspentPoints: false);
            else if (delta < 0)
                hero.HeroDeveloper.RemoveAttribute(attr, -delta);
        }

        // ── Level ────────────────────────────────────────────────────────────
        public static void SetLevel(Hero hero, int targetLevel)
        {
            if (hero?.HeroDeveloper == null) return;
            if (targetLevel < 1 || targetLevel > 62) return;
            try
            {
                hero.HeroDeveloper.SetInitialLevel(targetLevel);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"HeroHack: Level set failed — {ex.Message}",
                        Color.FromUint(0xFFFF4444)));
            }
        }

        // ── Skills ───────────────────────────────────────────────────────────
        public static void SetSkill(Hero hero, SkillObject skill, int targetValue)
        {
            if (hero == null || skill == null) return;
            targetValue = Math.Max(0, Math.Min(330, targetValue));
            hero.SetSkillValue(skill, targetValue);
        }

        public static void MaxAllSkills(Hero hero)
        {
            if (hero == null) return;
            hero.SetSkillValue(DefaultSkills.OneHanded, 330);
            hero.SetSkillValue(DefaultSkills.TwoHanded, 330);
            hero.SetSkillValue(DefaultSkills.Polearm, 330);
            hero.SetSkillValue(DefaultSkills.Bow, 330);
            hero.SetSkillValue(DefaultSkills.Crossbow, 330);
            hero.SetSkillValue(DefaultSkills.Throwing, 330);
            hero.SetSkillValue(DefaultSkills.Riding, 330);
            hero.SetSkillValue(DefaultSkills.Athletics, 330);
            hero.SetSkillValue(DefaultSkills.Crafting, 330);
            hero.SetSkillValue(DefaultSkills.Scouting, 330);
            hero.SetSkillValue(DefaultSkills.Tactics, 330);
            hero.SetSkillValue(DefaultSkills.Roguery, 330);
            hero.SetSkillValue(DefaultSkills.Charm, 330);
            hero.SetSkillValue(DefaultSkills.Leadership, 330);
            hero.SetSkillValue(DefaultSkills.Trade, 330);
            hero.SetSkillValue(DefaultSkills.Steward, 330);
            hero.SetSkillValue(DefaultSkills.Medicine, 330);
            hero.SetSkillValue(DefaultSkills.Engineering, 330);
        }

        // ── Attribute / Focus Points ─────────────────────────────────────────
        public static void AddAttributePoint(Hero hero)
        {
            if (hero?.HeroDeveloper == null) return;
            hero.HeroDeveloper.UnspentAttributePoints++;
        }

        public static void AddFocusPoint(Hero hero)
        {
            if (hero?.HeroDeveloper == null) return;
            hero.HeroDeveloper.UnspentFocusPoints++;
        }
    }
}
