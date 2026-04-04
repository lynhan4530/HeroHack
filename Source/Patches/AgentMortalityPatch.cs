using System;
using HarmonyLib;
using HeroHack.Cheats;
using TaleWorlds.MountAndBlade;

namespace HeroHack.Patches
{
    /// <summary>
    /// Layer 1 of invulnerability: override the mortality-state getter so the engine
    /// never transitions the player agent to Dead/Wounded.
    ///
    /// This alone is NOT enough — native code can still reduce HP to 0 and post-battle
    /// processing reads the low HP as "wounded."  DamageModelPatch (Layer 2) zeros
    /// incoming damage, and HeroHackMissionBehavior (Layer 3) clamps HP every tick.
    /// </summary>
    // DISABLED — suspected cause of character screen ragdoll corruption.
    // MortalityState.Invulnerable may poison the animation controller for
    // preview agents on the character/party screen.
    // [HarmonyPatch(typeof(Agent), nameof(Agent.CurrentMortalityState), MethodType.Getter)]
    public static class AgentMortalityPatch
    {
        // [HarmonyPostfix]
        public static void Postfix(Agent __instance, ref Agent.MortalityState __result)
        {
            try
            {
                if (__instance != null
                    && __instance.IsPlayerControlled
                    && (CombatCheats.IsPlayerInvulnerable || CombatCheats.IsPlayerImmortal))
                {
                    __result = Agent.MortalityState.Invulnerable;
                }
            }
            catch (Exception)
            {
                // Never crash the game
            }
        }
    }
}
