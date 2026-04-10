# HeroHack

A cheat/developer panel mod for **Mount & Blade II: Bannerlord v1.3.x**.  
Press **F10** on the campaign map (or click the **HH** button in the top-right corner) to open the panel.

---

## Features

| Phase | Status | Feature |
|-------|--------|---------|
| 1 | ✅ Done | Panel opens/closes via F10 and HUD button. Tab bar switches between Hero / Party / Settlement / Export-Import tabs. |
| 2 | ✅ Done | Hero stat editing — Gold, Renown, Influence, Attributes, Skills (all 18), Level. Hero selector (player + companions). Combat toggles (Invulnerable, Immortal, Persuasion AutoWin). |
| 3 | ✅ Done | Harmony patches — Invulnerability (HP clamp via MissionBehavior), One-Hit Kill (OnAgentHit), Immortality (blocks KillCharacterAction), Persuasion Auto-Win (DefaultPersuasionModel postfix). |
| 4 | ✅ Done | Party cheats — Boost morale, add food, heal all wounded, add troops. Settlement cheats — Prosperity, Loyalty, Security, fill garrison, max food stocks. |
| 5 | ✅ Done | XML Export / Import — full hero profile serialised to `Documents\HeroHack\exports\`. Covers identity, attributes, all 18 skills + focus, perks, traits, Battle/Stealth/Civilian equipment (all 12 slots each). Two-step import with confirm flow. |
| 6 | ✅ Done | Polish — UI alignment overhauls, error handling, status messages, final QA. |
| 7 | ✅ Done | Sprint A Expansion — Auto-promote injects XP into branching paths or resolves directly. Mount Hoarder fixes for herd-penalty validation. Advanced Custom Database Unit Spawner. Smart Upgrade Mount Provider calculates branching cavalry horse deficits exactly. |
| 8 | ✅ Done | Basic Spawn Culture Selection — Row 3 now has a `< Culture >` cycler (Player / Empire / Vlandia / Battania / Khuzait / Aserai / Sturgia). `AddTroops` backend refactored to resolve `CultureObject` by name from `ObjectManager`, falling back to player culture. |
| B | ✅ Done | Sprint B — Map speed multiplier slider (1×–50×, Harmony postfix on `CalculateFinalSpeed`), Party size override slider (Harmony postfix on `PartySizeLimit`), Recruit Prisoners one-click, Elite Troop Spawner with faction / class / tier filters. |
| C | ✅ Done | Sprint C — Remove Disorganized (reflection call to `SetDisorganized(false)`), Instant Siege Prep (sets all active siege engine construction progress to 100%). |

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
│       ├── HeroHackPanel.xml   ← Main cheat panel (920×1010, 4 tabs)
│       └── HeroHackHud.xml     ← Persistent HH button (top-right map HUD)
└── Source/
    ├── SubModule.cs            ← Entry point: Harmony init, layer injection, F10 hotkey
    ├── Cheats/
    │   ├── HeroCheats.cs       ← Hero stat helpers: SetSkill, SetGold, SetLevel, etc.
    │   ├── PartyCheats.cs      ← Party helpers: BoostMorale, AddFood, HealAllWounded, AddTroops
    │   ├── SettlementCheats.cs ← Settlement helpers: Prosperity, Loyalty, Security, Garrison, Food
    │   └── CombatCheats.cs     ← Shared static flags read by patches/behaviors
    ├── IO/
    │   ├── HeroExporter.cs     ← Serialize Hero → XML (identity, attrs, skills, perks, traits, equipment)
    │   └── HeroImporter.cs     ← Parse XML → apply to Hero with ImportResult warnings
    ├── Patches/
    │   ├── HeroHackMissionBehavior.cs    ← HP clamp (invulnerable) + One-Hit Kill via OnAgentHit
    │   ├── HeroDeathPatch.cs             ← Blocks KillCharacterAction for player when Immortal is ON
    │   ├── PersuasionPatch.cs            ← Forces 100% persuasion success
    │   ├── SpeedMultiplierPatch.cs       ← Harmony postfix on CalculateFinalSpeed; reads PartyCheats.SpeedMultiplier
    │   ├── PartySizeOverridePatch.cs     ← Harmony postfix on PartySizeLimit; reads PartyCheats.PartySizeOverride
    │   ├── DamageModelPatch.cs           ← One-hit kill via damage model override
    │   ├── AgentMortalityPatch.cs        ← (DISABLED) Kept as documentation: causes ragdoll bug
    │   └── DiagnosticHelper.cs           ← F11 runtime state dump
    └── UI/
        ├── HeroHackLayer.cs       ← GauntletLayer (order 200) for the main panel
        ├── HeroHackHudLayer.cs    ← GauntletLayer (order 100) for the HUD button
        ├── HeroHackPanelVM.cs     ← Main VM: IsOpen, tab switching, status bar
        ├── HeroHackHudVM.cs       ← HUD button VM
        ├── HeroTabVM.cs           ← Hero tab: stats/skills/toggles + companion nav
        ├── PartyTabVM.cs          ← Party tab: morale/food/heal/troops display + actions
        ├── SettlementTabVM.cs     ← Settlement tab: prosperity/loyalty/etc display + actions
        ├── IOTabVM.cs             ← Export/Import tab: file nav, hero nav, two-step confirm
        └── HeroSelectorVM.cs      ← Reusable hero picker item VM
```

## Export / Import Format

Exports are saved to `%USERPROFILE%\Documents\HeroHack\exports\` as XML files named:  
`<HeroName>_<yyyy-MM-dd>_<HH-mm>.xml`

The XML schema (`schema_version="1"`) covers:
- **Identity** — string_id, name, culture, clan, age, level, gold, renown
- **Attributes** — all 6 (Vigor, Control, Endurance, Cunning, Social, Intelligence)
- **Skills** — all 18 with focus level
- **Perks** — active perks only (import is additive)
- **Traits** — non-zero traits only
- **BattleEquipment**, **StealthEquipment**, **CivilianEquipment** — all 12 slots each

## License

MIT
