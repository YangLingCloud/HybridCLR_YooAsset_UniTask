using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using YangLing.Hybrid.Runtime;
using YooAsset;
using YooAsset.Editor;

namespace YangLing.Hybrid.Editor
{

/// <summary>
/// Hybrid 构建模式，用于描述一次构建任务需要执行的资产、脚本和应用打包范围。
/// </summary>
public enum HybridBuildOption
{
    /// <summary>
    /// 不进行任何构建
    /// </summary>
    None,
    /// <summary>
    /// 构建热更新资产与所有脚本
    /// </summary>
    BuildAll,

    /// <summary>
    /// 构建热更新资产
    /// </summary>
    BuildAsset,

    /// <summary>
    /// 构建AOT以及热更新脚本
    /// 并复制到指定文件夹
    /// </summary>
    BuildScript,

    /// <summary>
    /// 构建资产与脚本
    /// 并打包应用程序
    /// </summary>
    BuildApplication,
}

/// <summary>
/// Hybrid Builder 构建配置资产，保存资源包、脚本包、输出目录、版本号和 YooAsset 构建参数。
/// </summary>
[CreateAssetMenu(fileName = "HybridBuilderSettings", menuName = "Scriptable Objects/HybridBuilderSettings")]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, "HybridBuilderSettings")]
public class HybridBuilderSettings : ScriptableObject
{
    /// <summary>
    /// ScriptableObject 启用时初始化默认输出目录，保证新建配置资产可以直接参与构建。
    /// </summary>
    void OnEnable()
    {
        if (string.IsNullOrEmpty(_buildOutputPath))
        {
            // 默认使用相对于工程根目录的路径
            buildOutputPath = HybridPaths.DefaultBundleOutputDir;
        }
    }

    /// <summary>
    /// 字段赋值的统一入口：仅在值发生变化时写回并标记 dirty，避免重复 SetDirty。
    /// </summary>
    private void SetField<T>(ref T field, T value)
    {
        // 相同值不重复写回，避免 UI 刷新或 OnEnable 阶段产生无意义的 dirty 标记。
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        EditorUtility.SetDirty(this);
    }

    public HybridRuntimeSettings RuntimeSettings;


    /// <summary>
    /// 构建的所有资源包包名
    /// </summary>
    public List<string> AssetPackages = new List<string>();


    /// <summary>
    /// 代码包包名
    /// </summary>
    public string ScriptPackageName
    {
        get => scriptPackageName;
        set => SetField(ref scriptPackageName, value);
    }

    [SerializeField] private string scriptPackageName;

    /// <summary>
    /// 打包输出路径（支持相对路径，相对于工程根目录）
    /// </summary>
    public string buildOutputPath
    {
        get => _buildOutputPath;
        set => SetField(ref _buildOutputPath, value);
    }

    [SerializeField] private string _buildOutputPath;

    /// <summary>
    /// 获取构建输出的完整路径（含版本号子目录）
    /// 若 buildOutputPath 为相对路径，则基于工程根目录解析为绝对路径
    /// </summary>
    public string GetBuildOutputPath()
    {
        // YooAsset 输出目录按 Release 版本号分层，方便主包版本与热更新包版本隔离。
        var resolvedPath = ResolveBuildOutputPath();
        return Path.Combine(resolvedPath, _releaseBuildVersion.ToString());
    }

    /// <summary>
    /// 将 buildOutputPath 解析为绝对路径
    /// 相对路径基于工程根目录（Application.dataPath 的父目录）
    /// </summary>
    public string ResolveBuildOutputPath()
    {
        if (string.IsNullOrEmpty(_buildOutputPath))
            return string.Empty;
        if (Path.IsPathRooted(_buildOutputPath))
            return _buildOutputPath;
        // 相对路径基于工程根目录解析
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, _buildOutputPath));
    }

    /// <summary>
    /// 补充元数据AOTDLL路径 收集器组合 名称
    /// 在构建时,会遍历当前包中所有的 AssetBundleCollector 进行对比
    /// 如果没有将会报错
    /// </summary>
    public DefaultAsset PatchedAOTDLLFolder
    {
        get => _patchedAOTDLLFolder;
        set => SetField(ref _patchedAOTDLLFolder, value);
    }
    [SerializeField] private DefaultAsset _patchedAOTDLLFolder;

    public string PatchedAOTDLLCollectPath
    {
        get
        {
            if (!_patchedAOTDLLFolder)
            {
                Debug.unityLogger.LogError("HybridBuilderSettings",
                    "PatchedAOTDLLFolder is not assigned");
                return string.Empty;
            }
            // 返回 Assets 相对路径，后续构建任务会再转换为项目根下的绝对路径。
            var patchedAOTDLLPath = AssetDatabase.GetAssetPath(_patchedAOTDLLFolder);
            return patchedAOTDLLPath;
        }
    }

    /// <summary>
    /// 热更新Dll路径 收集器组合 名称
    /// 在构建时,会遍历当前包中的所有的 AssetBundleCollector 进行对比
    /// 如果没有将会报错
    /// </summary>
    public DefaultAsset HotUpdateDLLFolder
    {
        get => _hotUpdateDLLFolder;
        set => SetField(ref _hotUpdateDLLFolder, value);
    }
    [SerializeField] private DefaultAsset _hotUpdateDLLFolder;

    public string HotUpdateDLLCollectPath
    {
        get
        {
            if (!_hotUpdateDLLFolder)
            {
                Debug.unityLogger.LogError("HybridBuilderSettings",
                    "HotUpdateDLLFolder is not assigned");
                return string.Empty;
            }
            // 返回 YooAsset Collector 使用的 Assets 相对路径。
            var hotUpdateDLLPath = AssetDatabase.GetAssetPath(_hotUpdateDLLFolder);
            return hotUpdateDLLPath;
        }
    }
    /// <summary>
    /// 发行版本
    /// </summary>
    [SerializeField] private int _releaseBuildVersion = 0;

    public int ReleaseBuildVersion
    {
        get => _releaseBuildVersion;
        set => SetField(ref _releaseBuildVersion, value);
    }

    /// <summary>
    /// 资源构建版本
    /// </summary>
    [SerializeField] private int _assetBuildVersion;

    public int AssetBuildVersion
    {
        get => _assetBuildVersion;
        set => SetField(ref _assetBuildVersion, value);
    }

    /// <summary>
    /// 脚本构建版本
    /// </summary>
    [SerializeField] private int _scriptBuildVersion = 0;

    public int ScriptBuildVersion
    {
        get => _scriptBuildVersion;
        set => SetField(ref _scriptBuildVersion, value);
    }

    /// <summary>
    /// 是否使用自增版本
    /// </summary>
    public bool isUseSelfIncrementingVersions
    {
        get => _isUseSelfIncrementingVersions;
        set => SetField(ref _isUseSelfIncrementingVersions, value);
    }

    [SerializeField] private bool _isUseSelfIncrementingVersions;

    /// <summary>
    /// 是否清除构建缓存
    /// 当不勾选此项的时候，引擎会开启增量打包模式，会极大提高构建速度！
    /// </summary>
    public bool isClearBuildCache
    {
        get => _isClearBuildCache;
        set => SetField(ref _isClearBuildCache, value);
    }

    [SerializeField] private bool _isClearBuildCache;

    /// <summary>
    /// 在资源收集过程中，使用资源依赖关系数据库。
    /// 当开启此项的时候，会极大提高构建速度！
    /// </summary>
    public bool isUseAssetDependDB
    {
        get => _isUseAssetDependDB;
        set => SetField(ref _isUseAssetDependDB, value);
    }

    [SerializeField] private bool _isUseAssetDependDB;


    /// <summary>
    /// AB包加密方式
    /// </summary>
    public string assetEncryptionClassName
    {
        get => _assetEncryptionClassName;
        set => SetField(ref _assetEncryptionClassName, value);
    }

    [FormerlySerializedAs("_assetEncyptionClassName")]
    [SerializeField] private string _assetEncryptionClassName;


    /// <summary>
    /// AB包压缩方式
    /// </summary>
    public ECompressOption assetCompressOption
    {
        get => _assetCompressOption;
        set => SetField(ref _assetCompressOption, value);
    }

    [SerializeField] private ECompressOption _assetCompressOption;

    /// <summary>
    /// AB包命名方式
    /// </summary>
    public EFileNameStyle assetFileNameStyle
    {
        get => _assetFileNameStyle;
        set => SetField(ref _assetFileNameStyle, value);
    }

    [SerializeField] private EFileNameStyle _assetFileNameStyle;


    /// <summary>
    /// 首包copy选项
    /// </summary>
    public EBuildinFileCopyOption assetBuildinFileCopyOption
    {
        get => _assetBuildinFileCopyOption;
        set => SetField(ref _assetBuildinFileCopyOption, value);
    }

    [SerializeField] private EBuildinFileCopyOption _assetBuildinFileCopyOption;

    /// <summary>
    /// copy选项参数
    /// </summary>
    public string assetBuildinFileCopyParams
    {
        get => _assetBuildinFileCopyParams;
        set => SetField(ref _assetBuildinFileCopyParams, value);
    }

    [SerializeField] private string _assetBuildinFileCopyParams;

    /// <summary>
    /// 混合构建选项
    /// </summary>
    public HybridBuildOption hybridBuildOption
    {
        get => _hybridBuildOption;
        set => SetField(ref _hybridBuildOption, value);
    }

    [SerializeField] private HybridBuildOption _hybridBuildOption;


    public string GetCurrentVersion(bool isBuild)
    {
        var buildVersion = string.Empty;
        if (isBuild)
        {
            // 构建版本用于目录和包版本标识，必须保持三段数字下划线格式。
            buildVersion =
                $"{_releaseBuildVersion}_{_assetBuildVersion}_{_scriptBuildVersion}";
        }
        else
        {
            // 展示版本用于编辑器窗口阅读，保留标签以降低误读版本段的概率。
            buildVersion =
                $"Release:{_releaseBuildVersion} AssetPackage:{_assetBuildVersion} ScriptPackage:{_scriptBuildVersion}";
        }

        return buildVersion;
    }

}
}
