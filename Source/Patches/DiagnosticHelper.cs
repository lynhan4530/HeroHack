using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HeroHack.Patches
{
    /// <summary>
    /// Runtime diagnostic that dumps Hero and Agent internal state to the in-game
    /// message log.  Triggered by F11 from the campaign map (wired in SubModule).
    /// Use this to verify which internal flag is causing the character-screen ragdoll.
    /// </summary>
    public static class DiagnosticHelper
    {
        private static readonly Color DiagColor = Color.FromUint(0xFFFFAA00); // orange

        public static void DumpHeroState()
        {
            try
            {
                Msg("──── HeroHack Diagnostic Dump ────");

                // ── Campaign-level Hero state ──
                var hero = Hero.MainHero;
                if (hero == null)
                {
                    Msg("Hero.MainHero is NULL");
                    return;
                }

                Msg($"Hero.IsAlive       : {hero.IsAlive}");
                Msg($"Hero.IsDead        : {hero.IsDead}");
                Msg($"Hero.IsWounded     : {hero.IsWounded}");
                Msg($"Hero.HeroState     : {hero.HeroState}");
                Msg($"Hero.HitPoints     : {hero.HitPoints}");
                Msg($"Hero.MaxHitPoints  : {hero.MaxHitPoints}");

                // Private fields that might be corrupted
                DumpField(hero, typeof(Hero), "_heroState");
                DumpField(hero, typeof(Hero), "_health");
                DumpField(hero, typeof(Hero), "_characterState");

                // ── Agent-level state (only valid inside a Mission) ──
                var agent = Agent.Main;
                if (agent != null)
                {
                    Msg("── Agent.Main (in-mission) ──");
                    Msg($"Agent.Health           : {agent.Health:F1}");
                    Msg($"Agent.HealthLimit      : {agent.HealthLimit:F1}");
                    Msg($"Agent.State            : {agent.State}");
                    Msg($"Agent.CurrentMortalityState : {agent.CurrentMortalityState}");
                    Msg($"Agent.IsActive()       : {agent.IsActive()}");

                    DumpField(agent, typeof(Agent), "_stateFlags");
                    DumpField(agent, typeof(Agent), "_state");
                    DumpField(agent, typeof(Agent), "_agentFlags");
                }
                else
                {
                    Msg("Agent.Main: null (not in mission)");
                }

                // ── CombatCheats toggle snapshot ──
                Msg("── CombatCheats flags ──");
                Msg($"IsPlayerInvulnerable : {Cheats.CombatCheats.IsPlayerInvulnerable}");
                Msg($"IsPlayerImmortal     : {Cheats.CombatCheats.IsPlayerImmortal}");
                Msg($"IsOneHitKill         : {Cheats.CombatCheats.IsOneHitKill}");
                Msg($"IsPersuasionAutoWin  : {Cheats.CombatCheats.IsPersuasionAutoWin}");

                Msg("──── End Diagnostic Dump ────");
            }
            catch (Exception ex)
            {
                Msg($"DIAG ERROR: {ex.Message}");
            }
        }

        private static void DumpField(object obj, Type type, string fieldName)
        {
            try
            {
                var field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (field != null)
                    Msg($"  [{type.Name}].{fieldName} = {field.GetValue(obj)}");
                else
                    Msg($"  [{type.Name}].{fieldName} = (field not found)");
            }
            catch (Exception ex)
            {
                Msg($"  [{type.Name}].{fieldName} = ERROR: {ex.Message}");
            }
        }

        private static void Msg(string text)
        {
            InformationManager.DisplayMessage(new InformationMessage(text, DiagColor));
        }
    }
}
