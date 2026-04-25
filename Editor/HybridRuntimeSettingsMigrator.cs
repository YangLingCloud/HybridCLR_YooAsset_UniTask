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
    static HybridRuntimeSettingsMigrator()
    {
        EditorApplication.delayCall += MigrateAll;
    }

    private static void MigrateAll()
    {
        var guids = AssetDatabase.FindAssets($"t:{nameof(HybridRuntimeSettings)}");
        bool changed = false;
        foreach (var guid in guids)
        {
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
