# Bannerlord Modding — Learned Lessons (HeroHack Phase 1)

> Written for future Copilot sessions working on this mod.
> Every rule here was learned the hard way from a real runtime error or build failure.

---

## 1. Target Framework — ALWAYS net472

Bannerlord runs on its own embedded **Mono runtime**, which is `.NET Framework 4.7.2`-compatible.

```xml
<TargetFramework>net472</TargetFramework>
<LangVersion>10.0</LangVersion>                          <!-- keeps modern C# syntax -->
<GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
```

**Why net472 and NOT net6?**  
Targeting `net6.0` makes the compiler emit a dependency on `System.Runtime, Version=6.0.0.0`.
Bannerlord's Mono cannot resolve this and crashes at assembly load with:
```
Error while getting types and loading: Unable to load one or more of the requested types.
Loader Exceptions: Could not load file or assembly 'System.Runtime, Version=6.0.0.0'
```

**Also required:**
- No `<ImplicitUsings>enable</ImplicitUsings>` — net472 doesn't support it; add explicit `using` statements
- No `<Nullable>enable</Nullable>` for `!`-suppression unless you accept the warnings

---

## 2. Output Folder — Only YOUR DLL, Nothing Else

```xml
<OutputPath>bin\Win64_Shipping_Client\</OutputPath>
<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
```

After build, `bin\Win64_Shipping_Client\` must contain **only**:
- `HeroHack.dll`
- `HeroHack.pdb`

If any other `.dll` appears → a reference is missing `<Private>False</Private>`. Fix it immediately.

---

## 3. References — Workshop Mods Are NOT NuGet Packages

| Framework | Workshop ID | DLL |
|-----------|-------------|-----|
| Harmony | `2859188632` | `0Harmony.dll` |
| UIExtenderEx | `2859222409` | `Bannerlord.UIExtenderEx.dll` |
| ButterLib | `2859232415` | `Bannerlord.ButterLib.dll` |
| MCM | `2859238197` | `Bannerlord.MBOptionScreen.*.dll` |

**Always reference like this:**
```xml
<Reference Include="0Harmony">
  <HintPath>$(WorkshopFolder)\2859188632\bin\Win64_Shipping_Client\0Harmony.dll</HintPath>
  <Private>False</Private>
</Reference>
```

`Private=False` = compile-time only. The game assembly loader resolves DLLs from the Workshop folder at runtime.  
**NEVER** use `<PackageReference Include="Lib.Harmony">` — this copies a second `0Harmony.dll` into your output, causing version conflicts and Harmony failing silently.

---

## 4. SubModule.xml — DependedModule Id Must Be Exact

The `Id` in `<DependedModule Id="..."/>` must **exactly match** the `<Id value="..."/>` in the target mod's own `SubModule.xml`. Never guess from display names.

```
Harmony Workshop mod → <Id value="Bannerlord.Harmony"/>   ← NOT "Harmony"
```

**Verification step** (always do this for new dependencies):
```powershell
Get-Content "G:\...\workshop\content\261550\2859188632\SubModule.xml" | Select-String "Id value"
```

---

## 5. GauntletLayer Constructor Argument Order

```csharp
// CORRECT — string categoryId first, int layerOrder second
public HeroHackLayer() : base("GauntletLayer", 200) { }

// WRONG — swapped args cause CS1503
public HeroHackLayer() : base(200, "GauntletLayer") { }
```

---

## 6. GauntletLayer.Tick Is Protected, Not Public

```csharp
// CORRECT
protected override void Tick(float dt)
{
    base.Tick(dt);
    _panelVM.Tick(dt);     // drive VM timer from here
}

// WRONG — CS0507 (cannot change access modifier)
public override void Tick(float dt) { }
```

---

## 7. Gauntlet ViewModel Patterns

**`[DataSourceMethod]` does not exist in this TaleWorlds version.**  
Gauntlet binds `Command.Click="ExecuteXxx"` by naming convention. Just make the method `public` with no attribute.

**`ViewModel.OnTick(float)` is NOT overridable.**  
Pattern: implement your own `public void Tick(float dt)` and call it from inside the `GauntletLayer.Tick` override.

**`[DataSourceProperty]` IS correct** for bindable properties. Always call `OnPropertyChangedWithValue(value, nameof(Prop))` inside the setter.

**Tab switching:** XML cannot pass int parameters to commands. Use separate methods:
```csharp
public void ExecuteSelectTab0() => ActiveTabIndex = 0;
public void ExecuteSelectTab1() => ActiveTabIndex = 1;
// NOT: ExecuteSelectTab(int index)  ← XML binding can't call this
```

---

## 8. Buttons With TextWidget Children Swallow Clicks

Whenever a `<ButtonWidget>` has `<TextWidget>` children, the text widget intercepts mouse events and the button never fires.

**Fix:** add `DoNotPassEventsToChildren="true"` to the button:
```xml
<ButtonWidget Command.Click="ExecuteClose" DoNotPassEventsToChildren="true">
  <Children>
    <TextWidget Text="X" />
  </Children>
</ButtonWidget>
```

---

## 9. InputUsageMask — Name Collision Inside GauntletLayer Subclasses

`InputUsageMask` is **both** an inherited property (`ScreenLayer.InputUsageMask`) AND the enum type name (`TaleWorlds.ScreenSystem.InputUsageMask`).

Referring to it *inside* a `GauntletLayer` subclass causes:
```
CS0176: Member 'InputUsageMask.All' cannot be accessed with an instance reference
```

**Solution:** put all `SetInputRestrictions` / `ResetInputRestrictions` calls in a class that does NOT inherit `ScreenLayer` (e.g. `SubModule`). Expose `IsOpen` from the layer and call everything from outside:

```csharp
// SubModule.cs — no naming conflict here
private void ToggleHeroHackPanel()
{
    _heroHackLayer.TogglePanel();
    if (_heroHackLayer.IsOpen)
    {
        _heroHackLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
        _heroHackLayer.IsFocusLayer = true;
    }
    else
    {
        _heroHackLayer.InputRestrictions.ResetInputRestrictions();
        _heroHackLayer.IsFocusLayer = false;
    }
}
```

---

## 10. All Toggle Entry Points Must Share the Same Input-Restriction Logic

If you have multiple ways to toggle a panel (F10 hotkey, HUD button, close button inside the panel), **all paths must go through one method** that sets `InputRestrictions` + `IsFocusLayer`.

Common mistake: wiring the HUD button directly to `layer.TogglePanel()` (which only flips `IsOpen`) instead of the full `ToggleHeroHackPanel()` method. Result: panel opens but is unresponsive — clicks pass through to the map.

---

## 11. HUD Button — Mouse-Only Input Restrictions

A persistent HUD button should NOT block map keyboard shortcuts. Use:
```csharp
_hudLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.Mouse);
```

The first `bool` argument is "isMouseVisible", not "all input enabled". Passing `false` keeps the mouse usable without locking keyboard input.

---

## 12. Inspecting Game DLLs for API Signatures

When reflection fails in PowerShell (Mono-targeting DLLs often refuse to load into a 64-bit PowerShell process), use Python:

```python
import re
d = open(r'path\to\TaleWorlds.ScreenSystem.dll', 'rb').read()
strings = [m.group().decode('ascii','ignore') for m in re.finditer(rb'[\x20-\x7e]{3,}', d)]
relevant = [s for s in strings if re.search(r'Input|Layer|Mouse|Restrict', s)]
```

For finding type namespaces: search all game DLLs for the type name and check which one also contains `value__` (enum backing field) nearby.

---

## 13. Verified Working Layer Setup (Phase 1 Reference)

```csharp
// SubModule.OnApplicationTick
bool isOnMap = ScreenManager.TopScreen is MapScreen;  // SandBox.View.Map.MapScreen
if (isOnMap && !_layersInjected) InjectLayers();
else if (!isOnMap && _layersInjected) RemoveLayers();

// Injection
_heroHackLayer = new HeroHackLayer();          // GauntletLayer, order 200
_mapScreen.AddLayer(_heroHackLayer);

_hudLayer = new HeroHackHudLayer(ToggleHeroHackPanel);  // order 100
_hudLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.Mouse);
_mapScreen.AddLayer(_hudLayer);
```

---

## 14. Gauntlet `VerticalTopToBottom` Is INVERTED — Use `VerticalBottomToTop`

**Verified via widget tree dump (Phase 2).**

`StackLayout.LayoutMethod="VerticalTopToBottom"` places the **first** XML child at the **bottom** (highest Y value) and the **last** child at Y=0 (top of the container). This is the exact opposite of what the name implies.

To get a normal top-to-bottom visual stack (first child at top, last at bottom), use:

```xml
<ListPanel StackLayout.LayoutMethod="VerticalBottomToTop" ...>
```

This was confirmed by a runtime widget tree dump showing:
- Outer `VerticalTopToBottom`: Tab bar (last child) at Y=0, Content area (first child) at Y=73
- Inner `VerticalTopToBottom`: "Combat Toggles" (last child) at Y=0, hero name (first child) at Y=602

---

## 15. How to Dump the Widget Tree at Runtime

Add this to your `GauntletLayer` subclass to diagnose layout issues without screenshots:

```csharp
// Trigger 5 frames after panel opens (layout must settle first)
private void DumpWidgetTree()
{
    var sb = new StringBuilder();
    WalkWidget(UIContext.Root, 0, sb);
    File.WriteAllText(@"Documents\HeroHack\widget_dump.txt", sb.ToString());
}

private static void WalkWidget(Widget w, int depth, StringBuilder sb)
{
    var indent = new string(' ', depth * 2);
    // Use w.Left, w.Top, w.Right, w.Bottom — NOT w.GlobalPosition or w.Size (Vector2 causes CS0012)
    sb.AppendLine($"{indent}[{(w.IsVisible?"V":"H")}] {w.GetType().Name} Y={w.Top:F0} X={w.Left:F0} W={w.Right-w.Left:F0} H={w.Bottom-w.Top:F0}");
    for (int i = 0; i < w.ChildCount; i++) WalkWidget(w.GetChild(i), depth + 1, sb);
}
```

**Do NOT use `w.GlobalPosition` or `w.Size`** — they return `Vector2` from `System.Numerics.Vectors` which is not referenced by default and causes `CS0012`. Use `w.Left`, `w.Top`, `w.Right`, `w.Bottom` instead.

---

## 16. `DataSource="{SubVM}"` Must Be on the `ListPanel`, Not a Wrapper Widget

If you wrap the `DataSource` ListPanel in an outer `Widget` for visibility (`IsVisible="@IsTab0Active"`), bind `DataSource` to the **inner `ListPanel`** only — never the outer `Widget`.

```xml
<!-- CORRECT -->
<Widget IsVisible="@IsTab0Active">
  <Children>
    <ListPanel DataSource="{HeroTab}" ...>  <!-- bindings resolve here -->
```

```xml
<!-- WRONG — causes raw VM data to float at top-left or bindings to fail silently -->
<Widget IsVisible="@IsTab0Active" DataSource="{HeroTab}">
```

---

## 17. `ButtonType="Toggle"` Fires `Command.Click` Twice

`ButtonType="Toggle"` causes the engine to fire `Command.Click` on both mousedown AND mouseup. For a toggle button that calls `ExecuteToggleX()` (which flips a bool), this means the bool flips twice — always returning to its original value (always appearing OFF).

**Fix:** Remove `ButtonType="Toggle"`. Use `IsSelected="@IsBoolProp"` for the visual selected-state only, and let your `ExecuteToggleX()` method flip the bool manually.

---

## 18. `EditableTextWidget` Height — Use Fixed, Not Stretch

`EditableTextWidget` with `HeightSizePolicy="StretchToParent"` is consumed by internal brush padding and only shows 1–2 characters vertically.

**Fix:**
```xml
<EditableTextWidget HeightSizePolicy="Fixed" SuggestedHeight="22" FontSize="13" ... />
```

Use `SuggestedHeight="20"` for skill rows (3-col, tighter layout) and `SuggestedHeight="22"` for stat rows.

---

## 19. Gold Formatting — `ToString("N0")` + Strip Commas on Parse

Display gold with thousand separators: `hero.Gold.ToString("N0")` → `"2,000,001,000"`.

When reading back from an `EditableTextWidget` for parsing, strip the commas first:
```csharp
int.TryParse(_goldText.Replace(",", ""), out int gold)
```

---

## 20. `CoverChildren + VerticalAlignment="Top"` for Scrollable Content Sections

The proven pattern for a content section that sizes to its children and aligns to the top of its container:

```xml
<ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
           VerticalAlignment="Top"
           StackLayout.LayoutMethod="VerticalBottomToTop"
           DataSource="{HeroTab}"
           MarginLeft="30" MarginRight="30" MarginTop="10">
```

This avoids the `StretchToParent` pitfall (which causes unreliable layout direction) and reliably anchors content to the top of the content area.

---

## 21. `Agent.CurrentMortalityState` Getter Patch Corrupts Character Screen

**DO NOT** patch `Agent.CurrentMortalityState` (getter) to force `MortalityState.Invulnerable`.

The character/party screen creates **preview agents** to render hero models. Our postfix checked `__instance.IsPlayerControlled`, but preview agents representing the player character also return true for this check. Forcing their MortalityState to `Invulnerable` poisons the animation controller — the engine renders them in a static ragdoll/death pose (floating horizontally, no idle animation).

This affects **all** characters across **all** save files while the patch is active. Removing the patch immediately fixes it (no save corruption — it's a runtime rendering issue).

**Use `MissionBehavior.OnMissionTick` HP-clamping instead** (see lesson 24).

---

## 22. Harmony Cannot Patch `SandboxAgentApplyDamageModel.CalculateDamage`

Attempting to `[HarmonyPatch]` this method causes a **native crash at startup** during `PatchAll()`. No managed exception is logged — the engine terminates silently.

The method signature is:
```csharp
float CalculateDamage(ref AttackInformation, ref AttackCollisionData, float baseDamage)
```

`AttackInformation` is a large struct (~100 fields) passed by ref. Harmony's IL rewriter appears unable to generate a valid trampoline for this signature on Bannerlord's Mono runtime. BannerlordCheats (v3.0.3) claimed to patch this successfully, but that was for an older game version.

**Workaround:** Use `MissionBehavior` callbacks (`OnMissionTick`, `OnAgentHit`) which don't require Harmony.

---

## 23. Crash Debugging — Bannerlord Log Locations

Crash logs are at: `C:\ProgramData\Mount and Blade II Bannerlord\crashes\<timestamp>\`

Key files:
- `rgl_log_<pid>.txt` — engine log. Multiple PIDs exist (watchdog, child processes). The **crashing process** is the one whose log does NOT end with `Managed Interface deleted.`
- `crash_tags.txt` — system info only; rarely contains the exception
- `rgl_log_errors_<pid>.txt` — usually empty for managed crashes
- `watchdog_log_<pid>.txt` — memory dump info, not useful for debugging

For managed exceptions, search with:
```powershell
Select-String -Path "C:\ProgramData\Mount and Blade II Bannerlord\crashes\<timestamp>\rgl_log_*.txt" -Pattern "exception|error|fail|TypeLoad|MissingMethod" -CaseSensitive:$false
```

If no managed exception appears, the crash is from Harmony IL generation failing at the native level during `PatchAll()`.

---

## 24. The Correct Invulnerability Pattern — MissionBehavior HP Clamp

The only approach that works without causing ragdoll corruption or startup crashes:

```csharp
public class HeroHackMissionBehavior : MissionBehavior
{
    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    public override void OnMissionTick(float dt)
    {
        Agent player = Agent.Main;
        if (player == null || !player.IsActive()) return;

        if (CombatCheats.IsPlayerInvulnerable && player.Health < player.HealthLimit)
            player.Health = player.HealthLimit;
    }

    public override void OnAgentHit(Agent affected, Agent affector, 
        in MissionWeapon weapon, in Blow blow, in AttackCollisionData data)
    {
        if (CombatCheats.IsOneHitKill && affector == Agent.Main 
            && affected != Agent.Main && affected.IsActive())
        {
            affected.Health = 0;
            var killBlow = new Blow(affector.Index) { InflictedDamage = 10000 };
            affected.Die(killBlow);
        }
    }
}
```

Wired in SubModule via:
```csharp
public override void OnMissionBehaviorInitialize(Mission mission)
{
    base.OnMissionBehaviorInitialize(mission);
    mission.AddMissionBehavior(new HeroHackMissionBehavior());
}
```

**Why this works:**
- `OnMissionTick` runs every frame in managed code — player HP is restored before the engine processes state transitions
- No Harmony patches on native-passing-by-ref structs
- No `MortalityState` override that corrupts preview agent rendering
- `OnAgentHit` fires after damage is dealt — safe to kill the enemy there

---

## 25. `OnMissionBehaviorInitialize(Mission)` Signature

`MBSubModuleBase.OnMissionBehaviorInitialize` is virtual and takes one parameter:
```csharp
public override void OnMissionBehaviorInitialize(Mission mission)
```

Do **not** call it with zero parameters — it won't compile silently (it overloads a different method).

