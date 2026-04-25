using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YangLing.Hybrid.Editor
{

/// <summary>
/// 编辑器播放场景辅助工具，支持从构建列表首场景启动 Play Mode 以模拟真实启动链路。
/// </summary>
[InitializeOnLoad]
public static class SceneHelper
{
    public static string StartSceneName = "StartScene";
    public const string MenuName = "Scene/Auto Play From First Scene";

    static SceneHelper()
    {
        EditorApplication.playModeStateChanged += OnPlayerModeStateChanged;
    }

    /// <summary>
    /// 监听 Play Mode 状态变化，在进入播放前根据菜单开关设置启动场景。
    /// </summary>
    private static void OnPlayerModeStateChanged(PlayModeStateChange playModeState)
    {
        if (playModeState != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        var currentStartScene = EditorSceneManager.GetActiveScene();
        if (Menu.GetChecked(MenuName))
        {
            // 开启菜单时强制从 Build Settings 第一个场景启动，模拟真实应用启动顺序。
            if (currentStartScene.name != StartSceneName && EditorBuildSettings.scenes.Length > 0)
            {
                var targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
                EditorSceneManager.playModeStartScene = targetScene;
            }
        }
        else
        {
            // 关闭菜单时恢复为当前打开场景，避免影响普通编辑调试流程。
            var targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentStartScene.path);
            EditorSceneManager.playModeStartScene = targetScene;
        }
    }

    /// <summary>
    /// 切换“从首场景启动播放”菜单状态。
    /// </summary>
    [MenuItem(MenuName)]
    public static void RunStartScene()
    {
        bool isRunStartScene = Menu.GetChecked(MenuName);
        Menu.SetChecked(MenuName, !isRunStartScene);
    }
}
}
