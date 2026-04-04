// DELETED — Harmony cannot patch SandboxAgentApplyDamageModel.CalculateDamage
// without crashing (native struct ref parameters). Combat logic moved to
// HeroHackMissionBehavior which uses MissionBehavior.OnMissionTick instead.
//
// Kept as empty file so git tracks the deletion reason.

namespace HeroHack.Patches
{
    // Intentionally empty — see comment above.
    internal static class DamageModelPatch_Removed { }
}
