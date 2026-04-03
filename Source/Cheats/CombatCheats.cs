namespace HeroHack.Cheats
{
    /// <summary>
    /// Shared static state read by Harmony patches (implemented in Phase 3).
    /// </summary>
    public static class CombatCheats
    {
        public static bool IsPlayerInvulnerable { get; set; } = false;
        public static bool IsOneHitKill { get; set; } = false;
        public static bool IsPlayerImmortal { get; set; } = false;
        public static bool IsPersuasionAutoWin { get; set; } = false;
    }
}
