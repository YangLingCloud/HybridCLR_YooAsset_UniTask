using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace YangLing.Hybrid.Runtime
{
    /// <summary>
    /// 运行时包版本信息。
    /// </summary>
    [Serializable]
    public class PackageVersion
    {
        public string Name;
        public string Version;
    }

    /// <summary>
    /// 运行时热更新配置资产，保存资源服务器地址、发行版本号以及每个 YooAsset 包的版本信息。
    /// </summary>
    [CreateAssetMenu(fileName = "HybridRuntimeSettings", menuName = "Scriptable Objects/HybridRuntimeSettings")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, "HybridRuntimeSettings")]
    public class HybridRuntimeSettings : ScriptableObject
    {
        /// <summary>
        /// 资源服务器地址
        /// </summary>
        public string HostServerIP;

        /// <summary>
        /// 发行版本
        /// </summary>
        public int ReleaseBuildVersion;

        /// <summary>
        /// 所有需要加载的包名以及对应的版本
        /// </summary>
        [SerializeField] private List<PackageVersion> _packages = new List<PackageVersion>();

        /// <summary>
        /// 旧版本的 JSON 包版本字段，仅用于一次性迁移。
        /// </summary>
        [FormerlySerializedAs("Packages")]
        [SerializeField, HideInInspector] private string _packagesLegacyJson;

        public List<PackageVersion> Packages
        {
            get => _packages;
            // 外部写入 null 时自动恢复为空列表，避免运行时遍历包配置时空引用。
            set => _packages = value ?? new List<PackageVersion>();
        }

        public string PackagesLegacyJson => _packagesLegacyJson;

        /// <summary>
        /// 设置或新增包版本。
        /// </summary>
        public void SetPackageVersion(string packageName, string version)
        {
            if (string.IsNullOrEmpty(packageName))
                return;

            foreach (var package in _packages)
            {
                // 已存在同名包时仅更新版本，保持列表中包名唯一。
                if (package != null && package.Name == packageName)
                {
                    package.Version = version;
                    return;
                }
            }

            _packages.Add(new PackageVersion
            {
                // 新包按名称和版本追加，供补丁流程逐包初始化 YooAsset。
                Name = packageName,
                Version = version
            });
        }

        /// <summary>
        /// 清空所有包版本。
        /// </summary>
        public void ClearPackages()
        {
            // 清空结构化包列表，用于重新导入快照或构建流程重写版本信息。
            _packages.Clear();
        }

        /// <summary>
        /// 迁移旧版本 JSON 字符串包版本配置。
        /// </summary>
        public bool MigrateLegacyPackages(Func<string, Dictionary<string, string>> deserialize)
        {
            if (string.IsNullOrEmpty(_packagesLegacyJson) || _packages.Count > 0 || deserialize == null)
                return false;

            // 旧版本 Packages 字段是 JSON 字符串，通过外部传入反序列化函数避免 Runtime 直接依赖 Newtonsoft。
            var legacyPackages = deserialize(_packagesLegacyJson);
            if (legacyPackages == null || legacyPackages.Count == 0)
                return false;

            foreach (var package in legacyPackages)
            {
                SetPackageVersion(package.Key, package.Value);
            }

            // 迁移成功后清空旧字段，防止下次启动重复迁移。
            _packagesLegacyJson = string.Empty;
            return true;
        }
    }
}
