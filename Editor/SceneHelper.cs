using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YangLing.Hybrid.Editor
{

[InitializeOnLoad]
public static class SceneHelper
{
    public static string StartSceneName = "StartScene";
    public const string MenuName = "Scene/Auto Play From First Scene";

    static SceneHelper()
    {
        EditorApplication.playModeStateChanged += OnPlayerModeStateChanged;
    }

    private static void OnPlayerModeStateChanged(PlayModeStateChange playModeState)
    {
        if (playModeState != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        var currentStartScene = EditorSceneManager.GetActiveScene();
        if (Menu.GetChecked(MenuName))
        {
            if (currentStartScene.name != StartSceneName && EditorBuildSettings.scenes.Length > 0)
            {
                var targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
                EditorSceneManager.playModeStartScene = targetScene;
            }
        }
        else
        {
            var targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentStartScene.path);
            EditorSceneManager.playModeStartScene = targetScene;
        }
    }

    [MenuItem(MenuName)]
    public static void RunStartScene()
    {
        bool isRunStartScene = Menu.GetChecked(MenuName);
        Menu.SetChecked(MenuName, !isRunStartScene);
    }
}
}

