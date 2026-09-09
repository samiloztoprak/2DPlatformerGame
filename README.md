# Jumpy

An infinite vertical jumper (Doodle Jump–style) built in **Unity 6 (6000.4.4f1)** with **URP 2D**, targeting **Android**. The character bounces automatically from platform to platform; the player only steers left/right by touching or clicking either half of the screen.

This project was built as a technical case study with a deliberate focus on **clean architecture, established design patterns, and data-driven tooling** — not just "making the game work." Every system below was chosen for a specific, stated reason, and the codebase follows a documented C# style guide and refactoring discipline throughout (see [Engineering Standards](#engineering-standards)).

---

## Table of Contents

1. [Gameplay](#gameplay)
2. [Architecture Philosophy](#architecture-philosophy)
3. [Project Layout](#project-layout)
4. [Design Patterns](#design-patterns)
5. [Systems Reference](#systems-reference)
6. [UI (UI Toolkit)](#ui-ui-toolkit)
7. [Audio](#audio)
8. [Data Assets](#data-assets)
9. [Android Build](#android-build)
10. [Engineering Standards](#engineering-standards)
11. [Notable Bugs Found & Fixed](#notable-bugs-found--fixed)
12. [Possible Next Steps](#possible-next-steps)

---

## Gameplay

| Mechanic | Summary |
|---|---|
| Auto-jump | `Rigidbody2D` + one-way `PlatformEffector2D` platforms; the player is launched upward on any landing from above |
| Steering | Hold the left/right half of the screen (mouse or touch) to move horizontally |
| Screen wrap | Exiting one side of the screen re-enters from the opposite side |
| Camera | Follows upward only, never downward, with a hard catch-up clamp so a fast jump can never outrun the view |
| Infinite platform stream | Platforms are generated ahead of the player and pooled back once off-screen — no manual level layout exists |
| Platform variety | **Static**, **horizontally oscillating**, and **crumbling** platform types, spawned from a weighted random table |
| Crumbling platforms | Visually distinct (red/orange tint), solid for a brief moment after landing, then collapse and fall out of play |
| Platform spacing | Vertical gap between platforms is derived from the player's actual jump physics (see below), not a hand-tuned guess |
| Fail state | Falling below the camera reloads the scene back to the main menu |
| Pause | Opening the settings panel sets `Time.timeScale = 0`; UI and audio remain fully responsive while the game freezes |

**Platform spacing is physics-derived, not guessed.** The jump apex height is computed from the actual rigidbody values (`h = jumpForce² / (2 · gravity · gravityScale)` ≈ 3.33 units), and every platform type's vertical gap is set to **2/3 of that value (2.22 units)** — close enough to feel fair, far enough to require real timing.

---

## Architecture Philosophy

Three principles drive every decision in this codebase:

- **Data-driven.** Every tunable number (speeds, jump force, spawn distances, audio volumes) lives in `ScriptableObject` assets, not in code. Nothing is hand-placed in the scene — the entire level is generated at runtime from data (see `GameInitializer`).
- **Loosely coupled, but not dogmatically.** Systems communicate through `ScriptableObject`-based event channels where that decoupling actually pays off (see [Observer / Event Channels](#observer--scriptableobject-event-channels)). Where a relationship is naturally 1:1 and single-purpose (e.g. camera → player target), a direct reference is used instead — indirection is a tool, not a rule.
- **Small, single-purpose classes.** No God objects. The platform streaming pipeline alone is split into four classes, each with one job (see [Single Responsibility](#single-responsibility--the-platform-pipeline)).

These principles are grounded in two references kept alongside the project: Unity's official *"Create a C# Style Guide"* and Alexander Shvets' *"Dive Into Refactoring"* (the code-smell/refactoring catalog referenced throughout this document).

---

## Project Layout

```
Assets/Scripts/
  Core/
    Events/      → Game.Core.Events      generic SO event-channel infrastructure
    Pooling/     → Game.Core.Pooling     generic object pool infrastructure
    Utility/     → Game.Core.Utility     stateless helpers (e.g. screen bounds math)
  Data/          → Game.Data             ScriptableObject data containers
  Platforms/     → Game.Platforms        pooling, spawn/recycle, movement strategies
  Player/        → Game.Player           input, movement, jump, wrap, fall detection
  CameraSystem/  → Game.CameraSystem     camera follow logic
  Audio/         → Game.Audio            SFX playback, volume bridge
  UI/            → Game.UI               UI Toolkit controllers
  Bootstrap/     → Game.Bootstrap        game entry point
```

Dependencies flow **one way only**: `Data` never depends on `Platforms`, even though `Platforms` depends on `Data`. This is enforced by a deliberate design choice — see [Data-Driven ScriptableObjects](#data-driven-scriptableobjects) below.

---

## Design Patterns

### Object Pool — `Core/Pooling/`

**Where:** `IPoolable` interface + generic `ComponentPool<T>` (wraps `UnityEngine.Pool.ObjectPool<T>`), consumed by `Platforms/PlatformPoolManager`.

**Why:** The game streams platforms endlessly. Instantiating/destroying a GameObject on every spawn would create constant GC pressure and frame-time spikes — a real concern on the Android target. Pooling recycles the same instances instead of churning the heap.

```csharp
public class ComponentPool<T> where T : Component, IPoolable
```

Because it's generic, this pool isn't platform-specific — it can be reused for any future pooled object (projectiles, particles) without modification. `IPoolable.OnSpawned()` / `OnDespawned()` let each object type define its own reset logic (e.g. `PlatformController.OnDespawned()` clears its crumble state and re-enables its collider so a reused instance behaves identically to a fresh one).

`PlatformPoolManager` keeps **one pool per `PlatformDataSO`**, so static, moving, and crumbling platforms never share (and can't corrupt) each other's pool.

### Observer — ScriptableObject Event Channels — `Core/Events/`

**Where:** Generic `EventChannelSO<T>` base class, with concrete channels `FloatEventChannelSO`, `AudioClipEventChannelSO` (Core) and `PlatformEventChannelSO` (Platforms — kept in its own layer because its payload type belongs there, avoiding a reverse dependency).

**Why:** This is the ScriptableObject-based Observer pattern popularized by Unity's own architecture talks. Publishers and subscribers never reference each other's scripts — they share an asset, wired in the Inspector. That keeps every system independently testable and swappable.

**Concrete payoff:** `PlayerJumpController` raises `PlayerLandedChannel` on every landing. That event now has **two independent listeners** that know nothing about each other:

```csharp
// JumpSfxTrigger.cs — has zero knowledge of PlayerJumpController
private void OnEnable() => _playerLandedChannel.OnEventRaised += HandlePlayerLanded;
private void HandlePlayerLanded(PlatformController platform) => _sfxRequestChannel.Raise(_jumpClip);
```

A future scoring system could subscribe the same way without touching a single line of `PlayerJumpController`.

**Why generic payload types instead of one class per event:** `FloatEventChannelSO` is reused for input direction and could carry any other float-valued event; a dedicated `PlayerJumpedEventSO` per event name would be *Speculative Generality* (Dispensables, per the refactoring catalog). A handful of reusable payload types, instantiated as distinct assets per use case, is the leaner alternative.

**Why not everywhere:** Camera-follows-player (`CameraFollowController.SetTarget`) and settings-panel-hides-menu are naturally 1:1, single-consumer relationships. Routing those through an event channel would be indirection for its own sake — a straight reference is simpler and equally correct.

### Strategy + Factory — Platform Movement

**Where:** `Platforms/IPlatformMovement` interface, `StaticPlatformMovement` / `HorizontalOscillatePlatformMovement` implementations, `PlatformMovementFactory` (a single, centralized creation point).

**Why:** This is a direct application of *Replace Conditional with Polymorphism*. Instead of a `switch (movementType)` scattered through `Update()`, each behavior is its own small class:

```csharp
public interface IPlatformMovement
{
    Vector3 Evaluate(Vector3 basePosition, float elapsedTime, PlatformDataSO data);
}
```

The one `switch` inside `PlatformMovementFactory` is a legitimate factory — the smell the pattern avoids is *scattered* type-checks across a codebase, not a single, centralized creation point. Adding a new movement type later means adding one new class, not touching existing ones (Open/Closed Principle).

Note that **crumbling behavior deliberately isn't modeled as an `IPlatformMovement`.** It's a one-shot, *triggered* state change (touch → delay → fall → collider off) rather than a continuous function of elapsed time, and it needs to take over from — not compose with — the underlying movement strategy. Forcing it into the same interface would have meant polluting `IPlatformMovement` with a `Trigger()` no-op on every other implementation for a behavior that doesn't share their shape. Instead, `PlatformController.NotifyLanded()` handles it as an explicit state machine local to the controller — the same "right tool over reflexive pattern-matching" judgment call this document keeps coming back to.

### Data-Driven ScriptableObjects

**Where:** Every `...SO.cs` file under `Data/`.

| Asset | Purpose |
|---|---|
| `GameConfigSO` | Player speed, jump force, wrap/death margins, camera follow tuning, spawn/despawn distances |
| `PlatformDataSO` | Defines one platform *type*: prefab, sprite, vertical gap, movement type + parameters, crumble parameters |
| `PlatformSpawnSetSO` | Weighted list of `PlatformDataSO` entries with `GetRandomPlatform()` — difficulty/variety tuning lives entirely in this asset, no code changes required |
| `GameSettingsSO` | Runtime audio preferences (Master/SFX volume), backed by `PlayerPrefs` |

**Why:** A designer can rebalance the entire game from the Inspector without touching code or recompiling. `PlatformDataSO.Prefab` is deliberately typed as `GameObject`, not the more specific `PlatformController` — the alternative would make `Data` depend on `Platforms`, creating a circular reference (`Platforms` already depends on `Data`). This is a concrete instance of "dependencies flow one way."

### Single Responsibility — the Platform Pipeline

`PlatformStreamManager` (a thin `MonoBehaviour` orchestrator) drives `PlatformSpawner` (decides what spawns where) → `PlatformPoolManager` (pulls from the pool) → `PlatformRecycler` (returns off-screen platforms to the pool). Each class does exactly one thing and can be reasoned about — and unit-tested — in isolation. This avoids the *Large Class* / *Long Method* smells that a single "PlatformManager does everything" class would accumulate over time.

---

## Systems Reference

### Core

- **`EventChannelSO<T>`** — generic SO event channel base: `event Action<T> OnEventRaised` + `Raise(T value)`.
- **`ComponentPool<T>`** — generic pooling wrapper (see above).
- **`ScreenBoundsUtility`** — stateless helper that derives world-space screen bounds from `Camera.orthographicSize` + `aspect`. Reused by `PlatformStreamManager` (spawn width), `PlayerScreenWrapper` (wrap edges), and `PlayerFallDetector` (death threshold) — one implementation, three consumers.

### Platforms

- **`PlatformController`** — implements `IPoolable`; owns its `PlatformDataSO` and active `IPlatformMovement`. `Initialize()` also assigns the type's sprite from data, so visuals are fully data-driven rather than baked into the prefab. `PlaceAt()` is a deliberately separate step from `Initialize()` — positioning after pooling, not during, avoids a stale-position bug that surfaced during development (see [Notable Bugs](#notable-bugs-found--fixed)).
- **One-way platforms via `PlatformEffector2D`** — the character can jump up through a platform from below and only lands when descending onto it from above. This uses Unity's built-in physics component for exactly this use case instead of a hand-rolled pivot comparison (KISS: don't reinvent a solved problem).
- **Crumbling platforms** — `PlatformDataSO.IsCrumbling = true` types. `PlayerJumpController` calls `platform.NotifyLanded()` directly (a targeted call to the specific instance, alongside the broadcast event — a 1:1 relationship, not a broadcast one). The controller then waits `CrumbleDelay` seconds, disables its collider, and accelerates downward via `CrumbleFallAcceleration`. **No new pooling logic was needed** — once the platform physically falls below the camera, the existing `PlatformRecycler` picks it up automatically, exactly as it would any other platform. `OnDespawned()` fully resets the crumble state, verified by driving the same pooled instance through two full landing→fall→recycle→reuse cycles in testing.

### Player

- **`PlayerInputReader`** — reads `Pointer.current` from the new Input System and raises a direction event based on which half of the screen is held. Mouse and touch are handled by the same code path with no platform branching.
- **`PlayerMovementController`** — applies horizontal velocity from the direction event.
- **`PlayerJumpController`** — applies jump force on landing (the one-way effector already guarantees the collision only fires on a downward landing, so no extra velocity check is needed) and raises the landed event.
- **`PlayerScreenWrapper`** — wraps the player across screen edges.
- **`PlayerFallDetector`** — reloads the scene if the player falls below the camera's lower bound (+ margin).

### CameraSystem

- **`CameraFollowController`** — follows upward only (`Mathf.Max` guarantees it never regresses downward), smoothed with `SmoothDamp`, with a hard-clamp catch-up if the gap to the player exceeds `MaxCameraLag` — this is what prevents a fast jump from ever carrying the player off the top of the screen.

### Bootstrap

- **`GameInitializer`** — exposes `BeginGame()`, called by the main menu's Play button. Spawns the player, sets the camera target, and starts the platform stream. Nothing runs automatically on scene load; the game waits for explicit player input.

---

## UI (UI Toolkit)

Built with **UI Toolkit** (UXML/USS), a built-in Unity 6 module — no third-party UI package required.

- **Main Menu** (`Assets/UI/MainMenu/`) — title + Play button, driven by `MainMenuController`.
- **Settings Panel** (`Assets/UI/Settings/`) — Master/SFX volume sliders, driven by `SettingsMenuController`. Can be opened from the main menu *or* mid-run; it remembers which screen it was opened from and restores the correct state on close. Opening it pauses the game (`Time.timeScale = 0`); the panel itself and its audio feedback stay fully responsive since UI Toolkit and `AudioSource` are unaffected by time scale.
- **Persistent Settings Button** (`Assets/UI/Hud/`) — a fixed, round, top-right icon button, driven by `SettingsHudController`. It lives on its own always-active `UIDocument` so it's available identically in the menu and in-game, without needing per-screen wiring.
- **`KenneyButtonSkinner`** — a small, reusable, non-`MonoBehaviour` helper class that applies Kenney UI art (normal/pressed sprite swap + 9-slice border) to any UI Toolkit `Button`. Used by both the main menu and the HUD button — one implementation, no duplication.

`UIDocument.sortingOrder` layers the three panels deterministically: Main Menu (0) < HUD button (1) < Settings panel (2, so it always renders on top when open).

---

## Audio

- **`AudioClipEventChannelSO`** — a generic "play this clip" request channel (Core.Events) — the Observer pattern applied a second time, for sound.
- **`SfxPlayer`** — the single component that actually owns an `AudioSource` and listens for that channel, firing `PlayOneShot`.
- **`JumpSfxTrigger`** — subscribes to the *existing* `PlayerLandedChannel` and requests the jump SFX — proof the event channel architecture pays for itself, since no change to jump/landing code was needed to add sound.
- **`AudioSettingsController`** — multiplies Master × SFX volume from `GameSettingsSO` onto the `SfxPlayer`'s `AudioSource`, and persists changes. Deliberately initializes in `Start()`, not `Awake()` — see [Notable Bugs](#notable-bugs-found--fixed).

SFX are sourced from the Kenney UI Audio pack (`Assets/Audio/Kenney/`): `tap-a` (jump), `click-a` (button press), `switch-a` (slider/setting change). Unused pack variants were identified and removed from the repository (see engineering notes below).

---

## Data Assets

```
Assets/Data/
  GameConfig.asset              → GameConfigSO
  GameSettings.asset            → GameSettingsSO (PlayerPrefs-backed)
  Events/
    MoveDirectionChannel.asset  → FloatEventChannelSO
    PlayerLandedChannel.asset   → PlatformEventChannelSO
    SfxRequestChannel.asset     → AudioClipEventChannelSO
  Platforms/
    PlatformData_Static.asset     → PlatformDataSO (static platform)
    PlatformData_Moving.asset     → PlatformDataSO (horizontal oscillation)
    PlatformData_Crumbling.asset  → PlatformDataSO (collapses after landing, red/orange sprite)
    PlatformSpawnSet.asset        → PlatformSpawnSetSO (50% static / 25% moving / 25% crumbling)
```

All three platform types reuse the **same prefab** (`Platform.prefab`) — the difference between them is entirely data (sprite, movement, crumble parameters), which is the data-driven philosophy paying off directly in reduced asset count.

---

## Android Build

- **Target:** Android, IL2CPP scripting backend, ARM64 architecture (Play Store requirement — already the project default).
- **Package identifier:** `com.Jumpy.Jumpy`.
- **Orientation:** locked portrait (vertical gameplay).
- **Input:** the new Input System (`Pointer.current`) already unifies mouse and touch, so no Android-specific input code was required.
- Build was verified end-to-end with zero compile errors via `File > Build Settings > Android`.

---

## Engineering Standards

Every C# file follows two references kept in `Rules/`:

- Unity's official **Create a C# Style Guide** — PascalCase for classes/methods/public members, camelCase with a `_` prefix for private fields, Allman braces, braces on single-line `if` statements, `[SerializeField]` + `[Tooltip]` over public fields, minimal comments (only where the *why* isn't obvious from the code).
- **Dive Into Refactoring** (Refactoring.Guru) — the code-smell catalog (Bloaters, Object-Orientation Abusers, Change Preventers, Dispensables, Couplers) referenced by name throughout this document whenever a design decision was made specifically to avoid one of them.

**Repository hygiene:** the codebase was audited for dead code and unused assets before publishing — every public class, method, and property was cross-referenced for actual usage; 78 unused imported UI sprites and 3 unused audio clips were identified (by scanning every serialized reference project-wide, not just filename search) and removed, along with two write-only fields left over from earlier iterations.

---

## Notable Bugs Found & Fixed

Documented here deliberately — these reflect the actual debugging process, not just the final state:

- **Guaranteed first platform.** Early on, the very first platform spawned at a random X position instead of under the player, so a missed landing meant falling forever with nothing below to catch it. Fixed by adding `PlatformSpawner.SpawnAt(x)` so the initial platform is always placed directly under the spawn point.
- **Stale velocity check on one-way platforms.** After switching platforms to `PlatformEffector2D`, a leftover `if (velocity.y > 0) return;` guard in the jump handler could spuriously suppress a legitimate landing bounce (solver noise could report a tiny positive Y-velocity on contact), leaving the character permanently resting on a platform. Removed once the effector itself was confirmed to already guarantee landing-only collisions.
- **Cross-component `Awake()` ordering.** `AudioSettingsController.Awake()` depended on `SfxPlayer.Awake()` having already run on the same GameObject — an order Unity does not guarantee. Moved the dependent logic to `Start()`, which is guaranteed to run after every `Awake()` in the scene.
- **RectOffset field initializer.** A `[SerializeField] private RectOffset _slice = new RectOffset(...)` field initializer threw at runtime — `RectOffset`'s constructor performs native calls that Unity disallows outside `Awake()`/`Start()`. Removed the inline default and let the Inspector-assigned value take over.

---

## Possible Next Steps

- Difficulty currently emerges naturally from the crumbling-platform weight in `PlatformSpawnSetSO`; a system that shifts those weights based on height would give explicit, tunable progression.
- No scoring yet — trivial to add as a new listener on the existing `PlayerLandedChannel`, with no changes to player or platform code.
- Only one platform prefab exists today; new *visual* variants are just new `PlatformDataSO` assets, but a genuinely new *shape* would need a second prefab.
