#if UNITY_2019_4_OR_NEWER
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HybridCLR.Editor.Commands;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using YangLing.Hybrid.Editor;
using YangLing.Hybrid.Editor.ScriptableBuildPipeline;
using YangLing.Hybrid.Runtime;

namespace YooAsset.Editor
{
    /// <summary>
    /// ScriptableBuildPipeline 构建视图实现，负责编排脚本包、资源包和应用构建流程。
    /// </summary>
    internal class HybridScriptableBuildPipelineViewer : HybridBuildPipeViewerBase
    {
        private HybridBuilderSettings _hybridBuilderSettings;

        public HybridScriptableBuildPipelineViewer(BuildTarget buildTarget,
            HybridBuilderSettings hybridBuilderSettings, VisualElement parent)
            : base(EBuildPipeline.ScriptableBuildPipeline, buildTarget, hybridBuilderSettings, parent)
        {
            _hybridBuilderSettings = hybridBuilderSettings;
        }

        /// <summary>
        /// 执行构建
        /// </summary>
        protected override void ExecuteBuild()
        {
            var option = _hybridBuilderSettings.hybridBuildOption;
            if (option == HybridBuildOption.None)
            {
                return;
            }

            // 根据构建模式拆分所需前置条件，避免无关校验阻塞单一类型构建。
            bool needMetadataCheck = option == HybridBuildOption.BuildScript ||
                                     option == HybridBuildOption.BuildAll;
            bool needScriptPath = option == HybridBuildOption.BuildScript ||
                                  option == HybridBuildOption.BuildApplication ||
                                  option == HybridBuildOption.BuildAll;
            bool needAssetPackages = option == HybridBuildOption.BuildAsset ||
                                     option == HybridBuildOption.BuildApplication ||
                                     option == HybridBuildOption.BuildAll;

            if (needMetadataCheck && !ValidateMetadata())
                return;
            if (needScriptPath && !ValidateScriptPath())
                return;
            if (needAssetPackages && !ValidateAssetPackages())
                return;

            StartBuild();
        }

        /// <summary>
        /// 校验热更新 DLL 是否访问了已被裁剪且未补充元数据的 AOT 类型。
        /// </summary>
        private bool ValidateMetadata()
        {
            if (BuildHelper.CheckAccessMissingMetadata())
                return true;
            Debug.unityLogger.LogError("BuildPipeline",
                "Hot-update code references stripped types. Run Build Application first");
            return false;
        }

        /// <summary>
        /// 校验脚本包收集器中是否包含 AOT 补充 DLL 与热更新 DLL 两个必需目录。
        /// </summary>
        private bool ValidateScriptPath()
        {
            if (CheckScriptPathExist())
            {
                Debug.unityLogger.Log("CheckScriptPathExist Success");
                return true;
            }
            Debug.unityLogger.LogError("CheckScriptPathExist", "CheckScriptPathExist Failed");
            return false;
        }

        /// <summary>
        /// 校验资源构建模式下至少选择了一个资源包。
        /// </summary>
        private bool ValidateAssetPackages()
        {
            if (_hybridBuilderSettings.AssetPackages != null && _hybridBuilderSettings.AssetPackages.Count > 0)
                return true;
            Debug.unityLogger.LogError("BuildPipeline", "AssetPackages is null or empty");
            return false;
        }


        /// <summary>
        /// 构建当前激活平台的应用包，目前仅实现 Android APK 构建。
        /// </summary>
        bool BuildApplication()
        {
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            switch (activeBuildTarget)
            {
                case BuildTarget.Android:
                    // Android 使用 BuildHelper.BuildAPK 串起 IL2CPP 定义、link.xml 补全和 Unity BuildPipeline。
                    return BuildHelper.BuildAPK(_hybridBuilderSettings.GetBuildOutputPath(),
                        _hybridBuilderSettings.GetCurrentVersion(true));
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.iOS:
                    Debug.unityLogger.LogError("BuildApplication",
                        $"BuildApplication for {activeBuildTarget} is not implemented yet. " +
                        "Please run Build Asset / Build Script separately, or implement the platform-specific build logic in BuildHelper.");
                    return false;
                default:
                    Debug.unityLogger.LogError("BuildApplication",
                        $"Unsupported build target: {activeBuildTarget}");
                    return false;
            }
        }

        /// <summary>
        /// 确认是否存在AOT补充Dll路径和HotUpdatePath路径（在脚本包的收集器中）
        /// </summary>
        bool CheckScriptPathExist()
        {
            if (string.IsNullOrEmpty(_hybridBuilderSettings.ScriptPackageName))
            {
                Debug.unityLogger.LogError("CheckScriptPathExist", "ScriptPackageName == Null ");
                return false;
            }

            var patchedAOTPath = _hybridBuilderSettings.PatchedAOTDLLCollectPath;
            var hotUpdatePath = _hybridBuilderSettings.HotUpdateDLLCollectPath;
            if (string.IsNullOrEmpty(patchedAOTPath) || string.IsNullOrEmpty(hotUpdatePath))
            {
                return false;
            }

            var patchedAOTDLLPathGUID = AssetDatabase.AssetPathToGUID(patchedAOTPath);
            var hotUpdateDLLPathGUID = AssetDatabase.AssetPathToGUID(hotUpdatePath);

            // YooAsset Collector 以 GUID 绑定目录，通过 GUID 比较可以避免路径大小写或重命名造成误判。
            var buildPackage = AssetBundleCollectorSettingData.Setting.GetPackage(
                _hybridBuilderSettings.ScriptPackageName);

            bool hasPatchedAOTDLLPath = false;
            bool hasHotUpdateDllPath = false;
            foreach (var group in buildPackage.Groups)
            {
                foreach (var collector in group.Collectors)
                {
                    if (!hasPatchedAOTDLLPath &&
                        string.Equals(patchedAOTDLLPathGUID, collector.CollectorGUID))
                    {
                        hasPatchedAOTDLLPath = true;
                    }
                    else if (!hasHotUpdateDllPath &&
                             string.Equals(hotUpdateDLLPathGUID, collector.CollectorGUID))
                    {
                        hasHotUpdateDllPath = true;
                    }

                    if (hasPatchedAOTDLLPath && hasHotUpdateDllPath)
                        return true;
                }
            }

            return hasPatchedAOTDLLPath && hasHotUpdateDllPath;
        }

        /// <summary>
        /// 本地打包的Packages和版本
        /// </summary>
        /// <param name="packages"></param>
        void StartBuild()
        {
            switch (_hybridBuilderSettings.hybridBuildOption)
            {
                case HybridBuildOption.BuildAll:
                    // BuildAll 需要同时输出脚本包和选中的资源包。
                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        if (!BuildScriptPackage())
                        {
                            return;
                        }


                        if (!BuildAsset(assetPackage))
                        {
                            return;
                        }
                        
                    }

                    break;
                case HybridBuildOption.BuildApplication:
                    // 应用包先构建，随后输出本次 Release 对应的脚本包和资源包。
                    if (!BuildApplication())
                    {
                        return;
                    }

                    if (!BuildScriptPackage())
                    {
                        return;
                    }

                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        if (!BuildAsset(assetPackage))
                        {
                            return;
                        }
                    }

                    break;
                case HybridBuildOption.BuildAsset:
                    // 仅构建资源包时不触发脚本 DLL 编译与复制。
                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        if (!BuildAsset(assetPackage))
                        {
                            return;
                        }
                    }

                    break;
                case HybridBuildOption.BuildScript:
                    // 仅构建脚本包时只运行 RawFile 构建管线。
                    if (!BuildScriptPackage())
                    {
                        return;
                    }
                    break;
            }

            BuildFinish();
        }

        /// <summary>
        /// 将指定包的版本号写入运行时配置。
        /// </summary>
        void UpdatePackageVersion(string packageName,int version)
        {
            _hybridBuilderSettings.RuntimeSettings.SetPackageVersion(packageName, version.ToString());
        }

        /// <summary>
        /// 推进脚本包版本：将当前版本写入 RuntimeSettings 后自增。
        /// </summary>
        void IncrementScriptPackage()
        {
            UpdatePackageVersion(_hybridBuilderSettings.ScriptPackageName, _hybridBuilderSettings.ScriptBuildVersion);
            _hybridBuilderSettings.ScriptBuildVersion++;
        }

        /// <summary>
        /// 推进所有资源包版本：将当前版本写入 RuntimeSettings 后自增。
        /// </summary>
        void IncrementAssetPackages()
        {
            foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
            {
                UpdatePackageVersion(assetPackage, _hybridBuilderSettings.AssetBuildVersion);
            }
            _hybridBuilderSettings.AssetBuildVersion++;
        }

        void BuildFinish()
        {
            bool runtimeSettingsChanged = false;
            switch (_hybridBuilderSettings.hybridBuildOption)
            {
                case HybridBuildOption.BuildAsset:
                    // 资源包构建完成后记录当前资源版本，再递增下一次构建使用的资源版本。
                    IncrementAssetPackages();
                    runtimeSettingsChanged = true;
                    break;
                case HybridBuildOption.BuildScript:
                    // 脚本包构建完成后记录当前脚本版本，再递增下一次构建使用的脚本版本。
                    IncrementScriptPackage();
                    runtimeSettingsChanged = true;
                    break;
                case HybridBuildOption.BuildAll:
                    // 同时构建时，脚本包与资源包版本都需要推进。
                    IncrementScriptPackage();
                    IncrementAssetPackages();
                    runtimeSettingsChanged = true;
                    break;
                case HybridBuildOption.BuildApplication:
                    // 为了保证一次打包所有的包 Release 版本一致，应该在打完所有包之后增加 Release 版本
                    _hybridBuilderSettings.RuntimeSettings.ReleaseBuildVersion =
                        _hybridBuilderSettings.ReleaseBuildVersion;
                    _hybridBuilderSettings.ReleaseBuildVersion++;

                    IncrementScriptPackage();
                    IncrementAssetPackages();
                    runtimeSettingsChanged = true;
                    break;
            }

            if (!runtimeSettingsChanged)
            {
                // 没有版本变化时只打开输出目录，不落盘 RuntimeSettings.json。
                EditorUtility.RevealInFinder(_hybridBuilderSettings.GetBuildOutputPath());
                return;
            }

            // RuntimeSettings.json 写到输出根目录，运行时可通过远端 URL 获取当前 Release 的包版本配置。
            var json = JsonConvert.SerializeObject(_hybridBuilderSettings.RuntimeSettings);
            var outputRoot = _hybridBuilderSettings.ResolveBuildOutputPath();
            if (!string.IsNullOrEmpty(outputRoot))
            {
                try
                {
                    if (!Directory.Exists(outputRoot))
                        Directory.CreateDirectory(outputRoot);
                    File.WriteAllText(Path.Combine(outputRoot, HybridPaths.RuntimeSettingsJson), json);
                }
                catch (Exception e)
                {
                    Debug.unityLogger.LogError("BuildFinish",
                        $"Failed to write RuntimeSettings.json to {outputRoot}: {e.Message}");
                }
            }

            EditorUtility.SetDirty(_hybridBuilderSettings.RuntimeSettings);
            EditorUtility.SetDirty(_hybridBuilderSettings);
            AssetDatabase.SaveAssets();
            EditorUtility.RevealInFinder(_hybridBuilderSettings.GetBuildOutputPath());
        }

        /// <summary>
        /// 构建脚本 RawFile 包，内部会先编译热更新 DLL 并复制 AOT/HotUpdate 字节文件。
        /// </summary>
        bool BuildScriptPackage()
        {
            HybridScriptableBuildParameters buildParameters = new HybridScriptableBuildParameters();
            // DLL 收集路径来自构建配置，并在 TaskBuildScript_SBP 中转为绝对路径复制文件。
            buildParameters.PatchedAOTDLLCollectPath = _hybridBuilderSettings.PatchedAOTDLLCollectPath;
            buildParameters.HotUpdateDLLCollectPath = _hybridBuilderSettings.HotUpdateDLLCollectPath;
            buildParameters.BuildOutputRoot = _hybridBuilderSettings.GetBuildOutputPath();

            //打包后的拷贝目录,有需求可以自行更改,建议不要设置StreamingAsset，会随包打出
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            buildParameters.BuildTarget = BuildTarget;
            buildParameters.PackageName = _hybridBuilderSettings.ScriptPackageName;
            buildParameters.BuildBundleType = (int) EBuildBundleType.RawBundle;
            buildParameters.BuildPipeline = nameof(EBuildPipeline.RawFileBuildPipeline);
            buildParameters.PackageVersion = _hybridBuilderSettings.ScriptBuildVersion.ToString();

            buildParameters.EnableSharePackRule = true;
            buildParameters.VerifyBuildingResult = true;
            buildParameters.FileNameStyle = _hybridBuilderSettings.assetFileNameStyle;
            buildParameters.BuildinFileCopyOption = _hybridBuilderSettings.assetBuildinFileCopyOption;
            buildParameters.BuildinFileCopyParams = _hybridBuilderSettings.assetBuildinFileCopyParams;
            buildParameters.CompressOption = _hybridBuilderSettings.assetCompressOption;
            buildParameters.ClearBuildCacheFiles = _hybridBuilderSettings.isClearBuildCache;
            buildParameters.UseAssetDependencyDB = _hybridBuilderSettings.isUseAssetDependDB;
            buildParameters.EncryptionServices = CreateEncryptionInstance();

            HybridScriptableBuildPipeline pipeline = new HybridScriptableBuildPipeline();
            var buildResult = pipeline.Run(buildParameters, true);
            return buildResult.Success;
        }

        /// <summary>
        /// 构建指定 YooAsset 资源包。
        /// </summary>
        bool BuildAsset(string packageName)
        {
            ScriptableBuildParameters buildParameters = new ScriptableBuildParameters();
            buildParameters.BuildOutputRoot = _hybridBuilderSettings.GetBuildOutputPath();
            
            // 资源包使用标准 ScriptableBuildPipeline，脚本包使用自定义 HybridScriptableBuildPipeline。
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            buildParameters.BuildPipeline = BuildPipeline.ToString();
            buildParameters.BuildBundleType = (int) EBuildBundleType.AssetBundle;
            buildParameters.BuildTarget = BuildTarget;
            buildParameters.PackageName = packageName;
            buildParameters.PackageVersion = _hybridBuilderSettings.AssetBuildVersion.ToString();
            buildParameters.EnableSharePackRule = true;
            buildParameters.VerifyBuildingResult = true;
            buildParameters.FileNameStyle = _hybridBuilderSettings.assetFileNameStyle;
            buildParameters.BuildinFileCopyOption = _hybridBuilderSettings.assetBuildinFileCopyOption;
            buildParameters.BuildinFileCopyParams = _hybridBuilderSettings.assetBuildinFileCopyParams;
            buildParameters.CompressOption = _hybridBuilderSettings.assetCompressOption;
            buildParameters.ClearBuildCacheFiles = _hybridBuilderSettings.isClearBuildCache;
            buildParameters.UseAssetDependencyDB = _hybridBuilderSettings.isUseAssetDependDB;
            buildParameters.EncryptionServices = CreateEncryptionInstance();

            ScriptableBuildPipeline pipeline = new ScriptableBuildPipeline();
            var buildResult = pipeline.Run(buildParameters, true);
            return buildResult.Success;
        }
    }
}
#endif
