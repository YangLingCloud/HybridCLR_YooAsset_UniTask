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

/// <summary>
/// Hybrid Builder 编辑器窗口，负责加载构建配置、运行时配置，并承载 YooAsset 构建流水线视图。
/// </summary>
public class HybridBuilderWindow : EditorWindow
{
    private HybridBuilderSettings _hybridBuilderSettings;
    private List<HybridRuntimeSettings> _hybridRuntimeSettings = new List<HybridRuntimeSettings>();

    private Toolbar _toolbar;
    private ToolbarMenu _packageMenu;
    private ToolbarMenu _hybridBuilderSettingMenu;
    private ToolbarMenu _hybridRuntimeSettingMenu;
    private VisualElement _container;

    /// <summary>
    /// 打开 Hybrid Builder 编辑器窗口。
    /// </summary>
    [MenuItem("HybridTool/Hybrid Builder", false, 102)]
    public static void OpenWindow()
    {
        HybridBuilderWindow window =
            GetWindow<HybridBuilderWindow>("Hybrid Builder", true, WindowsDefine.DockedWindowTypes);
        window.minSize = new Vector2(800, 600);
    }

    /// <summary>
    /// 创建 UI Toolkit 界面并绑定构建配置、运行时配置和构建流水线视图。
    /// </summary>
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
                // 没有构建配置时显示空状态，不抛异常，避免窗口打开失败。
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
                // 运行时配置缺失时仍保持窗口可见，方便用户按提示补齐配置资产。
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

    /// <summary>
    /// 在工具栏区域显示缺失配置提示。
    /// </summary>
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

    /// <summary>
    /// 根据当前选中的构建配置刷新流水线参数面板。
    /// </summary>
    private void RefreshBuildPipelineView()
    {
        // 清空扩展区域
        _container.Clear();
        
        // 工具栏菜单文本始终反映当前配置资产，降低多配置项目中的误操作概率。
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
            // 只接受主资产类型完全匹配的对象，避免子资产或同名脚本误入列表。
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(T))
                continue;

            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                results.Add(asset);
        }
        return results;
    }

    /// <summary>
    /// 当构建配置尚未关联运行时配置时，自动选择第一个可用配置作为默认值。
    /// </summary>
    private void EnsureRuntimeSettingsAssigned()
    {
        if (_hybridBuilderSettings.RuntimeSettings != null || _hybridRuntimeSettings.Count == 0)
            return;

        // 仅在未配置时自动补齐，避免打开窗口时覆盖用户选择。
        _hybridBuilderSettings.RuntimeSettings = _hybridRuntimeSettings[0];
        EditorUtility.SetDirty(_hybridBuilderSettings);
    }

    /// <summary>
    /// 运行时配置菜单点击回调，切换当前构建配置关联的 RuntimeSettings。
    /// </summary>
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

    /// <summary>
    /// 返回运行时配置菜单项的勾选状态。
    /// </summary>
    private DropdownMenuAction.Status HybridRuntimeSettingMenuFun(DropdownMenuAction action)
    {
        var targetSetting = (HybridRuntimeSettings) action.userData;
        if (_hybridBuilderSettings.RuntimeSettings == targetSetting)
            return DropdownMenuAction.Status.Checked;
        else
            return DropdownMenuAction.Status.Normal;
    }
    
    
    /// <summary>
    /// 构建配置菜单点击回调，切换当前编辑的 HybridBuilderSettings。
    /// </summary>
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

    /// <summary>
    /// 返回构建配置菜单项的勾选状态。
    /// </summary>
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
