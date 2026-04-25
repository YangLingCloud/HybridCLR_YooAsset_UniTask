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
                if (package != null && package.Name == packageName)
                {
                    package.Version = version;
                    return;
                }
            }

            _packages.Add(new PackageVersion
            {
                Name = packageName,
                Version = version
            });
        }

        /// <summary>
        /// 清空所有包版本。
        /// </summary>
        public void ClearPackages()
        {
            _packages.Clear();
        }

        /// <summary>
        /// 迁移旧版本 JSON 字符串包版本配置。
        /// </summary>
        public bool MigrateLegacyPackages(Func<string, Dictionary<string, string>> deserialize)
        {
            if (string.IsNullOrEmpty(_packagesLegacyJson) || _packages.Count > 0 || deserialize == null)
                return false;

            var legacyPackages = deserialize(_packagesLegacyJson);
            if (legacyPackages == null || legacyPackages.Count == 0)
                return false;

            foreach (var package in legacyPackages)
            {
                SetPackageVersion(package.Key, package.Value);
            }

            _packagesLegacyJson = string.Empty;
            return true;
        }
    }
}
