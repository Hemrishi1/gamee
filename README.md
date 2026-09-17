# GardenGuardians — Mobile Tower-Defense Game

> A high-performance 2D mobile tower-defense portfolio game developed in **Unity 6 / C#** following the **EA Mobile Slingshot Studios** engineering standards.

---

## 🎮 Game Premise & Features

**GardenGuardians** is a strategic 2D grid-based tower-defense game built for mobile devices with responsive touch controls, testable pure C# pathfinding, dynamic maze building, atomic local save persistence, and a live REST API daily challenge with offline resilience.

- **Board**: Centered 8 columns × 6 rows (48 cells).
- **Core Route**: Enemies spawn at `(0, 2)` (Green Tile) and march toward the core at `(7, 2)` (Orange Tile).
- **Towers & Defenses**:
  - 🔫 **Blaster Turret** (25 Energy): 2-cell radius, 1.0s cooldown, 1 damage. Deals focused damage to nearest invaders.
  - ❄️ **Frost Spire** (35 Energy): 2-cell radius, 1.5s freeze aura (50% speed slow). Slows fast swarmers.
  - 🧱 **Barrier Wall** (15 Energy): Blocks a grid cell to force enemies into detours and killzones.
- **Dynamic BFS Pathfinding & Validation**:
  - Players can shape enemy paths with walls.
  - **Wall Placement Rule**: Walls are validated against a pure C# Breadth-First Search (BFS) algorithm before placement. If a wall would completely block the route, the action is rejected with *"Keep a route open!"* without deducting energy.
  - Active enemies reroute dynamically from their current cell without teleporting.
- **Three Progressive Levels**:
  - **Level 1 (Garden Perimeter)**: Introductory waves teaching placement fundamentals.
  - **Level 2 (Fast Surge)**: High-speed runners testing overlapping turret range.
  - **Level 3 (The Labyrinth)**: Armored brutes requiring wall mazes and slow tower chokepoints.
- **Safe Atomic Local Persistence**: Progress is serialized to JSON and saved via temporary file atomic replacement to prevent save corruption during unexpected app terminations or battery loss.
- **Daily Challenge REST API**: Fetches daily configuration payloads via HTTPS with immediate offline fallback caching.

---

## 🏛️ Project Architecture

The project is structured with clean separation of concerns and assembly definitions (`.asmdef`) for modularity and fast compilation:

```
Assets/
├── Scenes/
│   ├── Menu.unity                # Title, Level select, Daily Challenge card, settings
│   └── Game.unity                # Orthographic camera, GridRoot, GameManager, Canvas HUD
├── Scripts/
│   ├── Core/
│   │   └── GameManager.cs        # Lives, energy economy, win/loss triggers, pause
│   ├── Gameplay/
│   │   ├── BoardController.cs    # 48-cell generator, touch input, wall BFS validation, rerouting
│   │   ├── CellView.cs           # Cell state, touch/tap handler, visual highlights
│   │   ├── EnemyController.cs    # Waypoint navigation, speed/slow modifiers, flash on hit
│   │   ├── TurretController.cs   # 0.2s search timer, radius targeting, attack effects
│   │   └── WaveSpawner.cs        # Wave coroutines, alive enemy counter, phase progression
│   ├── Pathfinding/
│   │   └── GridPathfinder.cs     # Pure C# 4-direction BFS decoupled from UnityEngine
│   ├── UI/
│   │   ├── GameUIController.cs   # Mobile 16:9 canvas scaler, build selector, notifications
│   │   └── MenuUIController.cs   # Unlocked levels, daily challenge card, audio toggles
│   ├── Services/
│   │   ├── SaveService.cs        # Atomic JSON persistence (temp file replace)
│   │   └── DailyChallengeService.cs # UnityWebRequest HTTPS fetch with offline fallback
│   └── Editor/
│       ├── GameSetupUtility.cs   # ScriptableObject & Prefab factory
│       └── SceneSetupUtility.cs  # Automated scene wiring & canvas setup
├── Data/                         # ScriptableObjects for Towers, Enemies, Waves, Levels
├── Prefabs/                      # Cell, Turret, Enemy prefabs
├── Sprites/                      # Pixel-crisp 2D vector art for tiles, towers, and enemies
└── Tests/
    └── Editor/                   # NUnit automated test suites
        ├── PathfindingTests.cs   # 4 BFS algorithm unit tests
        └── SaveAndApiTests.cs    # 4 Save & REST API unit tests
```

---

## 🧪 Unit Tests (All 8 Passed)

All core rules and algorithms are covered by automated unit tests running in Unity EditMode (`unity test . --mode EditMode`):

| Test Suite | Test Case | Status | Description |
|---|---|---|---|
| `PathfindingTests` | `EmptyBoard_RouteExists` | ✅ Passed | Confirms shortest path (8 steps) exists from (0,2) to (7,2). |
| `PathfindingTests` | `OneWallOnDirectRoute_FindsDetour` | ✅ Passed | Confirms BFS discovers a detour around a single wall obstacle. |
| `PathfindingTests` | `CompleteBarrier_ReturnsNoRoute` | ✅ Passed | Confirms BFS returns null when a wall barrier seals the board. |
| `PathfindingTests` | `StartOrGoalPlacement_AlwaysRejected` | ✅ Passed | Verifies start and goal coordinates can never be blocked. |
| `SaveAndApiTests` | `DefaultSaveData_InitializedWithValidDefaults` | ✅ Passed | Validates clean schema version and defaults on first launch. |
| `SaveAndApiTests` | `SaveData_SerializationRoundTrip_MaintainsIntegrity` | ✅ Passed | Confirms JSON roundtrip maintains unlocked levels and scores. |
| `SaveAndApiTests` | `DailyChallenge_ValidJson_ParsesCorrectly` | ✅ Passed | Verifies online JSON payload correctly populates challenge data. |
| `SaveAndApiTests` | `DailyChallenge_MalformedJson_HandlesGracefully` | ✅ Passed | Confirms invalid or corrupt network response safely falls back. |

---

## ⚡ Performance Profiling & Mobile Optimization

As detailed in Section 9 of the engineering guide, target optimizations were applied to ensure smooth 60 FPS gameplay on Android:

1. **Cached BFS Recalculation**: Pathfinding is computed strictly on wall placement, never per frame.
2. **Zero-Allocation Enemy Search**: `BoardController.ActiveEnemies` maintains a direct list of live enemies, eliminating expensive `FindObjectsByType` allocations in update loops.
3. **Optimized Search Intervals**: Turrets scan for targets every 0.2s rather than running range checks on every single frame.
4. **Canvas Mobile Scaling**: UI Canvas uses `Scale With Screen Size` with a 1920×1080 reference resolution (match 0.5) to maintain crisp layout across all aspect ratios.

---

## 🚀 Building & Running

### Requirements
- Unity 6 LTS (6000.x) or Unity 2022.3 LTS
- Android Build Support (Android SDK & NDK Tools, OpenJDK)

### In Unity Editor
1. Open Unity Hub and click **Add project from disk**.
2. Select this repository directory.
3. Open `Assets/Scenes/Menu.unity` and press **Play**.

### Android APK Build
1. Go to **File -> Build Settings**.
2. Switch Platform to **Android**.
3. Ensure both `Assets/Scenes/Menu.unity` and `Assets/Scenes/Game.unity` are checked in the build list.
4. Click **Build** to produce the APK file for Android device testing.
