using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using YangLing.Hybrid.Runtime;

/// <summary>
/// 迁移 HybridRuntimeSettings 旧版 Packages JSON 字段到结构化列表。
/// </summary>
[InitializeOnLoad]
internal static class HybridRuntimeSettingsMigrator
{
    /// <summary>
    /// 注册延迟迁移任务，等待 AssetDatabase 初始化完成后再扫描配置资产。
    /// </summary>
    static HybridRuntimeSettingsMigrator()
    {
        EditorApplication.delayCall += MigrateAll;
    }

    /// <summary>
    /// 扫描工程内所有 HybridRuntimeSettings，将旧版 Packages JSON 字段迁移为结构化列表。
    /// </summary>
    private static void MigrateAll()
    {
        var guids = AssetDatabase.FindAssets($"t:{nameof(HybridRuntimeSettings)}");
        bool changed = false;
        foreach (var guid in guids)
        {
            // 逐个加载配置资产，迁移逻辑由 Runtime 类型自身控制，避免重复迁移已有结构化数据。
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var settings = AssetDatabase.LoadAssetAtPath<HybridRuntimeSettings>(path);
            if (settings == null)
                continue;

            if (settings.MigrateLegacyPackages(JsonConvert.DeserializeObject<Dictionary<string, string>>))
            {
                EditorUtility.SetDirty(settings);
                changed = true;
                Debug.unityLogger.Log("HybridRuntimeSettingsMigrator", $"Migrated legacy Packages JSON: {path}");
            }
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }
    }
}
