# Repository Guidelines

## Project Structure & Module Organization
- Unity project root with core configuration in `ProjectSettings/` and dependency list in `Packages/manifest.json`.
- Game content lives in `Assets/`; scenes are in `Assets/Scenes/` (e.g., `Assets/Scenes/SceneGame.unity`).
- C# scripts should live under `Assets/` (create `Assets/Scripts/` as needed). Existing examples are in `Assets/TutorialInfo/Scripts/`.
- Editor-only tooling should go in `Assets/**/Editor/` (see `Assets/TutorialInfo/Scripts/Editor/`).

## Build, Test, and Development Commands
- Open the project in Unity Hub with Unity **6000.3.4f1** (per `README.md`).
- Run locally: open the scene and press Play in the Unity Editor.
- Build: use **File → Build Settings** in the Unity Editor (no custom build scripts are defined here).
- Tests: use **Window → General → Test Runner** (Unity Test Framework is included).

## Coding Style & Naming Conventions
- C# formatting: 4-space indentation, braces on the same line as declarations.
- Naming: `PascalCase` for classes, files, and public members; `camelCase` for locals and private fields.
- Script file names should match the main class name (Unity convention).
- Keep Unity component/serialized fields grouped and clearly labeled in inspectors.
- Keep code clean and maintainable: small focused classes, clear method names, and minimal side effects.

## Testing Guidelines
- Framework: Unity Test Framework (`com.unity.test-framework`).
- Place tests under `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`.
- Name test files `*Tests.cs` and test classes `SomethingTests`.
- Run tests from the Unity Test Runner; there is no custom CLI test harness yet.

## Commit & Pull Request Guidelines
- Use Conventional Commits prefixes (`feat:`, `fix:`, `chore:`, etc.) with a short, imperative summary.
- Break work into a series of logical commits (one change set per commit) to keep history easy to review.
- PRs should include a brief summary, testing notes (or “not tested”), and screenshots/GIFs for gameplay or scene changes.
- Link related issues when applicable.

## Configuration Tips
- Treat `ProjectSettings/` and `Packages/` as source-of-truth configuration.
- Avoid committing generated/editor-specific folders like `Library/`, `Logs/`, and `UserSettings/` unless explicitly required.

## Unity MCP Tooling
- Prefer the Unity MCP tools for scene/object/asset/script operations over manual edits to `.meta` files or YAML assets; let Unity generate and manage metadata.
- Use MCP operations for creating/modifying GameObjects, components, materials, prefabs, and scripts to keep changes consistent with the Editor.

## Unity MCP Cheat Sheet
- Locate objects: use `find_gameobjects` (by name/tag/path) then read `mcpforunity://scene/gameobject/{id}` for details.
- Create/modify objects: `manage_gameobject` for create/rename/move/duplicate/delete; `manage_components` for add/remove/set properties.
- Assets & scripts: `manage_asset` for project assets, `create_script`/`manage_script`/`script_apply_edits` for C# changes.
- Scenes & prefabs: `manage_scene` for load/save/screenshot; `manage_prefabs` for create/open/save prefab stages.
- Efficiency: use `batch_execute` for multi-step changes and `refresh_unity` after larger edits.

## Project Update Summary (Jan 2026)
- Procedural terrain mesh is generated at runtime via a lightweight WFC-style grid, with 3+ paths converging at the center and a path overlay mesh for visibility.
- Scene wiring: Terrain, Tower (health + auto-attack), EnemySpawner, GameManager, DefenderPlacementManager, and HUD are set up in `Assets/Scenes/SceneGame.unity` using cubes as placeholders.
- Enemies spawn from path-based locations, move along paths to the tower, attack defenders in range, and damage the tower when close; enemies have health and can be killed.
- Defender placement uses predetermined, non-path grid cells; clicking a placement spot spawns a defender cube with health and auto-attack.
- Round-based economy: start at Round 1 with $200; defenders cost $100; kills reward $50; Round 1 spawns 3 enemies and later rounds scale enemy count and stats.
- HUD (1080p reference scaling) shows tower HP, money, round counter, and GAME OVER; round counter reflects prep/in-progress state.
