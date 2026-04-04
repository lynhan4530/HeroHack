# HeroHack

A cheat/developer panel mod for **Mount & Blade II: Bannerlord v1.3.x**.  
Press **F10** on the campaign map (or click the **HH** button in the top-right corner) to open the panel.

---

## Features (planned — implemented by phase)

| Phase | Status | Feature |
|-------|--------|---------|
| 1 | ✅ Done | Panel opens/closes via F10 and HUD button. Tab bar switches between Hero / Party / Settlement / Export-Import tabs. |
| 2 | ✅ Done | Hero stat editing — Gold (formatted), Renown, Influence, Attributes, Skills (all 18, cap 0–9999), Level. Hero selector (player + companions with Prev/Next/Refresh). Combat toggles (Invulnerable, Immortal, Persuasion AutoWin). |
| 3 | ✅ Done | Harmony patches — Invulnerability (HP clamp via MissionBehavior), One-Hit Kill (OnAgentHit), Immortality (blocks KillCharacterAction), Persuasion Auto-Win (DefaultPersuasionModel postfix). F11 diagnostic dump. |
| 4 | 🔜 Next | Party cheats — Add troops, max morale, add food, heal all. Settlement cheats — Prosperity, Loyalty, Security, Garrison. |
| 5 | 🔜 | XML Export / Import — full hero profile round-trip to `Documents\HeroHack\exports\`. |
| 6 | 🔜 | Polish — error handling, status messages, final QA. |

---

## Requirements

- Mount & Blade II: Bannerlord `v1.3.x`
- **[Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)** (Steam Workshop ID `2859188632`) — must be enabled and loaded before HeroHack

## Installation

1. Subscribe to **Harmony** on the Steam Workshop.
2. Copy the `HeroHack/` folder into `[GameInstall]\Modules\`.
3. Enable **HeroHack** in the Bannerlord launcher (after Harmony, Native, SandBoxCore, Sandbox, StoryMode, CustomBattle).
4. Load a campaign save.

## Build from Source

```powershell
cd "[GameInstall]\Modules\HeroHack"
dotnet build -c Debug
```

Output: `bin\Win64_Shipping_Client\HeroHack.dll`

### Dependency rules (for contributors)
- Target framework: `net472` (Bannerlord runs on Mono/net472, **not** net6+).
- All game and Workshop DLLs must use `<Private>False</Private>` — never copied to output.
- Harmony is referenced from `$(WorkshopFolder)\2859188632\bin\Win64_Shipping_Client\0Harmony.dll`.
- Only `HeroHack.dll` and `HeroHack.pdb` should appear in `bin\Win64_Shipping_Client\` after build.

## Project Structure

```
HeroHack/
├── HeroHack.csproj
├── SubModule.xml
├── GUI/
│   └── Prefabs/
│       ├── HeroHackPanel.xml   ← Main cheat panel (920×1010, tabbed)
│       └── HeroHackHud.xml     ← Persistent HH button (top-right map HUD)
└── Source/
    ├── SubModule.cs            ← Entry point: Harmony init, layer injection, F10 hotkey, F11 diagnostic
    ├── Cheats/
    │   ├── HeroCheats.cs       ← Static helpers: SetSkill, MaxAllSkills, AddGold, etc.
    │   └── CombatCheats.cs     ← Shared static flags read by patches/behaviors
    ├── Patches/
    │   ├── AgentMortalityPatch.cs        ← (DISABLED) MortalityState getter — causes ragdoll corruption
    │   ├── HeroDeathPatch.cs             ← Blocks KillCharacterAction for player when Immortal is ON
    │   ├── HeroHackMissionBehavior.cs    ← HP clamp (invulnerable) + One-Hit Kill via OnAgentHit
    │   ├── PersuasionPatch.cs            ← Forces 100% persuasion success
    │   └── DiagnosticHelper.cs           ← F11 runtime state dump (Hero + Agent + CombatCheats flags)
    └── UI/
        ├── HeroHackLayer.cs    ← GauntletLayer (order 200) for the main panel
        ├── HeroHackHudLayer.cs ← GauntletLayer (order 100) for the HUD button
        ├── HeroHackPanelVM.cs  ← Main ViewModel: IsOpen, tab switching, status bar
        ├── HeroHackHudVM.cs    ← HUD button ViewModel
        └── HeroTabVM.cs        ← Hero tab: all stat/skill/toggle bindings + companion navigation
```

## License

MIT
