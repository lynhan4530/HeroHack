# HeroHack — GitHub Copilot Master Prompt
> **Role Assignment**: You are a Senior C# Engineer with deep expertise in Bannerlord modding (TaleWorlds API, Harmony patching, and Gauntlet UI). You will implement the **HeroHack** mod for Mount & Blade II: Bannerlord version **1.3.x** step-by-step, following this specification exactly.

---

## 📐 Project Context

- **Mod Name**: HeroHack
- **Target Game Version**: Mount & Blade II: Bannerlord `v1.3.x`
- **Game Install Path**: `G:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`
- **Mod Path**: `[GameInstall]\Modules\HeroHack\`
- **Runtime**: `.NET 6.0`, `x64`, `win-x64`
- **Language**: `C# 10`
- **No MCM dependency** — All configuration is done inside the mod's own panel
- **No UIExtenderEx** — Panel injected directly via `GauntletLayer`
- **Harmony** is the only external dependency (for game method patching)

---

## 🗂️ Project File Structure (Complete)

```
HeroHack/
├── HeroHack.csproj
├── SubModule.xml
├── .gitignore
├── GUI/
│   └── Prefabs/
│       └── HeroHackPanel.xml         ← Gauntlet UI definition
├── ModuleData/
│   └── (empty for now)
├── Source/
│   ├── SubModule.cs                  ← Mod entry point
│   │
│   ├── UI/
│   │   ├── HeroHackPanelVM.cs        ← Main ViewModel (tabs, state)
│   │   ├── HeroHackLayer.cs          ← GauntletLayer injected into MapScreen
│   │   ├── HeroTabVM.cs              ← Sub-ViewModel: Hero tab
│   │   ├── PartyTabVM.cs             ← Sub-ViewModel: Party tab
│   │   ├── SettlementTabVM.cs        ← Sub-ViewModel: Settlement tab
│   │   ├── IOTabVM.cs                ← Sub-ViewModel: Export/Import tab
│   │   └── HeroSelectorVM.cs         ← Reusable hero picker dropdown VM
│   │
│   ├── Cheats/
│   │   ├── HeroCheats.cs             ← Hero stat manipulation methods
│   │   ├── PartyCheats.cs            ← Party/Army manipulation methods
│   │   ├── SettlementCheats.cs       ← Town/Castle manipulation methods
│   │   └── CombatCheats.cs           ← One-hit kill, invulnerability toggle state
│   │
│   ├── Patches/
│   │   ├── InvulnerabilityPatch.cs   ← Harmony: nullify player damage taken
│   │   ├── OneHitKillPatch.cs        ← Harmony: max damage on player attack
│   │   ├── ImmortalHeroPatch.cs      ← Harmony: prevent hero aging/death
│   │   └── PersuasionPatch.cs        ← Harmony: auto-succeed persuasion
│   │
│   └── IO/
│       ├── HeroExporter.cs           ← Serialize Hero → XML
│       ├── HeroImporter.cs           ← Parse XML → apply to Hero
│       └── HeroXmlSchema.cs          ← Schema model classes (POCOs)
│
└── bin/
    └── Win64_Shipping_Client/
        └── HeroHack.dll              ← Build output
```

---

## 1. SubModule.xml — Mod Manifest

**File**: `SubModule.xml`

```xml
<Module>
  <Name value="HeroHack"/>
  <Id value="HeroHack"/>
  <Version value="v1.0.0"/>
  <SingleplayerModule value="true"/>
  <MultiplayerModule value="false"/>
  <DependedModules>
    <DependedModule Id="Native"/>
    <DependedModule Id="SandBoxCore"/>
    <DependedModule Id="Sandbox"/>
    <DependedModule Id="CustomBattle"/>
    <DependedModule Id="StoryMode"/>
  </DependedModules>
  <SubModules>
    <SubModule>
      <Name value="HeroHack"/>
      <DLLName value="HeroHack.dll"/>
      <SubModuleClassType value="HeroHack.SubModule"/>
      <Tags>
        <Tag key="DedicatedServerType" value="none"/>
        <Tag key="IsClientOnly" value="false"/>
      </Tags>
    </SubModule>
  </SubModules>
  <Xmls/>
</Module>
```

---

## 2. HeroHack.csproj — Project File

**Requirements**:
- Target `net6.0`, platform `x64`
- Reference all `TaleWorlds.*.dll` from the game's `bin\Win64_Shipping_Client\` using a glob
- Reference `0Harmony.dll` from the game's `bin\Win64_Shipping_Client\`
- All references must be `Private="false"` (do NOT copy DLLs to output)
- Output must go to `bin\Win64_Shipping_Client\`
- Add a post-build event to copy the output DLL to the game's mod folder (optional convenience)

**Property `GameFolder`** must be configurable, default: `G:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`

---

## 3. SubModule.cs — Entry Point

**Namespace**: `HeroHack`
**Class**: `SubModule : MBSubModuleBase`

### Responsibilities

1. In `OnSubModuleLoad()`:
   - Initialize Harmony with ID `"com.herophack.mod"`
   - Call `harmony.PatchAll(Assembly.GetExecutingAssembly())`
   - Store Harmony instance as a `private static Harmony _harmony`

2. In `OnGameStart(Game game, IGameStarter gameStarter)`:
   - Check `game.GameType is Campaign`
   - Add `HeroHackCampaignBehavior` to the game starter (if you add any campaign behaviors — not strictly needed in Phase 1 but scaffold it)

3. In `OnApplicationTick(float dt)`:
   - Check if current screen is `MapScreen`
   - If yes, and if `HeroHackLayer` has not been injected yet, inject it
   - If screen changes away from `MapScreen`, remove the layer

4. In `OnSubModuleUnloaded()`:
   - Call `_harmony.UnpatchAll("com.herophack.mod")`
   - Clean up the layer

5. Handle the hotkey `F10`:
   - In `OnApplicationTick(float dt)`, listen for `Input.IsKeyPressed(InputKey.F10)` 
   - If pressed while on the `MapScreen`, toggle `HeroHackLayer.IsVisible`

### Key APIs
```csharp
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;
using HarmonyLib;
```

---

## 4. UI Layer — HeroHackLayer.cs

**Namespace**: `HeroHack.UI`
**Class**: `HeroHackLayer : GauntletLayer`

### Behavior
- Extends `GauntletLayer` with a unique layer order: `LayerOrder = 200`
- Holds a reference to `HeroHackPanelVM _panelVM`
- On construction: instantiate `_panelVM`, load the Gauntlet movie `HeroHackPanel`
  ```csharp
  LoadMovie("HeroHackPanel", _panelVM);
  ```
- Expose a public `bool IsVisible` property that shows/hides the panel by toggling `_panelVM.IsOpen`
- The `HeroHackLayer` is added to `ScreenManager.TopScreen` (the MapScreen) via:
  ```csharp
  MapScreen.AddLayer(this);
  ```

---

## 5. ViewModel — HeroHackPanelVM.cs

**Namespace**: `HeroHack.UI`
**Class**: `HeroHackPanelVM : ViewModel`

### Data-Bound Properties (all use `[DataSourceProperty]`)

```csharp
[DataSourceProperty] public bool IsOpen { get; set; }             // Panel visibility
[DataSourceProperty] public int ActiveTabIndex { get; set; }       // 0=Hero, 1=Party, 2=Settlement, 3=IO
[DataSourceProperty] public HeroTabVM HeroTab { get; set; }
[DataSourceProperty] public PartyTabVM PartyTab { get; set; }
[DataSourceProperty] public SettlementTabVM SettlementTab { get; set; }
[DataSourceProperty] public IOTabVM IOTab { get; set; }
[DataSourceProperty] public string StatusMessage { get; set; }     // Feedback line at bottom of panel
```

### Methods
- `ExecuteClose()` — sets `IsOpen = false`
- `ExecuteSelectTab(int index)` — sets `ActiveTabIndex = index`
- `SetStatus(string msg)` — sets `StatusMessage`, optionally clears after 3 seconds using a timer in `OnTick(float dt)`
- Override `OnTick(float dt)` to support the status clear timer

### Architecture Note
Each tab VM is self-contained. The parent VM only owns the tab references and the status bar. Tabs call back to parent via a passed `Action<string> onStatusUpdate` delegate.

---

## 6. HeroTabVM.cs — Hero Cheat Tab

**Namespace**: `HeroHack.UI`
**Class**: `HeroTabVM : ViewModel`

### Constructor
```csharp
public HeroTabVM(Action<string> onStatusUpdate)
```

### Hero Selector
- `[DataSourceProperty] public MBBindingList<HeroSelectorItemVM> AvailableHeroes { get; set; }`
- `[DataSourceProperty] public HeroSelectorItemVM SelectedHero { get; set; }`
- On initialization, populate `AvailableHeroes` with:
  - The **player hero**: `Hero.MainHero`
  - All **companions** in the player's party: `MobileParty.MainParty.MemberRoster` filtered by `Hero != null && Hero.IsActive && Hero.IsPlayerCompanion`
- When `SelectedHero` changes (via `OnHeroSelected(HeroSelectorItemVM item)`), refresh all displayed stat values from the newly selected `Hero`

### Stat Properties (each paired with a value and an apply command)
| Property | Type | Bannerlord Field |
|---|---|---|
| `GoldValue` | `int` (slider 0–9,999,999) | `Hero.Gold` |
| `RenownValue` | `int` (slider 0–10,000) | `Hero.Renown` |
| `InfluenceValue` | `int` (slider 0–10,000) | `Clan.Influence` (player clan) |
| `VigorValue` | `int` (slider 1–10) | `hero.GetAttributeValue(CharacterAttributesEnum.Vigor)` |
| `ControlValue` | `int` | `CharacterAttributesEnum.Control` |
| `EnduranceValue` | `int` | `CharacterAttributesEnum.Endurance` |
| `CunningValue` | `int` | `CharacterAttributesEnum.Cunning` |
| `SocialValue` | `int` | `CharacterAttributesEnum.Social` |
| `IntelligenceValue` | `int` | `CharacterAttributesEnum.Intelligence` |
| `HeroLevelValue` | `int` (slider 1–62) | `HeroDeveloper.SetInitialLevel(n)` |

### Skill Properties
For each of the 18 skills (e.g., OneHanded, TwoHanded, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Crafting, Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering):
- `[DataSourceProperty] public int SkillOneHandedValue { get; set; }` (0-330)

### Toggle Properties
- `[DataSourceProperty] public bool IsInvulnerable { get; set; }` — set via `CombatCheats.IsPlayerInvulnerable`
- `[DataSourceProperty] public bool IsImmortal { get; set; }` — set via `CombatCheats.IsPlayerImmortal`
- `[DataSourceProperty] public bool IsPersuasionAutoWin { get; set; }` — set via `CombatCheats.IsPersuasionAutoWin`

### Execute Methods (each one updates the game and calls `onStatusUpdate`)
```csharp
[DataSourceMethod] public void ExecuteApplyGold()
[DataSourceMethod] public void ExecuteApplyRenown()
[DataSourceMethod] public void ExecuteApplyInfluence()
[DataSourceMethod] public void ExecuteApplyAttributes()  // applies all 6 attributes at once
[DataSourceMethod] public void ExecuteMaxAllSkills()
[DataSourceMethod] public void ExecuteApplyLevel()
[DataSourceMethod] public void ExecuteToggleInvulnerable()
[DataSourceMethod] public void ExecuteToggleImmortal()
[DataSourceMethod] public void ExecuteTogglePersuasion()
[DataSourceMethod] public void ExecuteAddAttributePoint()  // +1 attribute point to selected hero
[DataSourceMethod] public void ExecuteAddFocusPoint()      // +1 focus point to selected hero
```

---

## 7. PartyTabVM.cs — Party Cheat Tab

**Class**: `PartyTabVM : ViewModel`

### Scope
Always targets `MobileParty.MainParty`.

### Properties
- `TroopTypeId` — `string` (text input for troop StringId, e.g. `"imperial_recruit"`)
- `TroopAddAmount` — `int` (slider 1–500)
- `FoodAmount` — `int` (slider 0–1000)
- `MoraleValue` — `int` (slider 0–100)
- `HealAllEnabled` — always shows as a button, not a toggle

### Execute Methods
```csharp
[DataSourceMethod] public void ExecuteAddTroops()
// Uses: MobileParty.MainParty.MemberRoster.AddToCounts(
//           CharacterObject.Find(TroopTypeId), TroopAddAmount)

[DataSourceMethod] public void ExecuteMaxMorale()
// Uses: MobileParty.MainParty.RecentEventsMorale = 100f (or inject via EventList)

[DataSourceMethod] public void ExecuteAddFood()
// Finds ItemObject with food tag, adds to MobileParty.MainParty.ItemRoster

[DataSourceMethod] public void ExecuteHealAllTroops()
// Iterates MobileParty.MainParty.MemberRoster, sets each element WoundedNumber to 0
```

---

## 8. SettlementTabVM.cs — Settlement Cheat Tab

**Class**: `SettlementTabVM : ViewModel`

### Scope
Targets the currently selected settlement on the campaign map if the player is in a settlement, or provides a dropdown of player-owned settlements.

### Properties
- `AvailableSettlements` — `MBBindingList<SettlementSelectorItemVM>` — all settlements owned by `Clan.PlayerClan`
- `SelectedSettlement` — `SettlementSelectorItemVM`
- `ProsperityValue` — `float` (0–50,000)
- `LoyaltyValue` — `float` (0–100)
- `SecurityValue` — `float` (0–100)
- `GarrisonGoldValue` — `int` (gold to add to town)
- `GarrisonTroopId` — `string` (troop ID to add to garrison)
- `GarrisonTroopAmount` — `int`

### Execute Methods
```csharp
[DataSourceMethod] public void ExecuteApplyProsperity()
// SelectedSettlement.Town.Prosperity = ProsperityValue

[DataSourceMethod] public void ExecuteApplyLoyalty()
// SelectedSettlement.Town.Loyalty = LoyaltyValue

[DataSourceMethod] public void ExecuteApplyGold()
// SelectedSettlement.Town.ChangeGold(delta)

[DataSourceMethod] public void ExecuteAddGarrisonTroops()
// SelectedSettlement.Town.GarrisonParty.AddElementToRoster(...)
```

---

## 9. IOTabVM.cs — Export / Import Tab

**Class**: `IOTabVM : ViewModel`

This is the most complex tab. It handles full hero profile serialization.

### Export Side
```
[Hero Picker: player + companions]
[Button: Export to XML]
→ Writes to: %USERPROFILE%\Documents\HeroHack\exports\<HeroName>_<CampaignDay>.xml
→ Shows "Exported to: <path>" in status
```

### Import Side
```
[Dropdown: list of .xml files in Documents\HeroHack\exports\]
[Button: Refresh file list]
[Hero Picker: who to apply it to]
[Button: Import]
→ Confirmation dialog: "This will overwrite <hero>'s stats and equipment. Continue?"
→ Applies if confirmed
```

### Properties
```csharp
[DataSourceProperty] public MBBindingList<HeroSelectorItemVM> ExportableHeroes { get; set; }
[DataSourceProperty] public HeroSelectorItemVM SelectedExportHero { get; set; }
[DataSourceProperty] public MBBindingList<string> AvailableExportFiles { get; set; }
[DataSourceProperty] public string SelectedImportFile { get; set; }
[DataSourceProperty] public MBBindingList<HeroSelectorItemVM> ImportTargetHeroes { get; set; }
[DataSourceProperty] public HeroSelectorItemVM SelectedImportTarget { get; set; }
[DataSourceProperty] public bool ShowImportConfirm { get; set; }
```

### Execute Methods
```csharp
[DataSourceMethod] public void ExecuteExport()
[DataSourceMethod] public void ExecuteRefreshFileList()
[DataSourceMethod] public void ExecuteImportConfirm()   // shows the confirm prompt
[DataSourceMethod] public void ExecuteImportApply()     // actual import after confirm
[DataSourceMethod] public void ExecuteImportCancel()    // hides confirm prompt
```

---

## 10. HeroExporter.cs

**Namespace**: `HeroHack.IO`
**Class**: `HeroExporter`

### Method Signature
```csharp
public static string Export(Hero hero, string outputDirectory)
```

### What to Serialize (Full Schema)

**Identity block**:
- `Hero.StringId`, `Hero.Name.ToString()`, `Hero.Culture.StringId`, `Hero.Clan?.StringId`, `Hero.Age`

**Attributes block** — iterate `CharacterAttributesEnum` (all 6):
```csharp
hero.GetAttributeValue(CharacterAttributesEnum.Vigor) // etc.
```

**Skills block** — iterate all 18 `DefaultSkills` static properties:
```csharp
hero.GetSkillValue(DefaultSkills.OneHanded) // etc.
```
Also persist `hero.HeroDeveloper.GetFocusValue(skill)` per skill (0–5).

**Perks block** — iterate `hero.GetPerkValue(perk)` for all perks (using `MBObjectManager.Instance.GetObjectTypeList<PerkObject>()`)

**Traits block** — iterate `TraitObject.All`:
```csharp
hero.GetTraitLevel(trait)
```

**Personality traits block** — iterate `HeroHelper.GetChartraits` or use known trait IDs: `Valor`, `Generosity`, `Honor`, `Mercy`, `Calculating`

**Equipment block** — two sets: `BattleEquipment` and `CivilianEquipment`:
```csharp
// For each EquipmentIndex (0–11 for battle, 0–11 for civilian):
Equipment[index].Item?.StringId
Equipment[index].ItemModifier?.StringId
```

**Gold**: `hero.Gold`
**Renown**: `hero.Renown`
**Level**: `hero.Level`

### Output Format (XML)
```xml
<?xml version="1.0" encoding="utf-8"?>
<HeroExport schema_version="1" game_version="1.3.x" export_date="2026-04-03T23:00:00">
  <Identity string_id="lord_1_1" name="Ira" culture="empire" clan="player_clan" age="25" level="12" gold="50000" renown="340"/>
  <Attributes>
    <Attr id="Vigor" value="5"/>
    <Attr id="Control" value="4"/>
    <Attr id="Endurance" value="5"/>
    <Attr id="Cunning" value="4"/>
    <Attr id="Social" value="4"/>
    <Attr id="Intelligence" value="5"/>
  </Attributes>
  <Skills>
    <Skill id="OneHanded" value="200" focus="5"/>
    <Skill id="TwoHanded" value="0" focus="0"/>
    <!-- ... all 18 skills ... -->
  </Skills>
  <Perks>
    <Perk id="OneHandedBasher" active="true"/>
    <!-- only perks that are TRUE -->
  </Perks>
  <Traits>
    <Trait id="Honor" value="2"/>
    <!-- ... all non-zero traits ... -->
  </Traits>
  <BattleEquipment>
    <Slot index="0" item_id="iron_sword_t3" modifier=""/>
    <Slot index="1" item_id="" modifier=""/>
    <!-- all 12 slots, empty string if none -->
  </BattleEquipment>
  <CivilianEquipment>
    <Slot index="0" item_id="noble_civilian_outfit" modifier=""/>
    <!-- all 12 slots -->
  </CivilianEquipment>
</HeroExport>
```

### File Naming
```
<HeroStringId>_day<CampaignDayAsInt>_<timestamp>.xml
Example: lord_1_1_day120_20260403T235900.xml
```

### Output Directory
```csharp
string dir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "HeroHack", "exports");
Directory.CreateDirectory(dir);
```

---

## 11. HeroImporter.cs

**Namespace**: `HeroHack.IO`
**Class**: `HeroImporter`

### Method Signature
```csharp
public static ImportResult Import(string xmlFilePath, Hero targetHero)
```

### Return Type
```csharp
public class ImportResult {
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<string> Warnings { get; set; } = new();
}
```

### Import Steps (in order)

1. **Validate XML** — parse with `XDocument.Load()`, verify `schema_version="1"` attribute exists. Return failure if malformed.

2. **Apply Attributes** — for each `<Attr>`:
   ```csharp
   int current = hero.GetAttributeValue(attr);
   int delta = targetValue - current;
   if (delta > 0) hero.HeroDeveloper.AddAttribute(attr, delta, checkUnspentPoints: false);
   ```
   If `delta < 0`: set directly via reflection on `Hero._heroAttributes` backing field (note: no public setter in v1.3.x — use property with care or clamp to current).

3. **Apply Skills** — for each `<Skill>`:
   ```csharp
   hero.HeroDeveloper.ChangeSkillLevel(skill, targetValue, shouldUpdateSkillStates: false);
   hero.HeroDeveloper.SetFocus(skill, focusValue);
   ```

4. **Apply Level** — if level differs, call `HeroDeveloper.SetInitialLevel(level)` or add XP to reach level.

5. **Apply Gold** — `GiveGoldAction.ApplyBetweenCharacters(null, hero, targetGold - hero.Gold)`

6. **Apply Renown** — direct: `hero.Clan?.AddRenown(delta)` or `hero.SetPersonalProperty(...)`.

7. **Apply Perks** — for each `<Perk active="true">`:
   ```csharp
   PerkObject perk = MBObjectManager.Instance.GetObject<PerkObject>(perkId);
   if (perk != null && !hero.GetPerkValue(perk))
       hero.SetPerkValue(perk, true);
   ```

8. **Apply Traits** — for each `<Trait>`:
   ```csharp
   TraitObject trait = TraitObject.All.FirstOrDefault(t => t.StringId == id);
   if (trait != null) hero.SetTraitLevel(trait, value);
   ```

9. **Apply Equipment** — for each slot in `BattleEquipment` and `CivilianEquipment`:
   ```csharp
   ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(item_id);
   EquipmentElement element = item != null ? new EquipmentElement(item) : EquipmentElement.Invalid;
   hero.BattleEquipment[index] = element;
   // Same for CivilianEquipment
   ```

10. **Return** `ImportResult { Success = true, Message = "Import successful. X warnings." }`

### Error Handling Rules
- **Never throw** — always catch and add to `Warnings`
- If an item ID is not found in `MBObjectManager` → add warning, skip that slot
- If a skill value exceeds the game's cap (330) → clamp to 330, add warning
- If attribute total would exceed character level constraints → clamp, add warning

---

## 12. Harmony Patches

### InvulnerabilityPatch.cs
```csharp
// Target: MissionCombatMechanicsHelper.GetBlowMagnitudeAdaptedToPlayerDifficulty
// Or patch: Agent.ApplyDamage / RegisterBlow
// Static flag: CombatCheats.IsPlayerInvulnerable (bool)
// Postfix: if (IsPlayerInvulnerable && __instance is Agent a && a.IsPlayerControlled) blow.InflictedDamage = 0;
```

**Exact Patch Target** for v1.3.x:
```csharp
[HarmonyPatch(typeof(Mission), "GetAttackCollisionResults")]
// OR patch Agent.RegisterBlow — prefix to set damage to 0 if target is player hero
```

**Recommended approach** (safer):
```csharp
[HarmonyPatch(typeof(AgentApplyDamageModel), "DecideAgentSuppressedByBlow")]
// This exists in v1.3.x
```
Investigate the actual `AgentApplyDamageModel` chain. The most reliable patch for invulnerability in v1.3.x is:
```csharp
// Patch DefaultAgentApplyDamageModel.DecideCrushedThrough or similar
// Apply the shield of: if target == Hero.MainHero → set inflictedDamage = 0 before applying
```

> ⚠️ **Important**: Use a **Prefix** returning `false` to completely skip damage calculation for the player.

### OneHitKillPatch.cs
Static flag: `CombatCheats.IsOneHitKill`

Target the same damage pipeline — if the **attacker** is the player's agent, set `blow.InflictedDamage = 999999`.

### ImmortalHeroPatch.cs
```csharp
[HarmonyPatch(typeof(KillCharacterAction), "ApplyInner")]
static bool Prefix(Hero victim)
{
    if (CombatCheats.IsPlayerImmortal && victim == Hero.MainHero) return false; // skip death
    return true;
}
```

Also patch aging:
```csharp
[HarmonyPatch(typeof(AgingCampaignBehavior), "MakeOldHeroDie")]
// If IsPlayerImmortal, skip for player hero
```

### PersuasionPatch.cs
```csharp
[HarmonyPatch(typeof(PersuasionTask), "PersuasionChance", MethodType.Getter)]
static void Postfix(ref float __result)
{
    if (CombatCheats.IsPersuasionAutoWin) __result = 1f;
}
```

---

## 13. CombatCheats.cs — Shared State

**Class**: `CombatCheats` (static)

```csharp
public static class CombatCheats
{
    public static bool IsPlayerInvulnerable { get; set; } = false;
    public static bool IsOneHitKill { get; set; } = false;
    public static bool IsPlayerImmortal { get; set; } = false;
    public static bool IsPersuasionAutoWin { get; set; } = false;
}
```

All patches read from this static class. VMs write to it.

---

## 14. GUI/Prefabs/HeroHackPanel.xml — Gauntlet UI

The UI is defined in Bannerlord's native Gauntlet XML format. Required elements:

### Panel Structure
```
Root: <Prefab>
  <Window>
    <Widget Name="HeroHackRoot" IsVisible="@IsOpen">
      <!-- Header bar: title + close button -->
      <Widget Name="Header">
        <TextWidget Text="HeroHack" />
        <ButtonWidget Id="CloseButton" Command.Click="ExecuteClose" />
      </Widget>

      <!-- Tab buttons: Hero | Party | Settlement | Export/Import -->
      <Widget Name="TabBar">
        <ButtonWidget Text="Hero"        Command.Click="ExecuteSelectTab0" IsSelected="@IsTab0Active"/>
        <ButtonWidget Text="Party"       Command.Click="ExecuteSelectTab1" IsSelected="@IsTab1Active"/>
        <ButtonWidget Text="Settlement"  Command.Click="ExecuteSelectTab2" IsSelected="@IsTab2Active"/>
        <ButtonWidget Text="Export/Import" Command.Click="ExecuteSelectTab3" IsSelected="@IsTab3Active"/>
      </Widget>

      <!-- Tab content panels: only one visible at a time -->
      <Widget Name="HeroTabContent"        IsVisible="@IsTab0Active" DataSource="{HeroTab}">
        <!-- Hero selector dropdown -->
        <!-- Sliders for Gold, Renown, Level, Skills, Attributes -->
        <!-- Toggles: Invulnerable, Immortal, Persuasion -->
        <!-- Buttons: Apply, Max Skills -->
      </Widget>

      <Widget Name="PartyTabContent"       IsVisible="@IsTab1Active" DataSource="{PartyTab}">
        <!-- Troop ID input + amount slider + Add button -->
        <!-- Morale, Food buttons -->
        <!-- Heal All button -->
      </Widget>

      <Widget Name="SettlementTabContent" IsVisible="@IsTab2Active" DataSource="{SettlementTab}">
        <!-- Settlement dropdown -->
        <!-- Sliders: Prosperity, Loyalty, Security -->
        <!-- Garrison troop add -->
      </Widget>

      <Widget Name="IOTabContent"          IsVisible="@IsTab3Active" DataSource="{IOTab}">
        <!-- Export section -->
        <!-- Import section with file dropdown -->
        <!-- Confirm overlay -->
      </Widget>

      <!-- Status bar -->
      <TextWidget Name="StatusBar" Text="@StatusMessage" />
    </Widget>
  </Window>
</Prefab>
```

> **Note for Copilot**: Study the existing Gauntlet prefabs in `[GameInstall]\Modules\Native\GUI\Prefabs\` for correct widget type names, property syntax (`@Property`, `DataSource="{SubVM}"`), and layout widget conventions (`StackLayout`, `ScrollablePanel`, etc.).

---

## 15. HUD Button Injection

To add a persistent button on the map screen (in addition to F10 hotkey), inject a secondary `GauntletLayer` with a minimal `HeroHackHudVM` that shows a small icon button anchored to the top-right corner of the screen.

```csharp
// HeroHackHudVM.cs
public class HeroHackHudVM : ViewModel {
    private readonly Action _onToggle;
    public HeroHackHudVM(Action onToggle) { _onToggle = onToggle; }
    [DataSourceMethod] public void ExecuteTogglePanel() => _onToggle();
}
```

The HUD prefab `HeroHackHud.xml` contains just one `ButtonWidget` with a `Text="⚔"` or icon sprite, bound to `ExecuteTogglePanel`. This layer persists on the map screen as long as the game is in campaign mode.

---

## 16. HeroSelectorVM.cs — Reusable Hero Picker

**Class**: `HeroSelectorItemVM : ViewModel`

```csharp
public HeroSelectorItemVM(Hero hero, Action<HeroSelectorItemVM> onSelect) {
    Hero = hero;
    Name = hero.Name.ToString();
    IsSelected = false;
    _onSelect = onSelect;
}

public Hero Hero { get; }
[DataSourceProperty] public string Name { get; set; }
[DataSourceProperty] public bool IsSelected { get; set; }
[DataSourceMethod] public void ExecuteSelect() => _onSelect(this);
```

---

## 17. Coding Standards & Rules

### Must Follow
1. **All `Hero`, `MobileParty`, `Settlement` accesses must be null-checked** before use.
   ```csharp
   if (Hero.MainHero == null || !Hero.MainHero.IsAlive) return;
   ```

2. **All Harmony patches must have guard conditions** — never blindly apply to all scenarios.

3. **Never use `throw`** in patch code — use `try/catch` and log with `InformationManager.DisplayMessage`.

4. **Use `TextObject` for all in-game text** (localization ready):
   ```csharp
   new TextObject("{=!}HeroHack: Gold applied.")
   ```

5. **Equipment modifications must check `Campaign.Current != null`** and be done on the **main thread** (not in background tasks).

6. **`ChangeSkillLevel` must use `shouldUpdateSkillStates: false`** to avoid cascading UI refreshes.

7. **All file I/O** must be wrapped in `try/catch(IOException)` and report friendly errors.

8. **Never hardcode DLL paths** — always use `GameFolder` property or relative references.

---

## 18. QA Acceptance Criteria

### AC-1: Panel Visibility
- ✅ Panel opens via `F10` on campaign map
- ✅ Panel opens via HUD button (top-right corner) on campaign map
- ✅ Panel does NOT open when in battle, town menu, or any non-map screen
- ✅ Panel closes via the X button inside the panel
- ✅ Panel closes via `F10` if already open (toggle)

### AC-2: Hero Tab — Player Hero
- ✅ Player hero is always first in the hero selector
- ✅ Setting gold to 50,000 → hero has exactly 50,000 gold post-apply
- ✅ Setting renown to 1,000 → `Hero.MainHero.Renown == 1000` post-apply
- ✅ Setting all skills to 300 via Max All Skills → all 18 skills are 300 in-game character sheet
- ✅ Toggle Invulnerable → player takes 0 damage in subsequent battle
- ✅ Toggle Immortal → player hero cannot die of old age or in battle (captured/wounded instead)
- ✅ Toggle Persuasion AutoWin → all persuasion checks succeed on first attempt

### AC-3: Hero Tab — Companion
- ✅ All active companions in the main party appear in the hero selector
- ✅ Modifying a companion's stats applies to that companion (not the player)
- ✅ Companion equipment import updates their `BattleEquipment` and is visually reflected in inventory

### AC-4: Party Tab
- ✅ Adding `imperial_recruit` x 50 → party roster shows 50 Imperial Recruits added
- ✅ Max morale → party morale shows 100 in the party panel
- ✅ Heal all → `WoundedNumber == 0` for all roster entries
- ✅ Adding invalid troop ID → status bar shows meaningful error, no crash

### AC-5: Settlement Tab
- ✅ Only player-owned settlements appear in the dropdown
- ✅ Setting prosperity to 8,000 → `Town.Prosperity == 8000` immediately
- ✅ Adding 100 garrison troops (by valid ID) → garrison count increased by 100

### AC-6: XML Export
- ✅ Export creates a valid XML file in `Documents\HeroHack\exports\`
- ✅ Filename follows the schema `<heroId>_day<n>_<timestamp>.xml`
- ✅ Export includes all 18 skills, 6 attributes, all 12 battle equipment slots, all 12 civilian slots
- ✅ Empty equipment slots are represented as `item_id=""` not omitted
- ✅ Status bar shows the full export path after successful export

### AC-7: XML Import
- ✅ Import file list is populated from `Documents\HeroHack\exports\`
- ✅ Refresh button updates the file list (picks up newly exported files)
- ✅ Importing a valid export file onto the player hero restores all stats exactly
- ✅ Importing a valid export file onto a companion restores that companion's stats
- ✅ If an item ID in the XML no longer exists in the game, the slot is left empty and a warning is shown
- ✅ Confirmation dialog appears before import is applied
- ✅ Cancelling the confirmation dialog does NOT apply any changes
- ✅ Importing a malformed XML file shows an error, does NOT crash

### AC-8: Stability
- ✅ Mod loads without errors when enabled via Bannerlord launcher
- ✅ Existing saves load normally with the mod active
- ✅ No `NullReferenceException` thrown in `Player.log` during normal panel usage
- ✅ Disabling mod in launcher and reloading save does not corrupt the save

---

## 19. Build & Test Instructions

### Build Command
```powershell
cd "G:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\HeroHack"
dotnet build -c Release
```

Expected output: `bin\Win64_Shipping_Client\HeroHack.dll`

### Test in Game
1. Launch Bannerlord via Steam
2. Enable `HeroHack` in the Mods list (after Native, SandBoxCore, etc.)
3. Load a campaign save
4. Verify green status message appears: `"HeroHack: Initialized"`
5. Press `F10` on the map — panel should appear

### Log File Location
```
%APPDATA%\..\LocalLow\Mount and Blade II Bannerlord\logs\rgl_log_*.txt
```
Or:
```
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\logs\
```

---

## 20. Implementation Order for Copilot (Phase by Phase)

### Phase 1 — Foundation (Do this first)
1. `HeroHack.csproj` — project file with DLL references
2. `SubModule.cs` — entry point with Harmony init, layer injection, F10 hotkey
3. `HeroHackLayer.cs` — empty Gauntlet layer that opens/closes
4. `HeroHackPanelVM.cs` — stub VM with `IsOpen` and tab switching
5. `HeroHackHudVM.cs` — minimal HUD button
6. `GUI/Prefabs/HeroHackPanel.xml` — basic panel with header and tab bar
7. `GUI/Prefabs/HeroHackHud.xml` — single toggle button

**✅ Gate**: Panel opens and closes via F10 and HUD button. Tabs switch but content is empty.

### Phase 2 — Hero Cheats
1. `HeroSelectorItemVM.cs`
2. `HeroCheats.cs`
3. `HeroTabVM.cs` (full implementation)
4. Update `HeroHackPanel.xml` with Hero tab content

**✅ Gate**: Gold, Renown, Skills, Attributes, Level all apply correctly to player and companion.

### Phase 3 — Toggles / Patches
1. `CombatCheats.cs`
2. `InvulnerabilityPatch.cs`
3. `OneHitKillPatch.cs`
4. `ImmortalHeroPatch.cs`
5. `PersuasionPatch.cs`
6. Wire toggles in `HeroTabVM`

**✅ Gate**: All four toggles work in-game.

### Phase 4 — Party & Settlement Tabs
1. `PartyCheats.cs` + `PartyTabVM.cs`
2. `SettlementCheats.cs` + `SettlementTabVM.cs`
3. Update XML prefab

**✅ Gate**: Troops, morale, food, settlement stats all apply.

### Phase 5 — XML Export/Import
1. `HeroXmlSchema.cs` — POCOs matching XML structure
2. `HeroExporter.cs`
3. `HeroImporter.cs`
4. `IOTabVM.cs`
5. Update XML prefab

**✅ Gate**: Full export/import round-trip verified. All AC-6 and AC-7 criteria pass.

### Phase 6 — Polish
1. Error handling throughout
2. Status messages for all actions
3. Final QA against all 8 AC sections
4. README update with feature list and instructions

---

*End of HeroHack Copilot Master Prompt — v1.0*
