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

        private bool ValidateMetadata()
        {
            if (BuildHelper.CheckAccessMissingMetadata())
                return true;
            Debug.unityLogger.LogError("BuildPipeline",
                "Hot-update code references stripped types. Run Build Application first");
            return false;
        }

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

        private bool ValidateAssetPackages()
        {
            if (_hybridBuilderSettings.AssetPackages != null && _hybridBuilderSettings.AssetPackages.Count > 0)
                return true;
            Debug.unityLogger.LogError("BuildPipeline", "AssetPackages is null or empty");
            return false;
        }


        bool BuildApplication()
        {
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            switch (activeBuildTarget)
            {
                case BuildTarget.Android:
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
                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        if (!BuildAsset(assetPackage))
                        {
                            return;
                        }
                    }

                    break;
                case HybridBuildOption.BuildScript:
                    if (!BuildScriptPackage())
                    {
                        return;
                    }
                    break;
            }

            BuildFinish();
        }

        void UpdatePackageVersion(string packageName,int version)
        {
            _hybridBuilderSettings.RuntimeSettings.SetPackageVersion(packageName, version.ToString());
        }
        void BuildFinish()
        {
            bool runtimeSettingsChanged = false;
            switch (_hybridBuilderSettings.hybridBuildOption)
            {
                case HybridBuildOption.BuildAsset:
                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        UpdatePackageVersion(assetPackage, _hybridBuilderSettings.AssetBuildVersion);
                    }

                    _hybridBuilderSettings.AssetBuildVersion++;
                    runtimeSettingsChanged = true;
                    break;
                case HybridBuildOption.BuildScript:

                    UpdatePackageVersion(_hybridBuilderSettings.ScriptPackageName,_hybridBuilderSettings.ScriptBuildVersion);

                    _hybridBuilderSettings.ScriptBuildVersion++;
                    runtimeSettingsChanged = true;
                    break;
                case HybridBuildOption.BuildAll:
                    UpdatePackageVersion(_hybridBuilderSettings.ScriptPackageName,_hybridBuilderSettings.ScriptBuildVersion);
                    _hybridBuilderSettings.ScriptBuildVersion++;
                    
                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        UpdatePackageVersion(assetPackage, _hybridBuilderSettings.AssetBuildVersion);
                    }

                    _hybridBuilderSettings.AssetBuildVersion++;
                    runtimeSettingsChanged = true;
                    break;
                case HybridBuildOption.BuildApplication:
                    //为了保证一次打包所有的包Release版本一致，应该在打完所有包之后增加Release版本
                    _hybridBuilderSettings.RuntimeSettings.ReleaseBuildVersion =
                        _hybridBuilderSettings.ReleaseBuildVersion;
                    _hybridBuilderSettings.ReleaseBuildVersion++;
                    
                    UpdatePackageVersion(_hybridBuilderSettings.ScriptPackageName,_hybridBuilderSettings.ScriptBuildVersion);
                    _hybridBuilderSettings.ScriptBuildVersion++;
                    
                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        UpdatePackageVersion(assetPackage, _hybridBuilderSettings.AssetBuildVersion);
                    }

                    _hybridBuilderSettings.AssetBuildVersion++;
                    runtimeSettingsChanged = true;
                    break;
            }

            if (!runtimeSettingsChanged)
            {
                EditorUtility.RevealInFinder(_hybridBuilderSettings.GetBuildOutputPath());
                return;
            }

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

        bool BuildScriptPackage()
        {
            HybridScriptableBuildParameters buildParameters = new HybridScriptableBuildParameters();
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

        bool BuildAsset(string packageName)
        {
            ScriptableBuildParameters buildParameters = new ScriptableBuildParameters();
            buildParameters.BuildOutputRoot = _hybridBuilderSettings.GetBuildOutputPath();
            
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
