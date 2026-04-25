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
            _toolbar = root.Q<Toolbar>("Toolbar");
            _container = root.Q("Container");
            

            var hybridBuilderSettings = FindAllHybridBuilderSettings();
            if (hybridBuilderSettings.Count == 0)
            {
                var label = new Label();
                label.text = "Not found any HybridBuilderSetting";
                label.style.width = 100;
                _toolbar.Add(label);
                return;
            }

            //HybridBuilder打包设置
            {
                _hybridBuilderSettings = hybridBuilderSettings[0];
                _hybridBuilderSettingMenu = new ToolbarMenu();
                _hybridBuilderSettingMenu.style.width = 200;
                foreach (var hybridBuilderSetting in hybridBuilderSettings)
                {
                    _hybridBuilderSettingMenu.menu.AppendAction(hybridBuilderSetting.name,
                        HybridBuilderSettingMenuAction, HybridBuilderSettingMenuFun, hybridBuilderSetting);
                }

                _toolbar.Add(_hybridBuilderSettingMenu);
            }
            _hybridRuntimeSettings = FindAllHybridRuntimeSettings();
            if (_hybridRuntimeSettings.Count == 0)
            {
                var label = new Label();
                label.text = "Not found any hybridRuntimeSetting";
                label.style.width = 100;
                _toolbar.Add(label);
                return;
            }

            EnsureRuntimeSettingsAssigned();
            _hybridRuntimeSettingMenu = new ToolbarMenu();
            _hybridRuntimeSettingMenu.style.width = 200;
            foreach (var runtimeSettings in _hybridRuntimeSettings)
            {
                _hybridRuntimeSettingMenu.menu.AppendAction(runtimeSettings.name,
                    HybridBuilderRuntimeMenuAction, HybridRuntimeSettingMenuFun, runtimeSettings);
            }

            _toolbar.Add(_hybridRuntimeSettingMenu);
            
            
            RefreshBuildPipelineView();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
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
    /// 查找工程下所有HybridBuilderSetting类型文件
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    List<HybridBuilderSettings> FindAllHybridBuilderSettings()
    {
        var hybridBuilderSettings = new List<HybridBuilderSettings>();
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(HybridBuilderSettings)}");
        foreach (string assetGUID in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType == typeof(HybridBuilderSettings))
            {
                var hybridBuilderSetting = AssetDatabase.LoadAssetAtPath<HybridBuilderSettings>(assetPath);
                if (hybridBuilderSetting != null)
                    hybridBuilderSettings.Add(hybridBuilderSetting);
            }
        }

        return hybridBuilderSettings;
    }
    
    /// <summary>
    /// 查找工程下所有HybridRuntimeSettings类型文件
    /// </summary>
    List<HybridRuntimeSettings> FindAllHybridRuntimeSettings()
    {
        var hybridRuntimeSettings = new List<HybridRuntimeSettings>();
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(HybridRuntimeSettings)}");
        foreach (string assetGUID in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType == typeof(HybridRuntimeSettings))
            {
                var hybridRuntimeSetting = AssetDatabase.LoadAssetAtPath<HybridRuntimeSettings>(assetPath);
                if (hybridRuntimeSetting != null)
                    hybridRuntimeSettings.Add(hybridRuntimeSetting);
            }
        }

        return hybridRuntimeSettings;
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
