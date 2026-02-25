using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

namespace HybridCLR.Tests.Editor
{
    /// <summary>
    /// HybridCLR + YooAsset 构建链路测试。
    /// </summary>
    public class HybridBuildPipelineTests
    {
        #region BuildConfig

        /// <summary>
        /// 验证 HybridCLRSettings 可访问。
        /// </summary>
        [Test]
        public void BuildConfig_HybridCLRSettingsExists()
        {
            Assert.IsNotNull(SettingsUtil.HybridCLRSettings, "HybridCLRSettings 未找到，请确认 HybridCLR 已安装并初始化");
        }

        /// <summary>
        /// 验证热更新程序集列表已配置。
        /// </summary>
        [Test]
        public void BuildConfig_HotUpdateAssemblyListConfigured()
        {
            var assemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            Assert.IsNotNull(assemblies, "热更新程序集列表为 null");
            Assert.IsTrue(assemblies.Count > 0, "热更新程序集列表为空，请在 HybridCLR Settings 中配置");
        }

        /// <summary>
        /// 验证补充元数据的 AOT 程序集列表字段存在。
        /// </summary>
        [Test]
        public void BuildConfig_PatchAOTAssemblyListExists()
        {
            var patchAssemblies = SettingsUtil.HybridCLRSettings.patchAOTAssemblies;
            Assert.IsNotNull(patchAssemblies, "patchAOTAssemblies 字段为 null");
        }

        /// <summary>
        /// 验证 BuildSettings 中至少有一个启用场景。
        /// </summary>
        [Test]
        public void BuildConfig_BuildScenesConfigured()
        {
            var scenes = BuildHelper.GetBuildScenes();
            Assert.IsNotNull(scenes, "BuildHelper.GetBuildScenes 返回 null");
            Assert.IsTrue(scenes.Length > 0, "EditorBuildSettings 中没有启用场景");
        }

        /// <summary>
        /// 验证工程根目录路径有效。
        /// </summary>
        [Test]
        public void BuildConfig_ProjectPathValid()
        {
            Assert.IsTrue(Directory.Exists(BuildHelper.ProjectPath), $"ProjectPath 不存在: {BuildHelper.ProjectPath}");
            Assert.IsTrue(Directory.Exists(Path.Combine(BuildHelper.ProjectPath, "Assets")),
                $"ProjectPath 不是有效 Unity 工程根目录: {BuildHelper.ProjectPath}");
        }

        #endregion

        #region BuilderSettings

        /// <summary>
        /// 验证 HybridBuilderSettings 资产存在。
        /// </summary>
        [Test]
        public void BuilderSettings_AssetExists()
        {
            Assert.IsNotNull(FindBuilderSettings(), "未找到 HybridBuilderSettings 资产");
        }

        /// <summary>
        /// 验证 RuntimeSettings 已关联。
        /// </summary>
        [Test]
        public void BuilderSettings_RuntimeSettingsLinked()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            Assert.IsNotNull(settings.RuntimeSettings, "HybridBuilderSettings.RuntimeSettings 未关联");
        }

        /// <summary>
        /// 验证构建输出路径字段非空。
        /// </summary>
        [Test]
        public void BuilderSettings_BuildOutputPathNotEmpty()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            Assert.IsFalse(string.IsNullOrEmpty(settings.buildOutputPath), "buildOutputPath 为空");
        }

        /// <summary>
        /// 验证相对路径会按工程根目录解析。
        /// </summary>
        [Test]
        public void BuilderSettings_ResolveBuildOutputPathResolvesRelativePath()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            var oldPath = settings.buildOutputPath;
            try
            {
                settings.buildOutputPath = "Bundles";
                var resolved = settings.ResolveBuildOutputPath();
                var expected = Path.GetFullPath(Path.Combine(BuildHelper.ProjectPath, "Bundles"));
                Assert.AreEqual(expected, resolved, "ResolveBuildOutputPath 未正确解析相对路径");
            }
            finally
            {
                settings.buildOutputPath = oldPath;
            }
        }

        /// <summary>
        /// 验证绝对路径会被原样透传。
        /// </summary>
        [Test]
        public void BuilderSettings_ResolveBuildOutputPathPassThroughAbsolutePath()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            var oldPath = settings.buildOutputPath;
            var absolutePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "HybridBuildPipelineTests_AbsoluteOutput"));
            try
            {
                settings.buildOutputPath = absolutePath;
                var resolved = settings.ResolveBuildOutputPath();
                Assert.AreEqual(absolutePath, resolved, "ResolveBuildOutputPath 未透传绝对路径");
            }
            finally
            {
                settings.buildOutputPath = oldPath;
            }
        }

        /// <summary>
        /// 验证 GetBuildOutputPath 会附加 release 版本目录。
        /// </summary>
        [Test]
        public void BuilderSettings_GetBuildOutputPathAppendsReleaseVersion()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            var oldPath = settings.buildOutputPath;
            var oldReleaseVersion = settings.ReleaseBuildVersion;
            try
            {
                settings.buildOutputPath = "Bundles";
                settings.ReleaseBuildVersion = 123;

                var outputPath = settings.GetBuildOutputPath();
                var expectedRoot = Path.GetFullPath(Path.Combine(BuildHelper.ProjectPath, "Bundles"));
                var expected = Path.Combine(expectedRoot, "123");

                Assert.AreEqual(expected, outputPath, "GetBuildOutputPath 未按预期附加 release 版本");
            }
            finally
            {
                settings.ReleaseBuildVersion = oldReleaseVersion;
                settings.buildOutputPath = oldPath;
            }
        }

        /// <summary>
        /// 验证版本字符串格式为 release_asset_script。
        /// </summary>
        [Test]
        public void BuilderSettings_VersionFormatCorrect()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            var version = settings.GetCurrentVersion(true);
            var parts = version.Split('_');

            Assert.AreEqual(3, parts.Length, $"版本格式错误，期望 X_Y_Z，实际: {version}");
            Assert.IsTrue(parts.All(p => int.TryParse(p, out _)), $"版本段必须为整数，实际: {version}");
        }

        #endregion

        #region RuntimeSettings

        /// <summary>
        /// 验证 HybridRuntimeSettings 资产存在。
        /// </summary>
        [Test]
        public void RuntimeSettings_AssetExists()
        {
            Assert.IsNotNull(FindRuntimeSettings(), "未找到 HybridRuntimeSettings 资产");
        }

        /// <summary>
        /// 验证 HostServerIP 已配置。
        /// </summary>
        [Test]
        public void RuntimeSettings_HostServerIPConfigured()
        {
            var runtimeSettings = FindRuntimeSettings();
            if (runtimeSettings == null)
            {
                Assert.Inconclusive("未找到 HybridRuntimeSettings 资产，跳过");
                return;
            }

            Assert.IsFalse(string.IsNullOrEmpty(runtimeSettings.HostServerIP), "HostServerIP 为空，请在 RuntimeSettings 中配置");
        }

        #endregion

        #region Platform Tests (Parameterized)

        /// <summary>
        /// 支持的目标平台列表，用于参数化测试。
        /// 新增平台时只需在此数组中添加即可自动生成对应测试用例。
        /// </summary>
        private static readonly BuildTarget[] SupportedPlatforms =
        {
            BuildTarget.StandaloneWindows64,
            BuildTarget.Android,
            BuildTarget.iOS,
        };

        /// <summary>
        /// 检查当前激活平台是否与目标平台一致，不一致则跳过测试。
        /// </summary>
        private static void RequireActivePlatform(BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Assert.Inconclusive(
                    $"Active platform is {EditorUserBuildSettings.activeBuildTarget}, " +
                    $"skipping test for {target}");
            }
        }

        /// <summary>
        /// 验证各平台热更新 DLL 输出目录路径可获取且非空。
        /// </summary>
        [Test]
        public void Platform_DllOutputDirValid([ValueSource(nameof(SupportedPlatforms))] BuildTarget target)
        {
            var outputDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            Assert.IsFalse(string.IsNullOrEmpty(outputDir),
                $"{target}: Hot-update DLL output directory is empty");
        }

        /// <summary>
        /// 验证各平台 AOT 裁剪目录路径可获取且非空。
        /// </summary>
        [Test]
        public void Platform_AOTStripDirValid([ValueSource(nameof(SupportedPlatforms))] BuildTarget target)
        {
            var stripDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            Assert.IsFalse(string.IsNullOrEmpty(stripDir),
                $"{target}: AOT strip directory is empty");
        }

        /// <summary>
        /// 验证不同平台的热更新 DLL 输出目录互不相同。
        /// </summary>
        [Test]
        public void Platform_DllOutputDirsDifferAcrossPlatforms()
        {
            var dirs = SupportedPlatforms
                .Select(t => SettingsUtil.GetHotUpdateDllsOutputDirByTarget(t))
                .ToList();
            Assert.AreEqual(dirs.Count, dirs.Distinct().Count(),
                "Hot-update DLL output directories should differ across platforms");
        }

        /// <summary>
        /// 验证不同平台的 AOT 裁剪目录互不相同。
        /// </summary>
        [Test]
        public void Platform_AOTStripDirsDifferAcrossPlatforms()
        {
            var dirs = SupportedPlatforms
                .Select(t => SettingsUtil.GetAssembliesPostIl2CppStripDir(t))
                .ToList();
            Assert.AreEqual(dirs.Count, dirs.Distinct().Count(),
                "AOT strip directories should differ across platforms");
        }

        /// <summary>
        /// 在当前激活平台下编译热更新 DLL，平台不匹配时自动跳过。
        /// </summary>
        [Test]
        [Category("SlowTest")]
        public void Platform_CompileHotUpdateDll([ValueSource(nameof(SupportedPlatforms))] BuildTarget target)
        {
            RequireActivePlatform(target);
            CompileDllCommand.CompileDllActiveBuildTarget();
            var outputDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                var dllPath = Path.Combine(outputDir, dll);
                Assert.IsTrue(File.Exists(dllPath), $"{target}: Hot-update DLL not found: {dllPath}");
            }
        }

        /// <summary>
        /// 在当前激活平台下拷贝热更新 DLL 到收集目录，平台不匹配时自动跳过。
        /// </summary>
        [Test]
        [Category("SlowTest")]
        public void Platform_CopyHotUpdateDllToCollectPath([ValueSource(nameof(SupportedPlatforms))] BuildTarget target)
        {
            RequireActivePlatform(target);
            var tempDir = Path.Combine(Path.GetTempPath(), $"HybridBuildPipelineTests_{target}_HotUpdate");
            try
            {
                RecreateDirectory(tempDir);
                CompileDllCommand.CompileDllActiveBuildTarget();
                BuildHelper.CopyHotUpdateDllToCollectPath(tempDir);
                foreach (var assemblyName in SettingsUtil.HotUpdateAssemblyNamesExcludePreserved)
                {
                    var bytesFile = Path.Combine(tempDir, $"{assemblyName}.bytes");
                    Assert.IsTrue(File.Exists(bytesFile), $"{target}: bytes file not found: {bytesFile}");
                }
                Assert.IsTrue(File.Exists(Path.Combine(tempDir, "HotUpdateDLLs.txt")),
                    $"{target}: HotUpdateDLLs.txt manifest not generated");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// 在当前激活平台下拷贝补充元数据 AOT DLL 到收集目录，平台不匹配时自动跳过。
        /// </summary>
        [Test]
        [Category("SlowTest")]
        public void Platform_CopyPatchedAOTDllToCollectPath([ValueSource(nameof(SupportedPlatforms))] BuildTarget target)
        {
            RequireActivePlatform(target);
            var patchAssemblies = SettingsUtil.HybridCLRSettings.patchAOTAssemblies;
            if (patchAssemblies == null || patchAssemblies.Length == 0)
                Assert.Inconclusive("patchAOTAssemblies is empty, run Build Application first");
            var tempDir = Path.Combine(Path.GetTempPath(), $"HybridBuildPipelineTests_{target}_AOT");
            try
            {
                RecreateDirectory(tempDir);
                BuildHelper.CopyPatchedAOTDllToCollectPath(tempDir);
                Assert.IsTrue(File.Exists(Path.Combine(tempDir, "AOTDLLs.txt")),
                    $"{target}: AOTDLLs.txt manifest not generated");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        #endregion

        #region FirstBuildPrerequisites

        /// <summary>
        /// 验证 AOT 裁剪目录路径为绝对路径且非空。
        /// </summary>
        [Test]
        public void FirstBuild_AOTStripDirPathResolvable()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            Assert.IsFalse(string.IsNullOrEmpty(aotDir), "AOT strip directory path is empty");
            Assert.IsTrue(Path.IsPathRooted(aotDir), $"AOT strip directory should be absolute: {aotDir}");
        }

        /// <summary>
        /// 验证 EnsureAOTStripDirExists 在目录已存在时直接返回 true，不触发弹窗。
        /// </summary>
        [Test]
        public void FirstBuild_EnsureAOTStripDirReturnsTrueWhenExists()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "HybridBuildPipelineTests_AOTStripExists");
            try
            {
                Directory.CreateDirectory(tempDir);
                Assert.IsTrue(BuildHelper.EnsureAOTStripDirExists(tempDir),
                    "EnsureAOTStripDirExists should return true when directory exists");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// 验证执行 PrebuildCommand.GenerateAll 后，AOT 裁剪目录和热更新 DLL 均存在。
        /// 这是首次构建的核心前置条件。
        /// </summary>
        [Test]
        [Category("SlowTest")]
        public void FirstBuild_GenerateAllCreatesRequiredDirectories()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            PrebuildCommand.GenerateAll();
            var aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            Assert.IsTrue(Directory.Exists(aotDir),
                $"Generate/All should create AOT strip directory: {aotDir}");
            var hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            Assert.IsTrue(Directory.Exists(hotUpdateDir),
                $"Generate/All should create hot-update DLL output directory: {hotUpdateDir}");
            foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                var dllPath = Path.Combine(hotUpdateDir, dll);
                Assert.IsTrue(File.Exists(dllPath),
                    $"Generate/All should compile hot-update DLL: {dllPath}");
            }
        }

        /// <summary>
        /// 验证执行 GenerateAll 后 CheckAccessMissingMetadata 能正常通过。
        /// 覆盖“首次构建时元数据检查失败”的场景。
        /// </summary>
        [Test]
        [Category("SlowTest")]
        public void FirstBuild_MetadataCheckPassesAfterGenerateAll()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            if (!Directory.Exists(aotDir))
            {
                PrebuildCommand.GenerateAll();
            }
            var result = BuildHelper.CheckAccessMissingMetadata();
            Assert.IsTrue(result,
                "CheckAccessMissingMetadata should pass after Generate/All. " +
                "If this fails, the Generate/All prerequisite chain may be incomplete.");
        }

        #endregion

        #region VersionLogic

        /// <summary>
        /// 验证版本自增后 GetCurrentVersion 输出正确递增。
        /// </summary>
        [Test]
        public void VersionLogic_IncrementUpdatesCurrentVersion()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            var oldRelease = settings.ReleaseBuildVersion;
            var oldAsset = settings.AssetBuildVersion;
            var oldScript = settings.ScriptBuildVersion;
            try
            {
                settings.ReleaseBuildVersion = 1;
                settings.AssetBuildVersion = 2;
                settings.ScriptBuildVersion = 3;

                Assert.AreEqual("1_2_3", settings.GetCurrentVersion(true));

                settings.AssetBuildVersion++;
                settings.ScriptBuildVersion++;
                Assert.AreEqual("1_3_4", settings.GetCurrentVersion(true));
            }
            finally
            {
                settings.ReleaseBuildVersion = oldRelease;
                settings.AssetBuildVersion = oldAsset;
                settings.ScriptBuildVersion = oldScript;
            }
        }

        /// <summary>
        /// 验证 GetCurrentVersion 的展示格式（isBuild=false）包含标签前缀。
        /// </summary>
        [Test]
        public void VersionLogic_DisplayFormatContainsLabels()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            var display = settings.GetCurrentVersion(false);
            Assert.IsTrue(display.Contains("Realse:"), $"展示格式应包含 'Realse:' 前缀，实际: {display}");
            Assert.IsTrue(display.Contains("AssetPakcage:"), $"展示格式应包含 'AssetPakcage:' 前缀，实际: {display}");
            Assert.IsTrue(display.Contains("ScriptPackge:"), $"展示格式应包含 'ScriptPackge:' 前缀，实际: {display}");
        }

        /// <summary>
        /// 验证 GetBuildOutputPath 在版本递增后输出路径同步变化。
        /// </summary>
        [Test]
        public void VersionLogic_BuildOutputPathReflectsVersionChange()
        {
            var settings = RequireBuilderSettingsOrInconclusive();
            var oldPath = settings.buildOutputPath;
            var oldRelease = settings.ReleaseBuildVersion;
            try
            {
                settings.buildOutputPath = "Bundles";
                settings.ReleaseBuildVersion = 10;
                var path1 = settings.GetBuildOutputPath();

                settings.ReleaseBuildVersion = 11;
                var path2 = settings.GetBuildOutputPath();

                Assert.AreNotEqual(path1, path2, "版本递增后输出路径应不同");
                Assert.IsTrue(path2.EndsWith("11"), $"路径末尾应为新版本号，实际: {path2}");
            }
            finally
            {
                settings.ReleaseBuildVersion = oldRelease;
                settings.buildOutputPath = oldPath;
            }
        }

        #endregion

        #region PipelineTypeValidation

        /// <summary>
        /// 验证 HybrdiScriptableBuildPipeline 传入非法参数类型时抛出异常。
        /// </summary>
        [Test]
        public void Pipeline_RejectsInvalidBuildParameterType()
        {
            var pipeline = new HybrdiScriptableBuildPipeline();
            var fakeBuildParameters = new FakeBuildParameters();

            Assert.Throws<Exception>(() => pipeline.Run(fakeBuildParameters, false),
                "传入非 HybridScriptableBuildParameters 类型时应抛出异常");
        }

        #endregion

        #region CopyDllEdgeCases

        /// <summary>
        /// 验证 CopyPatchedAOTDllToCollectPath 传入空路径时不崩溃。
        /// </summary>
        [Test]
        public void CopyDll_PatchedAOTHandlesNullPath()
        {
            Assert.DoesNotThrow(() => BuildHelper.CopyPatchedAOTDllToCollectPath(null),
                "传入 null 路径不应抛出异常");
            Assert.DoesNotThrow(() => BuildHelper.CopyPatchedAOTDllToCollectPath(string.Empty),
                "传入空字符串路径不应抛出异常");
        }

        /// <summary>
        /// 验证 CopyHotUpdateDllToCollectPath 传入空路径时不崩溃。
        /// </summary>
        [Test]
        public void CopyDll_HotUpdateHandlesNullPath()
        {
            Assert.DoesNotThrow(() => BuildHelper.CopyHotUpdateDllToCollectPath(null),
                "传入 null 路径不应抛出异常");
            Assert.DoesNotThrow(() => BuildHelper.CopyHotUpdateDllToCollectPath(string.Empty),
                "传入空字符串路径不应抛出异常");
        }

        /// <summary>
        /// 验证 CopyDllFileToByte 源目录不存在时返回空列表而非崩溃。
        /// </summary>
        [Test]
        public void CopyDll_CopyDllFileToByteHandlesNonExistentDir()
        {
            var result = BuildHelper.CopyDllFileToByte(
                new[] { "FakeAssembly" },
                "/non_existent_dir_12345",
                Path.GetTempPath());
            Assert.IsNotNull(result, "返回值不应为 null");
            Assert.AreEqual(0, result.Count, "源文件不存在时应返回空列表");
        }

        #endregion

        /// <summary>
        /// 用于验证流水线类型校验的伪参数。
        /// </summary>
        private class FakeBuildParameters : YooAsset.Editor.BuildParameters
        {
        }

        private static HybridBuilderSettings FindBuilderSettings()
        {
            var guid = AssetDatabase.FindAssets("t:HybridBuilderSettings").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<HybridBuilderSettings>(path);
        }

        private static HybridBuilderSettings RequireBuilderSettingsOrInconclusive()
        {
            var settings = FindBuilderSettings();
            if (settings == null)
            {
                Assert.Inconclusive("未找到 HybridBuilderSettings 资产，跳过");
                return null;
            }

            return settings;
        }

        private static HybridRuntimeSettings FindRuntimeSettings()
        {
            var guid = AssetDatabase.FindAssets("t:HybridRuntimeSettings").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<HybridRuntimeSettings>(path);
        }

        private static void RecreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }

        private static string ResolveSnapshotPath()
        {
            var importedGuid = AssetDatabase.FindAssets("HybridCLRSettingsSnapshot t:TextAsset").FirstOrDefault();
            if (!string.IsNullOrEmpty(importedGuid))
            {
                return AssetDatabase.GUIDToAssetPath(importedGuid);
            }

            return Path.Combine("Packages", "com.yanglingyun.hyu", "Samples~", "HotUpdateSample", "Editor", "HybridCLRSettingsSnapshot.json");
        }
    }
}
