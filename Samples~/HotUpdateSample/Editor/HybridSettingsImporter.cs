using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using YangLing.Hybrid.Editor;
using YangLing.Hybrid.Runtime;

namespace YooAsset.Editor
{
    /// <summary>
    /// HybridCLR Settings 快照导入导出工具
    /// 将当前工程的 HybridCLR 配置导出为 JSON 快照文件，其他工程导入 Sample 后可一键还原配置
    /// 覆盖字段：hotUpdateAssemblyDefinitions、hotUpdateAssemblies、preserveHotUpdateAssemblies、
    ///           externalHotUpdateAssembliyDirs、patchAOTAssemblies
    /// </summary>
    public static class HybridSettingsImporter
    {
        /// <summary>
        /// 快照文件名，存放在 Sample Editor 目录下
        /// </summary>
        private const string SnapshotFileName = "HybridCLRSettingsSnapshot.json";

        /// <summary>
        /// 首次导入检测标记，存储在 SessionState 中避免同一会话重复弹窗
        /// </summary>
        private const string InitializedKey = "com.yanglingyun.hyu.Sample.Initialized";

        #region 快照数据结构

        /// <summary>
        /// HybridCLR Settings 快照数据，序列化为 JSON 存储
        /// </summary>
        [Serializable]
        private class HybridCLRSettingsSnapshot
        {
            /// <summary>
            /// 热更新程序集定义名称列表（asmdef 的 name 字段）
            /// 导入时按名称在目标工程中查找对应的 AssemblyDefinitionAsset
            /// </summary>
            public string[] hotUpdateAssemblyDefinitions = Array.Empty<string>();

            /// <summary>
            /// 热更新程序集名称（无 asmdef 的额外程序集，不含 .dll 后缀）
            /// </summary>
            public string[] hotUpdateAssemblies = Array.Empty<string>();

            /// <summary>
            /// 需要保留的热更新程序集名称（不含 .dll 后缀）
            /// </summary>
            public string[] preserveHotUpdateAssemblies = Array.Empty<string>();

            /// <summary>
            /// 外部热更新程序集搜索目录
            /// </summary>
            public string[] externalHotUpdateAssembliyDirs = Array.Empty<string>();

            /// <summary>
            /// 补充元数据的 AOT 程序集名称（不含 .dll 后缀）
            /// </summary>
            public string[] patchAOTAssemblies = Array.Empty<string>();
        }

        /// <summary>
        /// asmdef 最小反序列化结构，用于从 AssemblyDefinitionAsset.text 中提取 name
        /// </summary>
        [Serializable]
        private class AsmdefData
        {
            public string name = "";
        }

        #endregion

        #region Editor 启动自动检测

        /// <summary>
        /// Editor 启动时自动检测：
        /// 1. 若 HybridCLR Settings 未配置且存在快照文件，弹窗提示导入
        /// 2. 自动规范化 AssetBundleCollector 的资源收集路径
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(InitializedKey, false))
                    return;

                SessionState.SetBool(InitializedKey, true);

                // —— 步骤 1：HybridCLR Settings 快照导入 ——
                TryImportHybridCLRSnapshot();

                // —— 步骤 2：规范化 AssetBundleCollector 资源收集路径 ——
                NormalizeCollectorPaths();
            };
        }

        /// <summary>
        /// 检测 HybridCLR Settings 是否未配置，若存在快照文件则弹窗提示导入
        /// </summary>
        private static void TryImportHybridCLRSnapshot()
        {
            var settings = HybridCLRSettings.Instance;
            if (settings == null)
                return;

            // 已有配置则跳过
            bool hasDefinitions = settings.hotUpdateAssemblyDefinitions != null
                                  && settings.hotUpdateAssemblyDefinitions.Length > 0;
            bool hasAssemblies = settings.hotUpdateAssemblies != null
                                 && settings.hotUpdateAssemblies.Length > 0;
            bool hasPatchAOT = settings.patchAOTAssemblies != null
                               && settings.patchAOTAssemblies.Length > 0;

            if (hasDefinitions || hasAssemblies || hasPatchAOT)
                return;

            // 查找快照文件
            var snapshotPath = FindSnapshotFile();
            if (string.IsNullOrEmpty(snapshotPath))
                return;

            bool doImport = EditorUtility.DisplayDialog(
                "HybridCLR Auto Configuration",
                "HybridCLR Settings not configured, and a preset snapshot file was found.\n\n" +
                "Restore the following settings from snapshot?\n" +
                "• HotUpdateAssemblyDefinitions\n" +
                "• HotUpdateAssemblies\n" +
                "• PreserveHotUpdateAssemblies\n" +
                "• ExternalHotUpdateAssemblyDirs\n" +
                "• PatchAOTAssemblies",
                "Restore from Snapshot",
                "Configure Later");

            if (doImport)
            {
                ImportFromSnapshot();
            }
        }

        #endregion

        #region 导出 - 从当前工程导出快照

        /// <summary>
        /// 将当前工程的 HybridCLR Settings 中 5 个字段导出为 JSON 快照文件
        /// 快照保存到 Sample Editor 目录下，随 Sample 一起分发
        /// </summary>
        [MenuItem("HybridTool/Sample-HotUpdateSample/Export HybridCLR Settings Snapshot", false, 0)]
        public static void ExportSnapshot()
        {
            var settings = HybridCLRSettings.Instance;
            if (settings == null)
            {
                Debug.unityLogger.LogError("HybridSettingsImporter",
                    "HybridCLRSettings not found. Please make sure HybridCLR is installed");
                return;
            }

            var snapshot = new HybridCLRSettingsSnapshot();

            // hotUpdateAssemblyDefinitions → 提取 asmdef name
            if (settings.hotUpdateAssemblyDefinitions != null)
            {
                var names = new List<string>();
                foreach (var ad in settings.hotUpdateAssemblyDefinitions)
                {
                    if (ad != null)
                    {
                        var data = JsonUtility.FromJson<AsmdefData>(ad.text);
                        if (!string.IsNullOrEmpty(data.name))
                            names.Add(data.name);
                    }
                }
                snapshot.hotUpdateAssemblyDefinitions = names.ToArray();
            }

            // 其余 4 个字段直接复制
            snapshot.hotUpdateAssemblies = settings.hotUpdateAssemblies ?? Array.Empty<string>();
            snapshot.preserveHotUpdateAssemblies = settings.preserveHotUpdateAssemblies ?? Array.Empty<string>();
            snapshot.externalHotUpdateAssembliyDirs = settings.externalHotUpdateAssembliyDirs ?? Array.Empty<string>();
            snapshot.patchAOTAssemblies = settings.patchAOTAssemblies ?? Array.Empty<string>();

            // 序列化并写入文件
            var json = JsonUtility.ToJson(snapshot, true);
            var outputPath = GetSnapshotExportPath();

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(outputPath, json);

            Debug.Log($"[HybridSettingsImporter] Export succeeded. Snapshot exported to: {outputPath}\n{json}");
        }

        #endregion

        #region 导入 - 从快照还原到目标工程

        /// <summary>
        /// 从快照文件读取配置并写入当前工程的 HybridCLR Settings
        /// 同时自动创建 HybridBuilderSettings 和 HybridRuntimeSettings 资产（如不存在）
        /// </summary>
        [MenuItem("HybridTool/Sample-HotUpdateSample/Restore HybridCLR Settings from Snapshot", false, 1)]
        public static void ImportFromSnapshot()
        {
            var snapshotPath = FindSnapshotFile();
            if (string.IsNullOrEmpty(snapshotPath))
            {
                Debug.unityLogger.LogError("HybridSettingsImporter",
                    $"Import failed. Snapshot file {SnapshotFileName} not found. Export first or ensure the Sample is imported correctly");
                return;
            }

            var json = File.ReadAllText(snapshotPath);
            var snapshot = JsonUtility.FromJson<HybridCLRSettingsSnapshot>(json);
            if (snapshot == null)
            {
                Debug.unityLogger.LogError("HybridSettingsImporter", "Import failed. Failed to parse snapshot file");
                return;
            }

            var settings = HybridCLRSettings.Instance;
            if (settings == null)
            {
                Debug.unityLogger.LogError("HybridSettingsImporter",
                    "HybridCLRSettings not found. Please make sure HybridCLR is installed");
                return;
            }

            int configuredCount = 0;

            // 1. hotUpdateAssemblyDefinitions — 按名称查找 AssemblyDefinitionAsset
            configuredCount += ApplyHotUpdateAssemblyDefinitions(settings, snapshot.hotUpdateAssemblyDefinitions);

            // 2. hotUpdateAssemblies
            configuredCount += ApplyStringArray(settings, snapshot.hotUpdateAssemblies,
                () => settings.hotUpdateAssemblies,
                v => settings.hotUpdateAssemblies = v,
                "hotUpdateAssemblies");

            // 3. preserveHotUpdateAssemblies
            configuredCount += ApplyStringArray(settings, snapshot.preserveHotUpdateAssemblies,
                () => settings.preserveHotUpdateAssemblies,
                v => settings.preserveHotUpdateAssemblies = v,
                "preserveHotUpdateAssemblies");

            // 4. externalHotUpdateAssembliyDirs
            configuredCount += ApplyStringArray(settings, snapshot.externalHotUpdateAssembliyDirs,
                () => settings.externalHotUpdateAssembliyDirs,
                v => settings.externalHotUpdateAssembliyDirs = v,
                "externalHotUpdateAssembliyDirs");

            // 5. patchAOTAssemblies
            configuredCount += ApplyStringArray(settings, snapshot.patchAOTAssemblies,
                () => settings.patchAOTAssemblies,
                v => settings.patchAOTAssemblies = v,
                "patchAOTAssemblies");

            HybridCLRSettings.Save();

            // 自动创建 Settings 资产
            EnsureBuilderSettingsAsset();
            EnsureRuntimeSettingsAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (configuredCount > 0)
            {
                Debug.Log($"[HybridSettingsImporter] Import succeeded. Restored from snapshot and configured {configuredCount} fields");
            }
            else
            {
                Debug.Log("[HybridSettingsImporter] All configuration fields are already ready. No changes needed");
            }
        }

        #endregion

        #region 应用逻辑

        /// <summary>
        /// 按 asmdef 名称在项目中查找 AssemblyDefinitionAsset 并写入 hotUpdateAssemblyDefinitions
        /// </summary>
        private static int ApplyHotUpdateAssemblyDefinitions(HybridCLRSettings settings, string[] asmdefNames)
        {
            if (asmdefNames == null || asmdefNames.Length == 0)
                return 0;

            // 收集已配置的名称
            var existingNames = new HashSet<string>();
            if (settings.hotUpdateAssemblyDefinitions != null)
            {
                foreach (var ad in settings.hotUpdateAssemblyDefinitions)
                {
                    if (ad != null)
                    {
                        var data = JsonUtility.FromJson<AsmdefData>(ad.text);
                        existingNames.Add(data.name);
                    }
                }
            }

            // 构建项目中所有 asmdef 的名称→资产映射
            var asmdefMap = BuildAsmdefNameMap();

            var result = new List<AssemblyDefinitionAsset>();
            if (settings.hotUpdateAssemblyDefinitions != null)
                result.AddRange(settings.hotUpdateAssemblyDefinitions.Where(a => a != null));

            int added = 0;
            var notFound = new List<string>();

            foreach (var name in asmdefNames)
            {
                if (existingNames.Contains(name))
                    continue;

                if (asmdefMap.TryGetValue(name, out var asset))
                {
                    result.Add(asset);
                    added++;
                    Debug.Log($"[HybridSettingsImporter] hotUpdateAssemblyDefinitions += {name}");
                }
                else
                {
                    notFound.Add(name);
                }
            }

            if (notFound.Count > 0)
            {
                Debug.LogWarning($"[HybridSettingsImporter] The following hot-update assembly definitions were not found in the project: {string.Join(", ", notFound)}\n" +
                                 "Please ensure the corresponding .asmdef files exist under the Assets/ directory");
            }

            if (added > 0)
            {
                settings.hotUpdateAssemblyDefinitions = result.ToArray();
                EditorUtility.SetDirty(settings);
            }

            return added > 0 ? 1 : 0;
        }

        /// <summary>
        /// 通用字符串数组字段应用：将快照值写入 settings 对应字段
        /// </summary>
        private static int ApplyStringArray(
            HybridCLRSettings settings,
            string[] snapshotValues,
            Func<string[]> getter,
            Action<string[]> setter,
            string fieldName)
        {
            if (snapshotValues == null || snapshotValues.Length == 0)
                return 0;

            var current = getter() ?? Array.Empty<string>();
            var currentSet = new HashSet<string>(current);
            var toAdd = snapshotValues.Where(v => !string.IsNullOrEmpty(v) && !currentSet.Contains(v)).ToArray();

            if (toAdd.Length == 0)
                return 0;

            var merged = new List<string>(current);
            merged.AddRange(toAdd);
            setter(merged.ToArray());

            foreach (var name in toAdd)
            {
                Debug.Log($"[HybridSettingsImporter] {fieldName} += {name}");
            }

            return 1;
        }

        #endregion

        #region Settings 资产自动创建

        /// <summary>
        /// 确保 HybridBuilderSettings.asset 存在，不存在则自动创建并关联 RuntimeSettings
        /// </summary>
        private static void EnsureBuilderSettingsAsset()
        {
            var guids = AssetDatabase.FindAssets("t:HybridBuilderSettings");
            if (guids.Length > 0)
                return;

            var dir = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var runtimeSettings = FindOrCreateRuntimeSettings(dir);

            var asset = ScriptableObject.CreateInstance<HybridBuilderSettings>();
            asset.RuntimeSettings = runtimeSettings;
            AssetDatabase.CreateAsset(asset, $"{dir}/HybridBuilderSettings.asset");
            Debug.Log("[HybridSettingsImporter] Created Assets/Settings/HybridBuilderSettings.asset");
        }

        /// <summary>
        /// 确保 HybridRuntimeSettings.asset 存在，不存在则自动创建
        /// </summary>
        private static void EnsureRuntimeSettingsAsset()
        {
            var guids = AssetDatabase.FindAssets("t:HybridRuntimeSettings");
            if (guids.Length > 0)
                return;

            var dir = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "Settings");

            FindOrCreateRuntimeSettings(dir);
            Debug.Log("[HybridSettingsImporter] Created Assets/Settings/HybridRuntimeSettings.asset");
        }

        /// <summary>
        /// 查找或创建 HybridRuntimeSettings 资产
        /// </summary>
        private static HybridRuntimeSettings FindOrCreateRuntimeSettings(string dir)
        {
            var guids = AssetDatabase.FindAssets("t:HybridRuntimeSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<HybridRuntimeSettings>(path);
            }

            var asset = ScriptableObject.CreateInstance<HybridRuntimeSettings>();
            AssetDatabase.CreateAsset(asset, $"{dir}/HybridRuntimeSettings.asset");
            return asset;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 构建项目中所有 AssemblyDefinitionAsset 的 name → asset 映射
        /// </summary>
        private static Dictionary<string, AssemblyDefinitionAsset> BuildAsmdefNameMap()
        {
            var map = new Dictionary<string, AssemblyDefinitionAsset>();
            var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                if (asset == null)
                    continue;

                var data = JsonUtility.FromJson<AsmdefData>(asset.text);
                if (!string.IsNullOrEmpty(data.name) && !map.ContainsKey(data.name))
                {
                    map[data.name] = asset;
                }
            }

            return map;
        }

        /// <summary>
        /// 获取快照导出路径
        /// 优先导出到已导入的 Sample 本地目录（Assets/Samples/.../Hot Update Sample/Editor/）
        /// 仅在开发环境下回退到 Package 的 Samples~ 目录
        /// </summary>
        private static string GetSnapshotExportPath()
        {
            // 优先导出到已导入的 Sample 本地目录
            var sampleRoot = FindSampleImportRoot();
            if (!string.IsNullOrEmpty(sampleRoot))
            {
                var editorDir = Path.Combine(sampleRoot, "Editor");
                if (Directory.Exists(editorDir))
                    return Path.Combine(editorDir, SnapshotFileName).Replace("\\", "/");
            }
            // 回退：开发环境下写入仓库内的 Samples~ 目录
            var packagePath = Path.GetFullPath("Samples~/HotUpdateSample/Editor");
            if (Directory.Exists(packagePath))
                return Path.Combine(packagePath, SnapshotFileName).Replace("\\", "/");
            // 最终回退
            return Path.Combine("Samples~", "HotUpdateSample", "Editor", SnapshotFileName).Replace("\\", "/");
        }

        /// <summary>
        /// 查找快照文件
        /// 优先在已导入的 Sample 本地目录中查找，其次通过 AssetDatabase 搜索，最后回退到 Package Samples~
        /// </summary>
        private static string FindSnapshotFile()
        {
            // 1. 优先在已导入的 Sample 本地目录中查找
            var sampleRoot = FindSampleImportRoot();
            if (!string.IsNullOrEmpty(sampleRoot))
            {
                var localPath = Path.Combine(sampleRoot, "Editor", SnapshotFileName).Replace("\\", "/");
                if (File.Exists(localPath))
                    return localPath;
            }
            // 2. 通过 AssetDatabase 搜索（兼容其他位置）
            var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(SnapshotFileName));
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(SnapshotFileName))
                    return path;
            }
            // 3. 回退到仓库内 Samples~（开发环境）
            var packagePath = Path.GetFullPath($"Samples~/HotUpdateSample/Editor/{SnapshotFileName}");
            if (File.Exists(packagePath))
                return packagePath;
            return null;
        }

        #endregion

        #region AssetBundleCollector 路径规范化

        /// <summary>
        /// Sample 中 AssetBundleCollectorSetting 的 CollectPath 使用相对于 Sample 根目录的路径存储
        /// 导入 Sample 后，通过此菜单命令将相对路径转换为当前工程中的实际 Assets/ 路径
        /// </summary>
        [MenuItem("HybridTool/Sample-HotUpdateSample/Normalize Collector Paths", false, 2)]
        public static void NormalizeCollectorPaths()
        {
            var sampleRoot = FindSampleImportRoot();
            if (string.IsNullOrEmpty(sampleRoot))
            {
                Debug.unityLogger.LogError("HybridSettingsImporter",
                    "Hot Update Sample import path not found. Please ensure the Sample is imported correctly");
                return;
            }

            var setting = AssetBundleCollectorSettingData.Setting;
            if (setting == null)
            {
                Debug.unityLogger.LogError("HybridSettingsImporter",
                    "AssetBundleCollectorSetting not found. Please ensure YooAsset is installed");
                return;
            }

            int fixedCount = 0;
            foreach (var package in setting.Packages)
            {
                foreach (var group in package.Groups)
                {
                    foreach (var collector in group.Collectors)
                    {
                        // 跳过已经是有效 Assets/ 路径的条目
                        if (collector.CollectPath.StartsWith("Assets/"))
                        {
                            // 检查路径是否实际存在，不存在则尝试基于 Sample 根目录修正
                            if (AssetDatabase.IsValidFolder(collector.CollectPath) ||
                                !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(collector.CollectPath)))
                                continue;

                            // 路径无效，尝试提取相对部分并基于 Sample 根目录重建
                            var relativePart = ExtractRelativePart(collector.CollectPath);
                            var newPath = $"{sampleRoot}/{relativePart}";
                            if (AssetDatabase.IsValidFolder(newPath) ||
                                !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(newPath)))
                            {
                                Debug.Log($"[HybridSettingsImporter] CollectPath: {collector.CollectPath} → {newPath}");
                                collector.CollectPath = newPath;
                                fixedCount++;
                            }
                            continue;
                        }

                        // 相对路径：直接拼接 Sample 根目录
                        var resolvedPath = $"{sampleRoot}/{collector.CollectPath}";
                        Debug.Log($"[HybridSettingsImporter] CollectPath: {collector.CollectPath} → {resolvedPath}");
                        collector.CollectPath = resolvedPath;
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0)
            {
                AssetBundleCollectorSettingData.SaveFile();
                Debug.Log($"[HybridSettingsImporter] Normalized {fixedCount} collector paths. Sample root: {sampleRoot}");
            }
            else
            {
                Debug.Log("[HybridSettingsImporter] All collector paths are valid, no changes needed");
            }
        }

        /// <summary>
        /// 从 Assets/ 开头的路径中提取相对部分
        /// 例如 Assets/HotUpdateAssets/Textures → HotUpdateAssets/Textures
        /// </summary>
        private static string ExtractRelativePart(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return assetPath;
            // 移除 Assets/ 前缀
            if (assetPath.StartsWith("Assets/"))
                return assetPath.Substring("Assets/".Length);
            return assetPath;
        }

        /// <summary>
        /// 查找 Hot Update Sample 在当前工程中的导入根目录
        /// 通过搜索 Sample 特征文件（HybridSettingsImporter 脚本自身）定位
        /// 返回格式：Assets/Samples/.../Hot Update Sample
        /// </summary>
        private static string FindSampleImportRoot()
        {
            // 通过查找本脚本自身来定位 Sample 的 Editor 目录
            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript HybridSettingsImporter");
            foreach (var guid in scriptGuids)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                // 脚本位于 .../Hot Update Sample/Editor/HybridSettingsImporter.cs
                // Sample 根目录是 Editor 的父目录
                if (scriptPath.Contains("Hot Update Sample") && scriptPath.StartsWith("Assets/"))
                {
                    var editorDir = Path.GetDirectoryName(scriptPath);
                    if (!string.IsNullOrEmpty(editorDir))
                    {
                        var sampleRoot = Path.GetDirectoryName(editorDir);
                        if (!string.IsNullOrEmpty(sampleRoot))
                            return sampleRoot.Replace("\\", "/");
                    }
                }
            }

            // 回退：搜索 HotUpdateAssets 文件夹
            var folderGuids = AssetDatabase.FindAssets("HotUpdateAssets t:Folder");
            foreach (var guid in folderGuids)
            {
                var folderPath = AssetDatabase.GUIDToAssetPath(guid);
                if (folderPath.Contains("Samples") && folderPath.Contains("Hot Update Sample"))
                {
                    // HotUpdateAssets 的父目录即为 Sample 根目录
                    var sampleRoot = Path.GetDirectoryName(folderPath);
                    if (!string.IsNullOrEmpty(sampleRoot))
                        return sampleRoot.Replace("\\", "/");
                }
            }

            return null;
        }

        #endregion
    }
}
