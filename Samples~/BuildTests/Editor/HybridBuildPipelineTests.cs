using System;
using System.IO;
using System.Linq;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using NUnit.Framework;
using UnityEditor;
using YangLing.Hybrid.Editor;
using YangLing.Hybrid.Editor.ScriptableBuildPipeline;
using YangLing.Hybrid.Runtime;
using YooAsset.Editor;

namespace HybridCLR.Tests.Editor
{
    /// <summary>
    /// HybridCLR + YooAsset 构建链路测试。
    /// 仅覆盖本包自身的公开契约（版本字符串、路径解析、拷贝防御、流水线类型校验、端到端首次构建）。
    /// 上游 API（HybridCLR SettingsUtil、YooAsset）的行为由上游仓库保证，本包不重复测试。
    /// </summary>
    public class HybridBuildPipelineTests
    {
        #region PathResolution

        /// <summary>
        /// 覆盖 ResolveBuildOutputPath 相对/绝对路径解析，以及 GetBuildOutputPath 追加版本号子目录的行为。
        /// 这是本包的核心路径契约（BuildHelper.BuildAPK、RuntimeSettings.json 落盘均依赖）。
        /// </summary>
        [Test]
        public void PathResolution_BuildOutputPathIsResolvedAndVersioned()
        {
            var settings = RequireBuilderSettings();
            var oldPath = settings.buildOutputPath;
            var oldRelease = settings.ReleaseBuildVersion;
            var absolutePath = Path.GetFullPath(
                Path.Combine(Path.GetTempPath(), "HybridBuildPipelineTests_Absolute"));
            try
            {
                // 相对路径 → 基于工程根解析
                settings.buildOutputPath = "Bundles";
                var expectedRoot = Path.GetFullPath(Path.Combine(BuildHelper.ProjectPath, "Bundles"));
                Assert.AreEqual(expectedRoot, settings.ResolveBuildOutputPath(),
                    "相对路径应基于工程根目录解析");

                // 附加 release 版本号
                settings.ReleaseBuildVersion = 42;
                Assert.AreEqual(Path.Combine(expectedRoot, "42"), settings.GetBuildOutputPath(),
                    "GetBuildOutputPath 应在根目录后附加 release 版本号");

                // 绝对路径原样透传
                settings.buildOutputPath = absolutePath;
                Assert.AreEqual(absolutePath, settings.ResolveBuildOutputPath(),
                    "绝对路径应原样透传");
            }
            finally
            {
                settings.buildOutputPath = oldPath;
                settings.ReleaseBuildVersion = oldRelease;
            }
        }

        #endregion

        #region VersionString

        /// <summary>
        /// 验证构建用版本字符串格式为 release_asset_script，三段皆为整数。
        /// 此字符串会成为构建输出目录名，格式错误会导致路径解析失败。
        /// </summary>
        [Test]
        public void VersionString_BuildFormatIsThreeIntegersJoinedByUnderscore()
        {
            var settings = RequireBuilderSettings();
            var oldRelease = settings.ReleaseBuildVersion;
            var oldAsset = settings.AssetBuildVersion;
            var oldScript = settings.ScriptBuildVersion;
            try
            {
                settings.ReleaseBuildVersion = 1;
                settings.AssetBuildVersion = 2;
                settings.ScriptBuildVersion = 3;
                Assert.AreEqual("1_2_3", settings.GetCurrentVersion(true));

                settings.AssetBuildVersion = 4;
                Assert.AreEqual("1_4_3", settings.GetCurrentVersion(true),
                    "任一版本字段变化应立即反映到版本字符串");

                var parts = settings.GetCurrentVersion(true).Split('_');
                Assert.AreEqual(3, parts.Length);
                Assert.IsTrue(parts.All(p => int.TryParse(p, out _)));
            }
            finally
            {
                settings.ReleaseBuildVersion = oldRelease;
                settings.AssetBuildVersion = oldAsset;
                settings.ScriptBuildVersion = oldScript;
            }
        }

        /// <summary>
        /// 验证展示用版本字符串包含 Release/AssetPackage/ScriptPackage 三个标签前缀。
        /// Hybrid Builder 窗口依赖此格式显示当前版本。
        /// </summary>
        [Test]
        public void VersionString_DisplayFormatContainsAllLabels()
        {
            var settings = RequireBuilderSettings();
            var display = settings.GetCurrentVersion(false);
            StringAssert.Contains("Release:", display);
            StringAssert.Contains("AssetPackage:", display);
            StringAssert.Contains("ScriptPackage:", display);
        }

        #endregion

        #region CopyDllDefensive

        /// <summary>
        /// 验证 CopyHotUpdateDll / CopyPatchedAOTDll 对 null / 空路径的防御。
        /// 构建管线任一步骤抛异常会中断整个流水线，此类防御极其关键。
        /// </summary>
        [Test]
        public void CopyDll_EmptyPathIsHandledGracefully(
            [Values(null, "")] string emptyPath)
        {
            Assert.DoesNotThrow(() => BuildHelper.CopyPatchedAOTDllToCollectPath(emptyPath),
                "CopyPatchedAOTDllToCollectPath 空路径不应抛异常");
            Assert.DoesNotThrow(() => BuildHelper.CopyHotUpdateDllToCollectPath(emptyPath),
                "CopyHotUpdateDllToCollectPath 空路径不应抛异常");
        }

        /// <summary>
        /// 验证 CopyDllFileToByte 源目录不存在时返回空列表，不抛异常。
        /// </summary>
        [Test]
        public void CopyDll_NonExistentSourceDirReturnsEmptyList()
        {
            var result = BuildHelper.CopyDllFileToByte(
                new[] { "FakeAssembly" },
                "/non_existent_dir_12345",
                Path.GetTempPath());
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region PipelineTypeValidation

        /// <summary>
        /// 验证 HybridScriptableBuildPipeline.Run 拒绝非 HybridScriptableBuildParameters 的参数。
        /// 保护 TaskBuildScript_SBP 的向下转型不会收到不兼容类型。
        /// </summary>
        [Test]
        public void Pipeline_RejectsInvalidBuildParameterType()
        {
            var pipeline = new HybridScriptableBuildPipeline();
            Assert.Throws<Exception>(
                () => pipeline.Run(new FakeBuildParameters(), false));
        }

        #endregion

        #region EndToEnd (SlowTest)

        /// <summary>
        /// 端到端：在当前激活平台下执行 PrebuildCommand.GenerateAll，验证全链路可完成
        /// —— 热更新 DLL 编译、AOT 裁剪目录生成、CheckAccessMissingMetadata 通过。
        /// 这是首次构建的关键前置条件，任一环节断裂都会导致 BuildApplication 失败。
        /// 平台并行化由 Unity Editor 启动时的 activeBuildTarget 决定 —— NUnit 参数化在单机上无意义。
        /// </summary>
        [Test]
        [Category("SlowTest")]
        public void EndToEnd_GenerateAllProducesValidBuildArtifacts()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;

            PrebuildCommand.GenerateAll();

            var aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            Assert.IsTrue(Directory.Exists(aotDir),
                $"Generate/All 应创建 AOT 裁剪目录: {aotDir}");

            var hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            Assert.IsTrue(Directory.Exists(hotUpdateDir),
                $"Generate/All 应创建热更新 DLL 输出目录: {hotUpdateDir}");

            foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                Assert.IsTrue(File.Exists(Path.Combine(hotUpdateDir, dll)),
                    $"Generate/All 应编译热更新 DLL: {dll}");
            }

            Assert.IsTrue(BuildHelper.CheckAccessMissingMetadata(),
                "Generate/All 后 CheckAccessMissingMetadata 应通过，否则前置链路不完整");
        }

        /// <summary>
        /// 端到端：在当前激活平台下执行 DLL 拷贝流程，验证 .bytes 与清单文件均正确生成。
        /// </summary>
        [Test]
        [Category("SlowTest")]
        public void EndToEnd_CopyHotUpdateDllProducesBytesAndManifest()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var tempDir = Path.Combine(Path.GetTempPath(), $"HybridBuildPipelineTests_{target}_CopyHotUpdate");
            try
            {
                RecreateDirectory(tempDir);
                CompileDllCommand.CompileDllActiveBuildTarget();
                BuildHelper.CopyHotUpdateDllToCollectPath(tempDir);

                foreach (var assemblyName in SettingsUtil.HotUpdateAssemblyNamesExcludePreserved)
                {
                    Assert.IsTrue(File.Exists(Path.Combine(tempDir, $"{assemblyName}.bytes")),
                        $"缺少 bytes 文件: {assemblyName}.bytes");
                }

                Assert.IsTrue(File.Exists(Path.Combine(tempDir, "HotUpdateDLLs.txt")),
                    "HotUpdateDLLs.txt 清单未生成");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        #endregion

        #region Helpers

        private class FakeBuildParameters : YooAsset.Editor.BuildParameters
        {
        }

        private static HybridBuilderSettings RequireBuilderSettings()
        {
            var guid = AssetDatabase.FindAssets("t:HybridBuilderSettings").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                Assert.Inconclusive("未找到 HybridBuilderSettings 资产，跳过。请先通过 Sample 导入或手动创建。");
                return null;
            }
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var settings = AssetDatabase.LoadAssetAtPath<HybridBuilderSettings>(path);
            if (settings == null)
            {
                Assert.Inconclusive($"HybridBuilderSettings 资产加载失败: {path}");
                return null;
            }
            return settings;
        }

        private static void RecreateDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        #endregion
    }
}
