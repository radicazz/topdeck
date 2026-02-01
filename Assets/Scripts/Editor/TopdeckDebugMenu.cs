using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class TopdeckDebugMenu
{
    private const string DebugOverlayPrefKey = "Topdeck.DefenderMenuDebugOverlay";
    private const string MenuPath = "Tools/Topdeck/Debug/Defender Menu Overlay %#d";
    private const string UndoLabel = "Toggle Defender Menu Debug Overlay";

    [MenuItem(MenuPath)]
    private static void ToggleDefenderMenuOverlay()
    {
        bool enabled = !EditorPrefs.GetBool(DebugOverlayPrefKey, true);
        EditorPrefs.SetBool(DebugOverlayPrefKey, enabled);
        Menu.SetChecked(MenuPath, enabled);
        ApplyToControllers(enabled);
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleDefenderMenuOverlayValidate()
    {
        bool enabled = EditorPrefs.GetBool(DebugOverlayPrefKey, true);
        Menu.SetChecked(MenuPath, enabled);
        return true;
    }

    private static void ApplyToControllers(bool enabled)
    {
        DefenderContextMenuController[] controllers =
            Object.FindObjectsByType<DefenderContextMenuController>(FindObjectsSortMode.None);
        if (controllers == null || controllers.Length == 0)
        {
            return;
        }

        FieldInfo field = typeof(DefenderContextMenuController).GetField(
            "showDebugOverlay",
            BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (DefenderContextMenuController controller in controllers)
        {
            if (controller == null || field == null)
            {
                continue;
            }

            Undo.RecordObject(controller, UndoLabel);
            field.SetValue(controller, enabled);
            EditorUtility.SetDirty(controller);
        }
    }
}
