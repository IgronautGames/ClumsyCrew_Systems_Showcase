#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Quick scene switching utility for Unity Editor.
/// Adds menu items for instantly opening key project scenes.
/// 
/// Features:
/// - Opens scenes directly from the "Scenes" menu.
/// - Prompts to save modified scenes before switching.
/// - Includes shortcut keys (Shift + 1–4).
/// 
/// Great for improving iteration speed during testing.
/// </summary>
public static class SceneSwitcherMenu
{
    private const string INTRO_SCENE = "Assets/Scenes/Intro.unity";
    private const string MAIN_SCENE = "Assets/Scenes/Main.unity";
    private const string TEST_SCENE = "Assets/Scenes/Testing.unity";
    private const string FEATURE_TEST_SCENE = "Assets/Scenes/FeatureTest.unity";

    [MenuItem("Scenes/Open Intro Scene %#1", false, 1)] // Shift+1
    public static void OpenIntro() => OpenScene(INTRO_SCENE);

    [MenuItem("Scenes/Open Main Scene %#2", false, 2)] // Shift+2
    public static void OpenMain() => OpenScene(MAIN_SCENE);

    [MenuItem("Scenes/Open Test Scene %#3", false, 3)] // Shift+3
    public static void OpenTest() => OpenScene(TEST_SCENE);

    [MenuItem("Scenes/Open Feature Test Scene %#4", false, 4)] // Shift+4
    public static void OpenFeatureTest() => OpenScene(FEATURE_TEST_SCENE);

    /// <summary>
    /// Opens a scene safely, asking to save modified scenes before switching.
    /// </summary>
    private static void OpenScene(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
            UnityEngine.Debug.Log($"✅ Scene opened: {path}");
        }
    }
}
#endif
