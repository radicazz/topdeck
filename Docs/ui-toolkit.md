# UI Toolkit HUD

This project uses a UI Toolkit-based HUD for tower health, money, round state, and the game-over menu overlay.

## Key Assets

- HUD layout: `Assets/UI/HudDocument.uxml`
- HUD styles: `Assets/UI/HudDocument.uss`
- Panel settings: `Assets/UI/HudPanelSettings.asset`
- Runtime driver: `Assets/Scripts/TowerHud.cs`

## Scene Wiring

The scene must include a `UIDocument` that points to the HUD assets.

1. Create a GameObject named `HUDDocument`.
2. Add a `UIDocument` component.
3. Assign:
   - `Panel Settings` -> `Assets/UI/HudPanelSettings.asset`
   - `Visual Tree Asset` -> `Assets/UI/HudDocument.uxml`
4. Ensure `TowerHud` (on `GameManager`) has:
   - `Hud Layout` -> `Assets/UI/HudDocument.uxml`
   - `Panel Settings` -> `Assets/UI/HudPanelSettings.asset`
   - Optional: `Hud Document` if you want to bind explicitly (otherwise `TowerHud` finds the document at runtime).

## UI Element Names

`TowerHud` queries elements by name. If you change these in UXML, update the serialized fields on `TowerHud`.

- `tower-health`
- `money`
- `round`
- `menu-overlay`
- `game-over`

Pause UI (`PauseMenuController`) uses:

- `pause-overlay`
- `resume-button`
- `quit-button`

Start UI (`TowerHud`) uses:

- `start-overlay`
- `start-button`
- `start-exit-button`

## Behavior

- `TowerHud` updates health, money, and round labels from `TowerHealth` and `GameManager` events.
- The `menu-overlay` is shown when `GameManager.IsGameOver` is true.
- The HUD scales via `PanelSettings` using a 1920x1080 reference resolution and Match 0.5.
- The start overlay is shown until `GameManager.BeginGame()` is called (via the start button).

## Pause Menu Behavior

- Press `Esc` or `P` to toggle the pause menu.
- Resume button unpauses the game (restores `Time.timeScale` to 1).
- Quit button exits the player or stops Play mode in the Editor.

## Input System Note

Pause input uses the Input System (`UnityEngine.InputSystem.Keyboard`). Ensure Project Settings -> Player -> Active Input Handling includes the Input System package.

## Troubleshooting

- If labels are blank, verify the `UIDocument` and `TowerHud` references in the scene.
- If the HUD does not scale, confirm the `PanelSettings` reference resolution/match values.
