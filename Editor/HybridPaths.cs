using System.IO;
using UnityEngine;

namespace YangLing.Hybrid.Editor
{

/// <summary>
/// 集中管理包内路径、文件名等常量，避免在多个文件中散落的魔法字符串。
/// </summary>
internal static class HybridPaths
{
    /// <summary>热更新资源目录（Assets 相对路径）</summary>
    public const string HotUpdateAssetsDir = "Assets/HotUpdateAssets";

    /// <summary>补充元数据 AOT DLL 子目录名</summary>
    public const string PatchedAOTDllFolder = "PatchedAOTDLL";

    /// <summary>热更新 DLL 子目录名</summary>
    public const string HotUpdateDllFolder = "HotUpdateDLL";

    /// <summary>HybridCLR 生成的 link.xml 相对路径（相对于 Assets）</summary>
    public const string GeneratedLinkXmlRelative = "HybridCLRData/Generated/link.xml";

    /// <summary>项目 Assets 根下的 link.xml</summary>
    public const string AssetsLinkXmlRelative = "link.xml";

    /// <summary>AOT DLL 列表清单文件名</summary>
    public const string AotDllManifest = "AOTDLLs.txt";

    /// <summary>热更新 DLL 列表清单文件名</summary>
    public const string HotUpdateDllManifest = "HotUpdateDLLs.txt";

    /// <summary>默认构建输出根目录</summary>
    public const string DefaultBundleOutputDir = "Bundles";

    /// <summary>运行时配置导出文件名</summary>
    public const string RuntimeSettingsJson = "RuntimeSettings.json";

    /// <summary>获取工程根目录（Assets 同级）</summary>
    public static string GetProjectRoot()
    {
        return Directory.GetParent(Application.dataPath).FullName;
    }
}
}

