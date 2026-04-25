using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YangLing.Hybrid.Runtime;
using YooAsset.Editor;

namespace YangLing.Hybrid.Editor
{

public class HybridBuilderWindow : EditorWindow
{
    private HybridBuilderSettings _hybridBuilderSettings;
    private List<HybridRuntimeSettings> _hybridRuntimeSettings = new List<HybridRuntimeSettings>();

    private Toolbar _toolbar;
    private ToolbarMenu _packageMenu;
    private ToolbarMenu _hybridBuilderSettingMenu;
    private ToolbarMenu _hybridRuntimeSettingMenu;
    private VisualElement _container;

    [MenuItem("HybridTool/Hybrid Builder", false, 102)]
    public static void OpenWindow()
    {
        HybridBuilderWindow window =
            GetWindow<HybridBuilderWindow>("Hybrid Builder", true, WindowsDefine.DockedWindowTypes);
        window.minSize = new Vector2(800, 600);
    }

    public void CreateGUI()
    {
        try
        {
            VisualElement root = this.rootVisualElement;

            // 加载布局文件
            var visualAsset = UxmlLoader.LoadWindowUXML<HybridBuilderWindow>();
            if (visualAsset == null)
                return;

            visualAsset.CloneTree(root);

            // 应用根容器样式
            root.AddToClassList("root-container");

            _toolbar = root.Q<Toolbar>("Toolbar");
            _container = root.Q("Container");


            var hybridBuilderSettings = FindAllAssets<HybridBuilderSettings>();
            if (hybridBuilderSettings.Count == 0)
            {
                ShowEmptyState(_toolbar, "No HybridBuilderSettings found",
                    "Please create a HybridBuilderSettings asset first.");
                return;
            }

            //HybridBuilder打包设置
            {
                _hybridBuilderSettings = hybridBuilderSettings[0];
                _hybridBuilderSettingMenu = root.Q<ToolbarMenu>("BuilderSettingMenu");
                if (_hybridBuilderSettingMenu == null)
                {
                    _hybridBuilderSettingMenu = new ToolbarMenu();
                    _hybridBuilderSettingMenu.name = "BuilderSettingMenu";
                    _hybridBuilderSettingMenu.AddToClassList("toolbar-menu");
                    _toolbar.Add(_hybridBuilderSettingMenu);
                }

                foreach (var hybridBuilderSetting in hybridBuilderSettings)
                {
                    _hybridBuilderSettingMenu.menu.AppendAction(hybridBuilderSetting.name,
                        HybridBuilderSettingMenuAction, HybridBuilderSettingMenuFun, hybridBuilderSetting);
                }
            }

            _hybridRuntimeSettings = FindAllAssets<HybridRuntimeSettings>();
            if (_hybridRuntimeSettings.Count == 0)
            {
                ShowEmptyState(_toolbar, "No HybridRuntimeSettings found",
                    "Please create a HybridRuntimeSettings asset first.");
                return;
            }

            EnsureRuntimeSettingsAssigned();
            _hybridRuntimeSettingMenu = root.Q<ToolbarMenu>("RuntimeSettingMenu");
            if (_hybridRuntimeSettingMenu == null)
            {
                _hybridRuntimeSettingMenu = new ToolbarMenu();
                _hybridRuntimeSettingMenu.name = "RuntimeSettingMenu";
                _hybridRuntimeSettingMenu.AddToClassList("toolbar-menu");
                _toolbar.Add(_hybridRuntimeSettingMenu);
            }

            foreach (var runtimeSettings in _hybridRuntimeSettings)
            {
                _hybridRuntimeSettingMenu.menu.AppendAction(runtimeSettings.name,
                    HybridBuilderRuntimeMenuAction, HybridRuntimeSettingMenuFun, runtimeSettings);
            }

            RefreshBuildPipelineView();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    private void ShowEmptyState(VisualElement parent, string title, string message)
    {
        var container = new VisualElement();
        container.AddToClassList("card");
        container.style.marginTop = 16;
        container.style.marginLeft = 16;
        container.style.marginRight = 16;

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("error-label");
        container.Add(titleLabel);

        var messageLabel = new Label(message);
        messageLabel.AddToClassList("info-label");
        container.Add(messageLabel);

        parent.Add(container);
    }

    private void RefreshBuildPipelineView()
    {
        // 清空扩展区域
        _container.Clear();
        
        _hybridBuilderSettingMenu.text = _hybridBuilderSettings.name;
        _hybridRuntimeSettingMenu.text = _hybridBuilderSettings.RuntimeSettings != null
            ? _hybridBuilderSettings.RuntimeSettings.name
            : "Runtime Settings Missing";
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;

        var viewer =
            new HybridScriptableBuildPipelineViewer(buildTarget, _hybridBuilderSettings, _container);

    }

    /// <summary>
    /// 查找工程下指定 ScriptableObject 类型的所有资产。
    /// </summary>
    private static List<T> FindAllAssets<T>() where T : ScriptableObject
    {
        var results = new List<T>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        foreach (string assetGUID in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(T))
                continue;

            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                results.Add(asset);
        }
        return results;
    }

    private void EnsureRuntimeSettingsAssigned()
    {
        if (_hybridBuilderSettings.RuntimeSettings != null || _hybridRuntimeSettings.Count == 0)
            return;

        // 仅在未配置时自动补齐，避免打开窗口时覆盖用户选择。
        _hybridBuilderSettings.RuntimeSettings = _hybridRuntimeSettings[0];
        EditorUtility.SetDirty(_hybridBuilderSettings);
    }

    void HybridBuilderRuntimeMenuAction(DropdownMenuAction action)
    {
        var targetSetting = (HybridRuntimeSettings) action.userData;
        if (_hybridBuilderSettings.RuntimeSettings != targetSetting)
        {
            _hybridBuilderSettings.RuntimeSettings = targetSetting;
            EditorUtility.SetDirty(_hybridBuilderSettings);
            RefreshBuildPipelineView();
        }
    }
    private DropdownMenuAction.Status HybridRuntimeSettingMenuFun(DropdownMenuAction action)
    {
        var targetSetting = (HybridRuntimeSettings) action.userData;
        if (_hybridBuilderSettings.RuntimeSettings == targetSetting)
            return DropdownMenuAction.Status.Checked;
        else
            return DropdownMenuAction.Status.Normal;
    }
    
    
    void HybridBuilderSettingMenuAction(DropdownMenuAction action)
    {
        var targetSetting = (HybridBuilderSettings) action.userData;
        if (_hybridBuilderSettings != targetSetting)
        {
            _hybridBuilderSettings = targetSetting;
            EnsureRuntimeSettingsAssigned();
            RefreshBuildPipelineView();
        }
    }

    private DropdownMenuAction.Status HybridBuilderSettingMenuFun(DropdownMenuAction action)
    {
        var targetSetting = (HybridBuilderSettings) action.userData;
        if (_hybridBuilderSettings == targetSetting)
            return DropdownMenuAction.Status.Checked;
        else
            return DropdownMenuAction.Status.Normal;
    }

    
}
}
