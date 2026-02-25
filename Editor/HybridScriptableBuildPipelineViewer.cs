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

namespace YooAsset.Editor
{
    internal class HybridScriptableBuildPipelineViewer : HybridBuildPipeViewerBase
    {
        private HybridBuilderSettings _hybridBuilderSettings;

        Dictionary<string,string> RuntimePackages;
        public HybridScriptableBuildPipelineViewer(BuildTarget buildTarget,
            HybridBuilderSettings hybridBuilderSettings, VisualElement parent)
            : base(EBuildPipeline.ScriptableBuildPipeline, buildTarget, hybridBuilderSettings, parent)
        {
            _hybridBuilderSettings = hybridBuilderSettings;
            RuntimePackages=new Dictionary<string, string>();
        }

        /// <summary>
        /// 执行构建
        /// </summary>
        protected override void ExecuteBuild()
        {
            if (_hybridBuilderSettings.hybridBuildOption == HybridBuildOption.None)
            {
                return;
            }

            switch (_hybridBuilderSettings.hybridBuildOption)
            {
                case HybridBuildOption.BuildScript:
                    if (!BuildHelper.CheckAccessMissingMetadata())
                    {
                        Debug.unityLogger.LogError("BuildPiepeline", "Hot-update code references stripped types. Run Build Application first");
                        return;
                    }

                    if (CheckScriptPathExsist())
                    {
                        Debug.unityLogger.Log($"CheckScriptPathExsist Success");
                    }
                    else
                    {
                        Debug.unityLogger.LogError("CheckScriptPathExsist", $"CheckScriptPathExsist Failed");
                        return;
                    }

                    break;
                case HybridBuildOption.BuildApplication:
                    if (CheckScriptPathExsist())
                    {
                        Debug.unityLogger.Log($"CheckScriptPathExsist Success");
                    }
                    else
                    {
                        Debug.unityLogger.LogError("CheckScriptPathExsist", $"CheckScriptPathExsist Failed");
                        return;
                    }

                    if (_hybridBuilderSettings.AssetPackages == null || _hybridBuilderSettings.AssetPackages.Count == 0)
                    {
                        Debug.unityLogger.LogError("BuildPiepeline", $"_hybridBuilderSettings.AssetPackages ==Null or empty");
                        return;
                    }

                    break;
                case HybridBuildOption.BuildAll:
                    if (!BuildHelper.CheckAccessMissingMetadata())
                    {
                        Debug.unityLogger.LogError("BuildPiepeline", "Hot-update code references stripped types. Run Build Application first");
                        return;
                    }

                    if (CheckScriptPathExsist())
                    {
                        Debug.unityLogger.Log($"CheckScriptPathExsist Success");
                    }
                    else
                    {
                        Debug.unityLogger.LogError("CheckScriptPathExsist", $"CheckScriptPathExsist Failed");
                        return;
                    }
                    
                    if (_hybridBuilderSettings.AssetPackages == null || _hybridBuilderSettings.AssetPackages.Count == 0)
                    {
                        Debug.unityLogger.LogError("BuildPiepeline", $"_hybridBuilderSettings.AssetPackages ==Null or empty");
                        return;
                    }

                    break;
                case HybridBuildOption.BuildAsset:
                    if (_hybridBuilderSettings.AssetPackages == null || _hybridBuilderSettings.AssetPackages.Count == 0)
                    {
                        Debug.unityLogger.LogError("BuildPiepeline", $"_hybridBuilderSettings.AssetPackages ==Null or empty");
                        return;
                    }
                    break;
            }

            StartBuild();
        }


        bool BuildApplication()
        {
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            switch (activeBuildTarget)
            {
                case BuildTarget.Android:
                    return BuildHelper.BuildAPK(_hybridBuilderSettings.GetBuildOutputPath(),
                        _hybridBuilderSettings.GetCurrentVersion(true));
                    break;
            }

            return false;
        }

        /// <summary>
        /// 确认是否存在AOT补充Dll路径和HotUpdatePath路径
        /// </summary>
        bool CheckScriptPathExsist()
        {
            if (string.IsNullOrEmpty(_hybridBuilderSettings.ScriptPackageName))
            {
                Debug.unityLogger.LogError("CheckScriptPathExsist", $"ScriptPackageName == Null ");
                return false;
            }

            bool hasPatchedAOTDLLPath = false;
            bool hasHotUpdateDllPath = false;

            var patchedAOTDLLPathGUID = AssetDatabase.AssetPathToGUID(_hybridBuilderSettings.PatchedAOTDLLCollectPath);
            var hotUpdateDLLPathGUID = AssetDatabase.AssetPathToGUID(_hybridBuilderSettings.HotUpdateDLLCollectPath);

            var buildPackage =
                AssetBundleCollectorSettingData.Setting.GetPackage(_hybridBuilderSettings.ScriptPackageName);
            foreach (var group in buildPackage.Groups)
            {
                foreach (var collector in group.Collectors)
                {
                    Debug.unityLogger.Log($"Iterating CollectorPath ==> {collector.CollectorGUID}");
                    if (string.Equals(patchedAOTDLLPathGUID, collector.CollectorGUID))
                    {
                        hasPatchedAOTDLLPath = true;
                        Debug.unityLogger.Log($"hasPatchedAOTDLLPath == CollectorGUID ==> {patchedAOTDLLPathGUID}");
                    }
                    else if (string.Equals(hotUpdateDLLPathGUID, collector.CollectorGUID))
                    {
                        hasHotUpdateDllPath = true;
                        Debug.unityLogger.Log($"hasPatchedAOTDLLPath == CollectorGUID ==> {collector.CollectorGUID}");
                    }
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
            if (!RuntimePackages.ContainsKey(packageName))
            {
                RuntimePackages.Add(
                    packageName,
                    version.ToString());
            }
            else
            {
                RuntimePackages[packageName] =
                    version.ToString();
            }
        }
        void BuildFinish()
        {
            if (string.IsNullOrEmpty(_hybridBuilderSettings.RuntimeSettings.Packages))
            {
                RuntimePackages = new Dictionary<string, string>();
            }
            else
            {
                RuntimePackages = JsonConvert.DeserializeObject<Dictionary<string,string>>(_hybridBuilderSettings.RuntimeSettings.Packages);

            }
            switch (_hybridBuilderSettings.hybridBuildOption)
            {
                case HybridBuildOption.BuildAsset:
                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        UpdatePackageVersion(assetPackage, _hybridBuilderSettings.AssetBuildVersion);
                    }

                    _hybridBuilderSettings.AssetBuildVersion++;
                    break;
                case HybridBuildOption.BuildScript:

                    UpdatePackageVersion(_hybridBuilderSettings.ScriptPackageName,_hybridBuilderSettings.ScriptBuildVersion);

                    _hybridBuilderSettings.ScriptBuildVersion++;
                    break;
                case HybridBuildOption.BuildAll:
                    UpdatePackageVersion(_hybridBuilderSettings.ScriptPackageName,_hybridBuilderSettings.ScriptBuildVersion);
                    _hybridBuilderSettings.ScriptBuildVersion++;
                    
                    foreach (var assetPackage in _hybridBuilderSettings.AssetPackages)
                    {
                        UpdatePackageVersion(assetPackage, _hybridBuilderSettings.AssetBuildVersion);
                    }

                    _hybridBuilderSettings.AssetBuildVersion++;
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
                    break;
            }

            var json =JsonConvert.SerializeObject(RuntimePackages);
            _hybridBuilderSettings.RuntimeSettings.Packages = json;
            
            json = JsonConvert.SerializeObject(_hybridBuilderSettings.RuntimeSettings);
            File.WriteAllText(Path.Combine(_hybridBuilderSettings.ResolveBuildOutputPath(), "RuntimeSettings.json"), json);

            EditorUtility.SetDirty(_hybridBuilderSettings.RuntimeSettings);
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

            HybrdiScriptableBuildPipeline pipeline = new HybrdiScriptableBuildPipeline();
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
            buildParameters.CompressOption = _hybridBuilderSettings.assetCompressOption;
            buildParameters.ClearBuildCacheFiles = _hybridBuilderSettings.isClearBuildCache;
            buildParameters.UseAssetDependencyDB = _hybridBuilderSettings.isUseAssetDependDB;
            buildParameters.EncryptionServices = CreateEncryptionInstance();

            ScriptableBuildPipeline pipeline = new ScriptableBuildPipeline();
            var buildResult = pipeline.Run(buildParameters, true);
            return buildResult.Success;
        }


        /// <summary>
        /// 内置着色器资源包名称
        /// 注意：和自动收集的着色器资源包名保持一致！
        /// </summary>
        private string GetBuiltinShaderBundleName(string packageName)
        {
            var uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            return packRuleResult.GetBundleName(packageName, uniqueBundleName); //todo
        }
    }
}
#endif
