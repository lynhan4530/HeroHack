# HeroHack Mod — Agent Handoff Prompt

## Project Overview

**HeroHack** is a cheat/quality-of-life mod for **Mount & Blade II: Bannerlord v1.3.15** (build 110062).  
It is a C# (.NET 4.7.2 / `net472`) mod compiled to:
```
g:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\HeroHack\
```

The mod injects a custom Gauntlet UI panel (toggled with **F10** on the campaign map) with multiple tabs: Combat, Party, and Settlement cheats.

**Build command** (run from the HeroHack module root):
```powershell
dotnet build -c Debug
```
Output DLL: `bin\Win64_Shipping_Client\HeroHack.dll`  
The build currently succeeds with **0 errors, 8 harmless CS8600/CS8604/CS8625 nullability warnings** (these are benign — the project targets `net472` which doesn't enforce nullable reference types at runtime).

---

## Complete File Structure

```
HeroHack\
├── HeroHack.csproj
├── SubModule.xml
├── README.md
├── reflect_sprint_b.ps1          ← reflection utility (can be deleted)
├── GUI\
│   └── Prefabs\
│       └── HeroHackPanel.xml     ← ALL UI layout (Gauntlet XML)
└── Source\
    ├── SubModule.cs              ← Entry point, Harmony init, F10 toggle
    ├── Cheats\
    │   ├── CombatCheats.cs       ← Static flags: IsPlayerImmortal, IsOneHitKill, etc.
    │   ├── PartyCheats.cs        ← Party cheat methods + Sprint B static flags
    │   └── SettlementCheats.cs   ← Settlement cheat methods
    ├── IO\
    │   └── (save/load helpers)
    ├── Patches\
    │   ├── HeroDeathPatch.cs          ← [HarmonyPatch] KillCharacterAction prefix
    │   ├── AgentMortalityPatch.cs     ← [HarmonyPatch] agent invulnerability
    │   ├── DamageModelPatch.cs        ← [HarmonyPatch] one-hit-kill
    │   ├── PersuasionPatch.cs         ← [HarmonyPatch] auto-win persuasion
    │   ├── HeroHackMissionBehavior.cs ← Mission behavior for combat cheats
    │   ├── DiagnosticHelper.cs        ← F11 debug dump
    │   ├── SpeedMultiplierPatch.cs    ← Sprint B: map speed patch (MANUAL registration)
    │   └── PartySizeOverridePatch.cs  ← Sprint B: party size patch (MANUAL registration)
    └── UI\
        ├── HeroHackLayer.cs           ← GauntletLayer wrapper
        ├── HeroHackHudLayer.cs        ← HUD button layer
        ├── HeroHackPanelVM.cs         ← Root panel VM (tab selection)
        ├── PartyTabVM.cs              ← Party tab VM (Sprint B bindings live here)
        ├── CombatTabVM.cs             ← Combat tab VM
        └── SettlementTabVM.cs         ← Settlement tab VM
```

---

## Architecture: How the UI Works

Bannerlord uses **Gauntlet** (custom MVVM UI framework):
- XML in `GUI/Prefabs/` defines layout. Widget properties bind to C# VM using `@PropertyName` syntax.
- `Command.Click="ExecuteMethodName"` calls a public `void ExecuteMethodName()` on the DataSource VM.
- `[DataSourceProperty]` attribute marks VM properties that Gauntlet can bind to.
- **CRITICAL**: Gauntlet XML is parsed at panel-open time (when the layer is injected into the MapScreen). XML parse errors or missing/wrong VM bindings cause an **immediate native crash (0xE0434352)** with no meaningful managed stack trace in the rgl logs.

`SubModule.cs` → `OnApplicationTick` → detects `MapScreen` → calls `InjectLayers()` → creates `HeroHackLayer` → loads `HeroHackPanel.xml` with `HeroHackPanelVM` as DataSource.

Tab VMs are children of `HeroHackPanelVM`:
- `PartyTab` → `PartyTabVM`
- `CombatTab` → `CombatTabVM`
- `SettlementTab` → `SettlementTabVM`

---

## Sprint B: What Was Implemented (Current State — NOT YET IN-GAME VALIDATED)

### Goal
Add two logistical cheats to the Party tab:
1. **Map Speed Multiplier**: `< 1x > … < 50x >` cycler — multiplies campaign map movement speed
2. **Party Size Override**: `< N >` cycler — overrides party size cap floor to at least N

### What Was Written

#### 1. `PartyCheats.cs` — Static Flags Added
```csharp
// At top of PartyCheats class:
public static float SpeedMultiplier { get; set; } = 1f;   // read by SpeedMultiplierPatch
public static int PartySizeOverride { get; set; } = 0;    // read by PartySizeOverridePatch
```

#### 2. `SpeedMultiplierPatch.cs` (NEW FILE)
```
Source/Patches/SpeedMultiplierPatch.cs
```
- **NO `[HarmonyPatch]` attribute** — registered manually via `ApplyPatch(harmony)`
- Patches: `DefaultPartySpeedCalculatingModel.CalculateFinalSpeed`
- **Actual confirmed signature** (via reflection): `ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)`
- Postfix: `ref ExplainedNumber __result` → calls `__result.AddFactor(SpeedMultiplier - 1f, null)`
- Only fires when `mobileParty == MobileParty.MainParty`

#### 3. `PartySizeOverridePatch.cs` (NEW FILE)
```
Source/Patches/PartySizeOverridePatch.cs
```
- **NO `[HarmonyPatch]` attribute** — registered manually via `ApplyPatch(harmony)`
- Patches: `DefaultPartySizeLimitModel.GetPartyMemberSizeLimit`
- **Actual confirmed signature** (via reflection): `ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions)`
- Postfix: `ref ExplainedNumber __result` → if `target > current`: `__result.Add(target - current, null, null)`
- **IMPORTANT**: `ExplainedNumber.Add()` takes **3 params**: `(float value, TextObject description, TextObject variable)` — passing 2 args causes a `MissingMethodException` crash.

#### 4. `SubModule.cs` — Manual Patch Registration

`OnSubModuleLoad` was updated to manually register the Sprint B patches with try/catch and error logging:
```csharp
try { SpeedMultiplierPatch.ApplyPatch(_harmony); }
catch (Exception ex) { LogPatchError("SpeedMultiplierPatch", ex); }

try { PartySizeOverridePatch.ApplyPatch(_harmony); }
catch (Exception ex) { LogPatchError("PartySizeOverridePatch", ex); }
```
If patch registration fails, the error is written to `Documents\HeroHack\patch_errors.txt`.

> **NOTE**: The other patches (`HeroDeathPatch`, `AgentMortalityPatch`, etc.) still use `[HarmonyPatch]` attributes and are auto-discovered by `_harmony.PatchAll(Assembly.GetExecutingAssembly())`. Only the Sprint B patches use manual registration.

#### 5. `PartyTabVM.cs` — New VM Properties & Commands

Private fields added:
```csharp
private int _speedMultiplierInt = 1;   // 1..50
private int _partySizeOverrideInt = 0; // 0 = uninitialised; auto-set from actual cap on first RefreshDisplay
private string _actualPartySizeCapText = "--";
```

Public properties (all `[DataSourceProperty]`):
- `string SpeedMultiplierText` → returns `$"{_speedMultiplierInt}x"` (e.g. `"5x"`)
- `string PartySizeOverrideText` → returns override value or `"--"` if not yet initialized
- `string ActualPartySizeCapText` → shows the vanilla game limit for reference

Execute methods:
- `ExecuteSpeedDown()` / `ExecuteSpeedUp()` — step speed by 1, range 1–50
- `ExecutePartySizeDown()` / `ExecutePartySizeUp()` — step party cap by ±10, range 1–9999

`RefreshDisplay()` auto-initializes `_partySizeOverrideInt` from `party.Party?.PartySizeLimit` on first open.

#### 6. `HeroHackPanel.xml` — Two New UI Rows Added to Party Tab

Inserted **ABOVE** "Row 2: Add Food" (around line 397):
```xml
<!-- Row S1: Speed Multiplier -->
<ListPanel ...>
  <TextWidget Text="Map Speed:" ... />
  <ButtonWidget Command.Click="ExecuteSpeedDown" ...> &lt; </ButtonWidget>
  <TextWidget Text="@SpeedMultiplierText" ... Color="#6BFF8FFF" />
  <ButtonWidget Command.Click="ExecuteSpeedUp" ...> &gt; </ButtonWidget>
</ListPanel>

<!-- Row S2: Party Size Override -->
<ListPanel ...>
  <TextWidget Text="Party Cap:" ... />
  <TextWidget Text="(base:" ... />
  <TextWidget Text="@ActualPartySizeCapText" ... Color="#CFB53BFF" />
  <TextWidget Text=")" ... />
  <ButtonWidget Command.Click="ExecutePartySizeDown" ...> &lt; </ButtonWidget>
  <TextWidget Text="@PartySizeOverrideText" ... Color="#6BFF8FFF" />
  <ButtonWidget Command.Click="ExecutePartySizeUp" ...> &gt; </ButtonWidget>
</ListPanel>
```

> **WHY NOT SLIDERS**: A native Gauntlet `SliderWidget` with `ValueInt="@SpeedMultiplierInt"` was tried first but caused a **crash 33 seconds into load** (at MapScreen activate time). The issue is that `SliderWidget` in the campaign Gauntlet context cannot two-way bind to a DataSource `int` property the way battle/options screens can. The cycler button pattern is safe and consistent with the rest of the panel.

---

## Known API Gotchas (Confirmed via Reflection on v1.3.15)

| API | Actual Signature | Common Mistake |
|---|---|---|
| `ExplainedNumber.Add` | `void Add(float value, TextObject description, TextObject variable)` | Calling with 2 args crashes |
| `ExplainedNumber.AddFactor` | `void AddFactor(float value, TextObject description)` | OK with `null` description |
| `CalculateFinalSpeed` | `ExplainedNumber CalculateFinalSpeed(MobileParty, ExplainedNumber)` | NOT `float` return |
| `GetPartyMemberSizeLimit` | `ExplainedNumber GetPartyMemberSizeLimit(PartyBase, bool)` | NOT `int` return |
| `SliderWidget.ValueInt` | Cannot bind to DataSource `int` property in campaign context | Use cycler buttons instead |

---

## Current Status

| Item | Status |
|---|---|
| Build | ✅ Succeeds (0 errors) |
| SpeedMultiplierPatch | ✅ Written, builds, NOT yet in-game tested |
| PartySizeOverridePatch | ✅ Written, builds, NOT yet in-game tested |
| XML (cycler rows) | ✅ Written, XML validates, NOT yet in-game tested |
| VM bindings | ✅ Written, compiles, NOT yet in-game tested |
| `patch_errors.txt` | 🔄 Diagnostic file — will be written to `Documents\HeroHack\` if patch registration fails |

---

## What The Next Agent Should Do

1. **Test in-game** — load a save, open the HeroHack panel (F10), switch to Party tab
2. Verify **Speed row** appears: `Map Speed: < 1x >`
3. Verify **Party Cap row** appears: `Party Cap: (base: N) < M >`
4. Click `>` on speed — confirm your character moves faster on the map
5. Click `>` on party cap — confirm you can recruit more troops than the vanilla limit

### If there's still a crash:
- Check `Documents\HeroHack\patch_errors.txt` for the full exception
- Check the latest crash folder in `C:\ProgramData\Mount and Blade II Bannerlord\crashes\` for rgl logs
- The App Run Time in `crash_tags.txt` tells you **when** — <30s = XML parse crash, >60s = runtime crash

### After validation, commit and push:
```powershell
git add -A
git commit -m "feat: Sprint B - map speed multiplier + party size override via Harmony postfix patches"
git push
```

### Then update README.md in the HeroHack module root to document:
- Map Speed Multiplier (Party tab, 1x–50x, `<`/`>` steps by 1)
- Party Size Override (Party tab, shows base limit, steps by ±10)

---

## Key Design Principles (Follow These)

1. **Never crash the game** — all patches have try/catch with silent fallthrough
2. **Guard for MainParty** — always check `mobileParty == MobileParty.MainParty` before applying cheat effects
3. **XML-safe strings** — use `&lt;` and `&gt;` in XML for `<` and `>`; never raw angle brackets in attribute values
4. **`[DataSourceProperty]` is mandatory** — any property bound in XML must have this attribute or Gauntlet will ignore it
5. **No SliderWidget in campaign context** — use `< value >` cycler buttons instead
6. **Manual Harmony registration for new patches** — use `ApplyPatch(harmony)` pattern with try/catch rather than `[HarmonyPatch]` attributes, so failures are catchable at startup

---

## Game Version & Environment

- Bannerlord: `v1.3.15.110062`
- Framework: `net472` (Mono, NOT .NET Core)
- Harmony: `Bannerlord.Harmony v2.4.2.0` (workshop mod)
- Game DLLs at: `g:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\`
- Module path: `g:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\HeroHack\`
