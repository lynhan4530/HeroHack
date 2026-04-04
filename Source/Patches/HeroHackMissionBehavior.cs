using System;
using HeroHack.Cheats;
using TaleWorlds.MountAndBlade;

namespace HeroHack.Patches
{
    /// <summary>
    /// Primary combat cheat logic via MissionBehavior (no Harmony needed).
    ///
    ///  • Invulnerable / Immortal → clamp player HP to max every tick.
    ///  • One-Hit Kill → kill every non-friendly agent near the player on each
    ///    tick where the player lands a blow (detected via HP delta).
    ///
    /// This replaces the DamageModelPatch which crashed Harmony on startup
    /// and the MortalityState getter which corrupted character-screen animations.
    /// </summary>
    public class HeroHackMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnMissionTick(float dt)
        {
            try
            {
                Agent player = Agent.Main;
                if (player == null || !player.IsActive())
                    return;

                // ── Invulnerable / Immortal: keep HP at max ──
                if ((CombatCheats.IsPlayerInvulnerable || CombatCheats.IsPlayerImmortal)
                    && player.Health < player.HealthLimit)
                {
                    player.Health = player.HealthLimit;
                }
            }
            catch (Exception)
            {
                // Never crash the game
            }
        }

        public override void OnAgentHit(
            Agent affectedAgent,
            Agent affectorAgent,
            in MissionWeapon affectorWeapon,
            in Blow blow,
            in AttackCollisionData attackCollisionData)
        {
            try
            {
                // ── One-Hit Kill: if player hit an enemy, kill them ──
                if (CombatCheats.IsOneHitKill
                    && affectorAgent != null
                    && affectorAgent == Agent.Main
                    && affectedAgent != null
                    && affectedAgent != Agent.Main
                    && affectedAgent.IsActive()
                    && affectedAgent.Health > 0)
                {
                    affectedAgent.Health = 0;

                    var killBlow = new Blow(affectorAgent.Index);
                    killBlow.InflictedDamage = 10000;
                    killBlow.DamageType = TaleWorlds.Core.DamageTypes.Cut;
                    killBlow.GlobalPosition = affectedAgent.Position;
                    killBlow.WeaponRecord = blow.WeaponRecord;
                    affectedAgent.Die(killBlow);
                }
            }
            catch (Exception)
            {
                // Never crash the game
            }
        }
    }
}
