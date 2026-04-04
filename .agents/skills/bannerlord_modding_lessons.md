# Bannerlord Modding Lessons (HeroHack)

> Hard-won rules from real crashes and bugs. Read before writing any code in this mod.

---

## Build & Project

**1. Always `net472`.** Bannerlord = Mono. `net6.0` → `System.Runtime` load failure. Also set `LangVersion=10.0`, `GenerateTargetFrameworkAttribute=false`. No `ImplicitUsings`.

**2. Output = only your DLL.** `<OutputPath>bin\Win64_Shipping_Client\</OutputPath>` + `AppendTargetFrameworkToOutputPath=false`. If extra DLLs appear, a reference is missing `<Private>False</Private>`.

**3. Workshop mods ≠ NuGet.** Reference via `<HintPath>$(WorkshopFolder)\<ID>\bin\Win64_Shipping_Client\<dll></HintPath>` with `<Private>False</Private>`. NEVER `<PackageReference>` for Harmony/ButterLib/MCM — causes duplicate DLL conflicts.

**4. `DependedModule Id` must exact-match** the target mod's `<Id value="..."/>`. Harmony = `Bannerlord.Harmony`, NOT `Harmony`. Always verify: `Get-Content SubModule.xml | Select-String "Id value"`.

## Gauntlet UI

**5. GauntletLayer ctor:** `base("GauntletLayer", 200)` — string first, int second.

**6. `Tick` is `protected override`**, not public. `ViewModel.OnTick` is not overridable — make your own `Tick(float)` and call from layer.

**7. VM binding rules:**
- `[DataSourceMethod]` doesn't exist — just use `public void ExecuteXxx()` (naming convention).
- `[DataSourceProperty]` + `OnPropertyChangedWithValue(value, nameof(Prop))` in setter.
- XML can't pass int params → use `ExecuteSelectTab0()`, `ExecuteSelectTab1()`, etc.

**8. ButtonWidget + TextWidget child = swallowed clicks.** Fix: `DoNotPassEventsToChildren="true"` on the button.

**9. `InputUsageMask` name collision** inside `GauntletLayer` subclasses (property vs enum). Move `SetInputRestrictions`/`ResetInputRestrictions` calls to `SubModule.cs` instead.

**10. All panel toggle paths** (hotkey, HUD button, close button) must go through ONE method that sets `InputRestrictions` + `IsFocusLayer`. Otherwise clicks pass through to the map.

**11. HUD button:** `SetInputRestrictions(false, InputUsageMask.Mouse)` — mouse-only, don't block keyboard.

**12. DLL inspection:** PowerShell `Add-Type` often works for game DLLs. Fallback: Python binary string search on the `.dll`.

**13. Layer injection pattern:** Check `ScreenManager.TopScreen is MapScreen` in `OnApplicationTick`. Inject on map enter, remove on map leave.

**14. `VerticalTopToBottom` is INVERTED.** First child → bottom, last → top. Use `VerticalBottomToTop` for normal top-down stacking. Verified via widget dump.

**15. Widget tree dump:** Use `w.Left`, `w.Top`, `w.Right`, `w.Bottom`. NOT `w.GlobalPosition`/`w.Size` (Vector2 → CS0012). Trigger 5 frames after open.

**16. `DataSource="{SubVM}"`** goes on the inner `ListPanel`, not an outer visibility wrapper Widget.

**17. `ButtonType="Toggle"` fires Click twice** (mousedown + mouseup) → bool flips back. Remove it; use `IsSelected="@BoolProp"` for visuals only.

**18. `EditableTextWidget`:** Use `HeightSizePolicy="Fixed" SuggestedHeight="22"`. `StretchToParent` collapses to ~1 char height.

**19. Gold formatting:** Display with `ToString("N0")`, parse with `.Replace(",", "")` before `int.TryParse`.

**20. Content sections:** `HeightSizePolicy="CoverChildren" VerticalAlignment="Top"` + `VerticalBottomToTop`. Avoids `StretchToParent` layout bugs.

## Harmony & Combat Patches

**21. DO NOT patch `Agent.CurrentMortalityState` getter.** Forcing `MortalityState.Invulnerable` corrupts character/party screen — preview agents get stuck in ragdoll pose (all characters, all saves, while patch active). `IsPlayerControlled` returns true for preview agents too. Fix is immediate on patch removal (runtime issue, not save corruption).

**22. DO NOT Harmony-patch `SandboxAgentApplyDamageModel.CalculateDamage`.** Native crash at startup during `PatchAll()` — no managed exception logged. `AttackInformation` is a ~100-field struct passed by ref; Harmony can't generate a valid trampoline on Mono. Use `MissionBehavior` callbacks instead.

**23. Crash log location:** `C:\ProgramData\Mount and Blade II Bannerlord\crashes\<timestamp>\`. The crashing process's `rgl_log_<pid>.txt` does NOT end with `Managed Interface deleted.` Search: `Select-String -Pattern "exception|error|TypeLoad|MissingMethod"`. No managed exception = Harmony IL generation failure.

**24. Correct invulnerability: MissionBehavior HP clamp.**
```csharp
// In MissionBehavior.OnMissionTick:
if (cheatOn && player.Health < player.HealthLimit) player.Health = player.HealthLimit;
// In MissionBehavior.OnAgentHit: for OHK, set affected.Health=0 then affected.Die(blow)
// Wire via SubModule: override OnMissionBehaviorInitialize(Mission mission) → mission.AddMissionBehavior(...)
```
No ragdoll corruption, no Harmony ref-struct issues, catches all damage sources including native.

**25. `OnMissionBehaviorInitialize`** takes `Mission mission` (one param, virtual). Not zero params.

