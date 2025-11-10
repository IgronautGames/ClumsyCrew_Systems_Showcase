#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Opens the Game View window in fullscreen mode inside the Unity Editor.
/// - Hides toolbar for a true fullscreen preview.
/// - Safely closes before domain reload to prevent stuck states.
/// - Accessible via menu: Window → General → Game (Fullscreen) or shortcut <c>Ctrl+Alt+Shift+2</c>.
/// 
/// Useful for testing UI layouts, split-screen setups, or cinematic cameras.
/// </summary>
public static class FullscreenGameView
{
    // --- Reflection Setup ---
    private static readonly Type GameViewType =
        Type.GetType("UnityEditor.GameView,UnityEditor");

    private static readonly PropertyInfo ShowToolbarProperty =
        GameViewType?.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly object False = false; // Boxed constant for reuse

    private static EditorWindow instance;

    // --- Assembly Reload Safety ---
    static FullscreenGameView()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
    }

    private static void OnBeforeAssemblyReload()
    {
        // Automatically exit fullscreen to prevent Unity from freezing during recompilation
        if (instance != null)
        {
            instance.Close();
            instance = null;
        }
    }

    // --- Main Toggle Entry ---
    [MenuItem("Window/General/Game (Fullscreen) %#&2", priority = 2)]
    public static void Toggle()
    {
        if (GameViewType == null)
        {
            Debug.LogError("❌ GameView type not found.");
            return;
        }

        if (instance != null)
        {
            // Exit fullscreen
            instance.Close();
            instance = null;
        }
        else
        {
            // Create fullscreen Game View instance
            instance = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);

            // Hide toolbar if supported
            ShowToolbarProperty?.SetValue(instance, False);

            // Match desktop resolution
            Vector2 desktopResolution = new(Screen.currentResolution.width, Screen.currentResolution.height);
            Rect fullscreenRect = new(Vector2.zero, desktopResolution);

            instance.ShowPopup();
            instance.position = fullscreenRect;
            instance.Focus();

            Debug.Log("✅ Entered fullscreen Game View.");
        }
    }
}
#endif
