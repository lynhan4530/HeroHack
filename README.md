# HeroHack

A cheat/developer panel mod for **Mount & Blade II: Bannerlord v1.3.x**.  
Press **F10** on the campaign map (or click the **HH** button in the top-right corner) to open the panel.

---

## Features (planned — implemented by phase)

| Phase | Status | Feature |
|-------|--------|---------|
| 1 | ✅ Done | Panel opens/closes via F10 and HUD button. Tab bar switches between Hero / Party / Settlement / Export-Import tabs. |
| 2 | 🔜 Next | Hero stat editing — Gold, Renown, Influence, Attributes, Skills, Level. Hero selector (player + companions). |
| 3 | 🔜 | Combat toggles — Invulnerability, One-Hit Kill, Immortality, Persuasion Auto-Win (Harmony patches). |
| 4 | 🔜 | Party cheats — Add troops, max morale, add food, heal all. Settlement cheats — Prosperity, Loyalty, Security, Garrison. |
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
│       ├── HeroHackPanel.xml   ← Main cheat panel (900×700, tabbed)
│       └── HeroHackHud.xml     ← Persistent HH button (top-right map HUD)
└── Source/
    ├── SubModule.cs            ← Entry point: Harmony init, layer injection, F10 hotkey
    └── UI/
        ├── HeroHackLayer.cs    ← GauntletLayer (order 200) for the main panel
        ├── HeroHackHudLayer.cs ← GauntletLayer (order 100) for the HUD button
        ├── HeroHackPanelVM.cs  ← Main ViewModel: IsOpen, tab switching, status bar
        └── HeroHackHudVM.cs    ← HUD button ViewModel
```

## License

MIT
