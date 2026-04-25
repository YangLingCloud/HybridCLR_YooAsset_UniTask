using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor;
using System.IO;
using System.Linq;
using System.Xml;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.HotUpdate;
using HybridCLR.Editor.Meta;
using Newtonsoft.Json;
using UnityEditor.Build.Reporting;

namespace YangLing.Hybrid.Editor
{

/// <summary>
/// Hybrid 热更新构建辅助类，集中封装 HybridCLR 前置生成、AOT 元数据检查、DLL 拷贝、APK 构建和 link.xml 补全等编辑器能力。
/// </summary>
public class BuildHelper
{
    /// <summary>
    /// 工程目录路径，Assets上一层
    /// </summary>
    public static string ProjectPath => HybridPaths.GetProjectRoot();

    /// <summary>
    /// 获取当前 Build Settings 中启用的场景路径列表，供应用构建流程传入 BuildPipeline。
    /// </summary>
    public static string[] GetBuildScenes()
    {
        List<string> names = new List<string>();
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
        {
            if (e == null)
                continue;
            if (e.enabled)
                names.Add(e.path);
        }
        
        return names.ToArray();
    }

    /// <summary>
    /// 检查 AOT 裁剪目录是否存在，不存在时弹窗提示用户自动执行 Generate/All
    /// 完整执行：编译热更新 DLL → 生成 IL2CPP 定义 → 生成 link.xml → 生成裁剪 AOT DLL → 生成桥接函数
    /// </summary>
    /// <param name="aotDir">AOT 裁剪 DLL 目录路径</param>
    /// <returns>目录存在或自动生成成功返回 true，否则返回 false</returns>
    public static bool EnsureAOTStripDirExists(string aotDir)
    {
        // AOT 裁剪目录存在时说明 HybridCLR 前置生成链路已完成，可以直接继续后续构建。
        if (Directory.Exists(aotDir))
            return true;

        // 首次构建通常还没有 HybridCLR 生成产物，此处通过弹窗把必要前置链路显式交给用户确认。
        bool autoGenerate = EditorUtility.DisplayDialog(
            "Missing HybridCLR Build Data",
            $"AOT strip directory not found:\n{aotDir}\n\n" +
            "This is required for the first hot-update build.\n" +
            "Run HybridCLR Generate/All automatically?\n" +
            "(Compiles hot-update DLLs, generates link.xml, strips AOT DLLs, etc.)",
            "Generate All", "Cancel");
        if (!autoGenerate)
            return false;
        try
        {
            // GenerateAll 会串起热更新 DLL 编译、link.xml 生成、AOT 裁剪 DLL 生成和桥接函数生成。
            PrebuildCommand.GenerateAll();
        }
        catch (Exception e)
        {
            Debug.unityLogger.LogError("EnsureAOTStripDirExists",
                $"Generate/All failed: {e.Message}");
            return false;
        }
        if (!Directory.Exists(aotDir))
        {
            // 上游命令执行成功但目录仍缺失时，说明当前平台或 HybridCLR 配置仍不完整。
            Debug.unityLogger.LogError("EnsureAOTStripDirExists",
                $"Directory still missing after Generate/All: {aotDir}");
            return false;
        }
        Debug.unityLogger.Log($"[EnsureAOTStripDirExists] Generate/All completed successfully: {aotDir}");
        return true;
    }

    /// <summary>
    /// 一般来说，发布热更新包时，由于中间可能调用过generate/all，SettingsUtil.GetAssembliesPostIl2CppStripDir(target)目录中包含了最新的aot dll，
    /// 肯定无法检查出类型或者函数裁剪的问题。
    /// 需要在构建完主包后，将当时的aot dll保存下来，供后面补充元数据或者裁剪检查。
    ///  换句话说,验证的可靠性是建立在当前热更新数据对比上次构建应用时已被裁切后的AOTDLL进行对比得到的
    /// </summary>
    /// <returns></returns>
    [MenuItem("HybridTool/Check AOT Metadata")]
    public static bool CheckAccessMissingMetadata()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);

        // 检查 AOT 裁剪目录是否存在，不存在则提示自动生成
        if (!EnsureAOTStripDirExists(aotDir))
            return false;

        // 第2个参数hotUpdateAssNames为热更新程序集列表。对于旗舰版本，该列表需要包含DHE程序集，即SettingsUtil.HotUpdateAndDHEAssemblyNamesIncludePreserved。
        var checker = new MissingMetadataChecker(aotDir, SettingsUtil.HotUpdateAssemblyNamesIncludePreserved);

        // 热更新 DLL 输出目录与热更新程序集列表必须来自当前激活平台，否则元数据检查结果不可信。
        string hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        var hotUpdateDlls = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved;
        if (hotUpdateDlls == null || hotUpdateDlls.Count == 0)
        {
            Debug.unityLogger.LogWarning("MetadataCheck",
                "Hot-update assembly list is empty. Configure HybridCLR Settings first");
            return false;
        }

        bool allPassed = true;
        foreach (var dll in hotUpdateDlls)
        {
            // 逐个 DLL 检查，累积错误而不是首个失败即退出，便于一次性暴露所有缺失元数据问题。
            string dllPath = Path.Combine(hotUpdateDir, dll);
            if (!checker.Check(dllPath))
            {
                Debug.unityLogger.LogError("MetadataCheck", $"Metadata check failed for {dll}");
                allPassed = false;
            }
        }

        return allPassed;
    }

    /// <summary>
    /// 菜单调试入口，使用默认 Bundles 输出目录构建 Android APK。
    /// </summary>
    [MenuItem("HybridTool/Build APK")]
    public static void Debug_BuildAPK()
    {
        var sampleOutputPath = Path.Combine(ProjectPath, HybridPaths.DefaultBundleOutputDir);
        BuildAPK(sampleOutputPath, "9999");
    }

    /// <summary>
    /// 打包安卓平台
    /// </summary>
    /// <param name="outputPath">  APK/Project输出路径  </param>
    /// <param name="isExportProject">  是否导出AndroidProject  </param>
    public static bool BuildAPK(string outputPath, string version, bool isExportProject = false)
    {
        //如果是生成代码，则只需要更新AOT和热更新代码即可
        Il2CppDefGeneratorCommand.GenerateIl2CppDef();
        //由于该方法中已经执行了生成热更新dll，因此无需重复执行生成热更新DLL
        LinkGeneratorCommand.GenerateLinkXml();
            
        //补全热更新预制体依赖
        BuildHelper.SupplementPrefabDependent();
        
        // BuildPlayerOptions 显式指定场景、目标平台和输出路径，避免依赖 Unity Editor 当前面板状态。
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = GetBuildScenes();
        EditorUserBuildSettings.exportAsGoogleAndroidProject = isExportProject;
        var buildPath = string.Empty;

        buildPlayerOptions.target = BuildTarget.Android;
        var buildSuffix = isExportProject ? string.Empty : ".apk";
        buildPath = Path.Combine(outputPath,
            $"{PlayerSettings.productName}_{version}_{DateTime.Now:yyyy_M_d_HH_mm_s}{buildSuffix}");
        buildPlayerOptions.options = BuildOptions.None;

        Debug.unityLogger.Log(buildPath);
        buildPlayerOptions.locationPathName = buildPath;
        //执行打包 场景名字，打包路径
        var report=  BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            //获取需要补充元数据的AOTDLL列表
            //这里为什么不执行Generate/AOTGenericReference和Generate/AotDlls
            //因为前者本身就是生成用于参考的文件,和下面这个方法一致
            //为什么不执行Generate/AotDlls,是因为执行AOT本质上就是打一次包并把裁剪后的AOTDLL拷贝到HybridCLRData目录下
            //因此如果要打包,直接在打包后通过TaskBuildScript_SBP将AOTDLL拷贝到Package目录即可
            BuildHelper.GetPatchedAOTAssemblyListToHybridCLRSettings();
                            
            //生成桥接函数
            MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper();
            
            EditorUtility.ClearProgressBar();
            //以上是必须要在打包Application时完成的方法
            return true;
        }
        EditorUtility.ClearProgressBar();
        return false;
    }

    /// <summary>
    /// 获取AOT之前,应先编译热更新代码
    /// 执行之前需要先编译热更新代码 CompileDllCommand.CompileDllActiveBuildTarget()
    ///
    /// 通过对比热更新DLL和AOTDLL,获取需要补充元数据的AOTDLL
    /// 并将结果写入HybridCLRSettings中
    /// </summary>
    public static void GetPatchedAOTAssemblyListToHybridCLRSettings()
    {
        var gs = SettingsUtil.HybridCLRSettings;
        List<string> hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

        // 通过 HybridCLR 的深度引用分析器，从热更新程序集反推需要补充元数据的 AOT 程序集。
        AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(
            MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(EditorUserBuildSettings.activeBuildTarget,
                hotUpdateDllNames), hotUpdateDllNames);
        var analyzer = new Analyzer(new Analyzer.Options
        {
            MaxIterationCount = Math.Min(20, gs.maxGenericReferenceIteration),
            Collector = collector,
        });

        analyzer.Run();

        var moduleSet = new HashSet<dnlib.DotNet.ModuleDef>();
        foreach (var type in analyzer.AotGenericTypes)
        {
            // 泛型类型引用会触发 AOT 元数据需求，按所属模块聚合避免重复写入。
            moduleSet.Add(type.Type.Module);
        }
        foreach (var method in analyzer.AotGenericMethods)
        {
            // 泛型方法引用同样需要补充元数据，最终与类型引用合并为模块集合。
            moduleSet.Add(method.Method.Module);
        }

        List<dnlib.DotNet.ModuleDef> modules = moduleSet.ToList();
        modules.Sort((a, b) => a.Name.CompareTo(b.Name));

        List<string> patchedAOTAssemblies = new List<string>();
        foreach (dnlib.DotNet.ModuleDef module in modules)
        {
            //替换掉程序集的拓展名,以方便后续拷贝AOTDll的时候可以和HotUpdateDll共用相同的拷贝逻辑
            var patchedAOTAssemblyName = module.Name.Replace(".dll", string.Empty);
            Debug.unityLogger.Log("BuildHelper", $"AOT assembly requiring supplemental metadata ========= {patchedAOTAssemblyName}");
            patchedAOTAssemblies.Add(patchedAOTAssemblyName);
        }

        gs.patchAOTAssemblies = patchedAOTAssemblies.ToArray();
        EditorUtility.SetDirty(gs);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 菜单调试入口，编译热更新 DLL 后刷新 HybridCLR Settings 中的补充元数据程序集列表。
    /// </summary>
    [MenuItem("HybridTool/Get Patched AOT Assembly List")]
    public static void Debug_GetPatchedAOTAssemblyList()
    {
        CompileDllCommand.CompileDllActiveBuildTarget();

        GetPatchedAOTAssemblyListToHybridCLRSettings();
    }

    /// <summary>
    /// 将指定 DLL 从源目录复制到目标目录并改写为 .bytes 文件，返回成功复制的程序集名称列表。
    /// </summary>
    public static List<string> CopyDllFileToByte(string[] originFileNames, string originDir, string targetDir)
    {
        List<string> bytesFiles = new List<string>();
        foreach (var originFileName in originFileNames)
        {
            // HybridCLR 输出目录保存的是 .dll，YooAsset RawFile 收集目录需要 .bytes 后缀以避免 Unity 导入为托管程序集。
            var dllFilePath = Path.Combine(ProjectPath, originDir, $"{originFileName}.dll");
            if (!File.Exists(dllFilePath))
            {
                Debug.unityLogger.Log("BuildHelper", $"{dllFilePath} not found");
                continue;
            }

            var targetFileName = $"{originFileName}.bytes";
            var dllRawFilePath = Path.Combine(targetDir, targetFileName);
            // 覆盖复制保证重复构建时可以刷新旧 DLL 内容。
            File.Copy(dllFilePath, dllRawFilePath, true);
            bytesFiles.Add(originFileName);
        }

        return bytesFiles;
    }

    /// <summary>
    /// 将生成裁剪后的AOT dlls拷贝到AssetBundle打包路径下
    /// 依赖于   HybridCLR/Generate/Il2CppDef
    /// HybridCLR/Generate/LinkXmlH
    /// ybridCLR/Generate/AotDlls  三条指令生成数据
    /// </summary>
    /// <param name="rawFileCollectPath"></param>
    public static void CopyPatchedAOTDllToCollectPath(string rawFileCollectPath)
    {
        if (string.IsNullOrEmpty(rawFileCollectPath))
        {
            Debug.unityLogger.LogError("CopyPatchedAOTDllToCollectPath", $"{nameof(rawFileCollectPath)}===>Null");
            return;
        }

        var patchedAOTAssemblies = SettingsUtil.HybridCLRSettings.patchAOTAssemblies;

        // 补充元数据 DLL 必须来自当前平台 IL2CPP 裁剪后的 AOT 输出目录。
        var dllOutputPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);

        // 检查 AOT 裁剪目录是否存在，不存在则提示自动生成
        if (!EnsureAOTStripDirExists(dllOutputPath))
            return;

        var dllRawFileAssetNames = CopyDllFileToByte(patchedAOTAssemblies, dllOutputPath, rawFileCollectPath);

        if (dllRawFileAssetNames != null && dllRawFileAssetNames.Count > 0)
        {
            // 清单记录不带扩展名的程序集名，运行时按 RawFile 地址加载对应 .bytes 数据。
            var namesJson = JsonConvert.SerializeObject(dllRawFileAssetNames);
            File.WriteAllText(Path.Combine(rawFileCollectPath, HybridPaths.AotDllManifest), namesJson);
            AssetDatabase.Refresh();
            Debug.unityLogger.Log("CopyPatchedAOTDllToCollectPath Success!");
        }
        else
        {
            Debug.unityLogger.LogError("CopyPatchedAOTDllToCollectPath", $"{nameof(dllRawFileAssetNames)}===>Null");
        }
    }

    /// <summary>
    /// 菜单调试入口，生成当前平台 AOT 裁剪 DLL 并复制到样例资源收集目录。
    /// </summary>
    [MenuItem("HybridTool/Generate AOT DLLs and Copy")]
    public static void Debug_GenerateAOTDllListFile()
    {
        //先生成AOT文件
        Il2CppDefGeneratorCommand.GenerateIl2CppDef();
        LinkGeneratorCommand.GenerateLinkXml();
        StripAOTDllCommand.GenerateStripedAOTDlls();

        var aotDllRawFileCollectPath = Path.Combine(Application.dataPath,
            "HotUpdateAssets", HybridPaths.PatchedAOTDllFolder);

        Debug.unityLogger.Log(aotDllRawFileCollectPath);
        CopyPatchedAOTDllToCollectPath(aotDllRawFileCollectPath);
    }

    /// <summary>
    /// 菜单调试入口，编译当前平台热更新 DLL 并复制到样例资源收集目录。
    /// </summary>
    [MenuItem("HybridTool/Generate Hot-Update DLLs and Copy")]
    public static void Debug_GenerateHotUpdateDllListFile()
    {
        CompileDllCommand.CompileDllActiveBuildTarget();

        var hotUpdateDllRawFileCollectPath = Path.Combine(Application.dataPath,
            "HotUpdateAssets", HybridPaths.HotUpdateDllFolder);

        Debug.unityLogger.Log(hotUpdateDllRawFileCollectPath);
        CopyHotUpdateDllToCollectPath(hotUpdateDllRawFileCollectPath);
    }

    /// <summary>
    /// 将生成裁剪后的HotUpdate dlls拷贝到AssetBundle打包路径下
    /// 依赖于   CompileDllCommand.CompileDllActiveBuildTarget()  生成数据
    /// </summary>
    /// <param name="rawFileCollectPath"></param>
    public static void CopyHotUpdateDllToCollectPath(string rawFileCollectPath)
    {
        if (string.IsNullOrEmpty(rawFileCollectPath))
        {
            Debug.unityLogger.LogError("CopyHotUpdateDllToCollectPath", $"{rawFileCollectPath}===>Null");
            return;
        }

        var hotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

        // 热更新 DLL 输出目录由 HybridCLR 按当前平台生成，不能跨平台复用。
        var hotUpdateOutputPath =
            SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget);

        var dllRawFileAssetNames =
            CopyDllFileToByte(hotUpdateAssemblies.ToArray(), hotUpdateOutputPath, rawFileCollectPath);

        if (dllRawFileAssetNames != null && dllRawFileAssetNames.Count > 0)
        {
            // 运行时先读取清单，再按清单顺序加载每个热更新程序集。
            var json = JsonConvert.SerializeObject(dllRawFileAssetNames);
            File.WriteAllText(Path.Combine(rawFileCollectPath, HybridPaths.HotUpdateDllManifest), json);
            AssetDatabase.Refresh();
            Debug.unityLogger.Log("CopyHotUpdateDllToCollectPath  Success");
        }
        else
        {
            Debug.unityLogger.LogError("CopyHotUpdateDllToCollectPath", $"{nameof(dllRawFileAssetNames)}===>Null");
        }
    }


    //[UnityEditor.UnityEditor.MenuItem("整合工具/删除本地沙盒文件夹")]
    /// <summary>
    /// 删除项目根目录下的 YooAsset 沙盒目录，用于调试本地缓存清理流程。
    /// </summary>
    public static void DeleteSandBoxDirectory()
    {
        var path = $"{ProjectPath}/SandBox";
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        Debug.unityLogger.Log("BuildHelper", "Sandbox directory deleted successfully");
    }

    /// <summary>
    /// 菜单调试入口，扫描热更新预制体依赖并补充 Assets/link.xml。
    /// </summary>
    [MenuItem("HybridTool/Supplement Prefab Dependencies")]
    public static void Debug_SupplementPrefabDependent()
    {
        EditorUtility.DisplayProgressBar("Progress", "Find Class...", 0);
        SupplementPrefabDependent();
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 扫描热更新资源中的预制体组件，将 UnityEngine/TMPro 类型写入 link.xml 防止 IL2CPP 裁剪。
    /// </summary>
    public static void SupplementPrefabDependent()
    {
        try
        {
            SupplementPrefabDependentInternal();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 预制体依赖补全的实际实现，负责收集类型、合并已有 link.xml 并写入最终文件。
    /// </summary>
    private static void SupplementPrefabDependentInternal()
    {
        string[] dirs = ResolveHotUpdateAssetsSearchFolders();
        if (dirs.Length == 0)
        {
            Debug.unityLogger.Log("[SupplementPrefabDependent] No HotUpdateAssets folders found, skipping");
            return;
        }

        var assetGuids = AssetDatabase.FindAssets("t:Prefab", dirs);
        if (assetGuids.Length == 0)
        {
            Debug.unityLogger.Log(
                $"[SupplementPrefabDependent] No prefabs found in HotUpdateAssets folders: {string.Join(", ", dirs)}");
            return;
        }
        // 第一阶段：扫描预制体，收集需要保留的类型（按程序集分组）
        var discoveredTypes = new Dictionary<string, HashSet<string>>();
        for (int i = 0; i < assetGuids.Length; i++)
        {
            // 只读取 GameObject 根对象，再通过 GetComponentsInChildren 覆盖嵌套节点和隐藏节点。
            string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;
            EditorUtility.DisplayProgressBar("Scanning Prefabs", prefab.name, (i + 1) / (float) assetGuids.Length);
            var components = prefab.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                // 防御：Missing Script 会导致 component 为 null
                if (component == null)
                    continue;
                // 组件类型及其公开属性中的 UnityEngine/TMPro 类型都可能在反射或序列化路径中被访问。
                CollectPreserveType(component.GetType(), discoveredTypes);
            }
        }
        if (discoveredTypes.Count == 0)
        {
            Debug.unityLogger.Log("[SupplementPrefabDependent] No preservable types found in prefabs");
            return;
        }
        // 第二阶段：加载或创建 link.xml
        EditorUtility.DisplayProgressBar("Processing", "Loading link.xml...", 0);
        string generatedLinkPath = Path.Combine(Application.dataPath,
            "HybridCLRData", "Generated", "link.xml");
        string outputLinkPath = Path.Combine(Application.dataPath, HybridPaths.AssetsLinkXmlRelative);
        XmlDocument xml = LoadOrCreateLinkXml(generatedLinkPath);
        XmlNode linker = xml.DocumentElement;
        if (linker == null)
        {
            Debug.unityLogger.LogError("SupplementPrefabDependent", "Failed to parse link.xml root element");
            return;
        }
        // 第三阶段：解析已有的 link.xml 条目，建立索引
        var existingTypes = new Dictionary<string, HashSet<string>>();
        foreach (XmlNode child in linker.ChildNodes)
        {
            if (child is XmlElement assemblyElement)
            {
                var assemblyFullName = assemblyElement.GetAttribute("fullname");
                if (string.IsNullOrEmpty(assemblyFullName))
                    continue;
                if (!existingTypes.ContainsKey(assemblyFullName))
                    existingTypes[assemblyFullName] = new HashSet<string>();
                foreach (XmlNode typeChild in assemblyElement.ChildNodes)
                {
                    if (typeChild is XmlElement typeElement)
                    {
                        var typeFullName = typeElement.GetAttribute("fullname");
                        if (!string.IsNullOrEmpty(typeFullName))
                            existingTypes[assemblyFullName].Add(typeFullName);
                    }
                }
            }
        }
        // 第四阶段：将新发现的类型合并写入 link.xml
        int addedCount = 0;
        foreach (var kvp in discoveredTypes)
        {
            var assemblyName = kvp.Key;
            var typeNames = kvp.Value;
            // 查找或创建 assembly 节点
            XmlElement targetAssemblyNode = null;
            if (existingTypes.ContainsKey(assemblyName))
            {
                foreach (XmlNode child in linker.ChildNodes)
                {
                    if (child is XmlElement el && el.GetAttribute("fullname") == assemblyName)
                    {
                        targetAssemblyNode = el;
                        break;
                    }
                }
            }
            if (targetAssemblyNode == null)
            {
                targetAssemblyNode = xml.CreateElement("assembly");
                targetAssemblyNode.SetAttribute("fullname", assemblyName);
                linker.AppendChild(targetAssemblyNode);
                existingTypes[assemblyName] = new HashSet<string>();
            }
            foreach (var typeName in typeNames)
            {
                if (existingTypes[assemblyName].Contains(typeName))
                    continue;
                // 新类型统一 preserve=all，优先保证热更新预制体运行安全。
                var typeNode = xml.CreateElement("type");
                typeNode.SetAttribute("fullname", typeName);
                typeNode.SetAttribute("preserve", "all");
                targetAssemblyNode.AppendChild(typeNode);
                existingTypes[assemblyName].Add(typeName);
                addedCount++;
            }
        }
        xml.Save(outputLinkPath);
        // 仅刷新目标文件，避免全量 Refresh 导致的大范围重导入
        var relativeLinkPath = "Assets/link.xml";
        AssetDatabase.ImportAsset(relativeLinkPath, ImportAssetOptions.ForceUpdate);
        Debug.unityLogger.Log($"[SupplementPrefabDependent] Done. Added {addedCount} type entries to link.xml");
    }

    /// <summary>
    /// 解析所有可能的 HotUpdateAssets 搜索目录，兼容包开发态和 Sample 导入态路径。
    /// </summary>
    private static string[] ResolveHotUpdateAssetsSearchFolders()
    {
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AssetDatabase.IsValidFolder(HybridPaths.HotUpdateAssetsDir))
            dirs.Add(HybridPaths.HotUpdateAssetsDir);

        // Sample 导入后通常位于 Assets/Samples/.../Hot Update Sample/HotUpdateAssets。
        foreach (var guid in AssetDatabase.FindAssets("HotUpdateAssets t:Folder"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path))
                continue;

            var folderName = Path.GetFileName(path.TrimEnd('/', '\\'));
            if (string.Equals(folderName, "HotUpdateAssets", StringComparison.OrdinalIgnoreCase))
                dirs.Add(path);
        }

        return dirs.OrderBy(path => path).ToArray();
    }

    /// <summary>
    /// 递归收集需要在 link.xml 中保留的 UnityEngine / TMPro 类型
    /// 包括组件自身类型及其公开属性中引用的相关类型
    /// </summary>
    private static void CollectPreserveType(Type type, Dictionary<string, HashSet<string>> result)
    {
        if (type == null || string.IsNullOrEmpty(type.FullName))
            return;
        // 只处理 UnityEngine/TMPro 类型，避免把业务脚本或第三方程序集全部写入 link.xml。
        if (!type.FullName.StartsWith("UnityEngine") && !type.FullName.StartsWith("TMPro"))
            return;
        var assemblyName = type.Assembly.GetName().Name;
        if (!result.ContainsKey(assemblyName))
            result[assemblyName] = new HashSet<string>();
        result[assemblyName].Add(type.FullName);
        // 扫描公开属性中引用的 UnityEngine/TMPro 类型（按 Type 缓存结果，避免每次反射）
        foreach (var propInfo in GetCachedPreservableProperties(type))
        {
            if (!result.ContainsKey(propInfo.assembly))
                result[propInfo.assembly] = new HashSet<string>();
            result[propInfo.assembly].Add(propInfo.fullName);
        }
    }

    /// <summary>Type → 其公开属性中需要保留的 (assembly, fullName) 列表缓存</summary>
    private static readonly Dictionary<Type, (string assembly, string fullName)[]> _preservablePropertyCache
        = new Dictionary<Type, (string assembly, string fullName)[]>();

    private static (string assembly, string fullName)[] GetCachedPreservableProperties(Type type)
    {
        if (_preservablePropertyCache.TryGetValue(type, out var cached))
            return cached;

        var list = new List<(string, string)>();
        try
        {
            foreach (var prop in type.GetProperties())
            {
                // 属性类型可能是数组，保留时应记录数组元素类型而不是数组包装类型。
                var propType = prop.PropertyType;
                if (propType == null || string.IsNullOrEmpty(propType.FullName))
                    continue;
                if (propType.IsArray && propType.GetElementType() != null)
                    propType = propType.GetElementType();
                if (propType.FullName == null)
                    continue;
                if (!propType.FullName.StartsWith("UnityEngine") && !propType.FullName.StartsWith("TMPro"))
                    continue;
                list.Add((propType.Assembly.GetName().Name, propType.FullName));
            }
        }
        catch (Exception e)
        {
            // 部分属性反射可能抛出异常（如索引器、泛型约束等），记录警告后跳过
            Debug.unityLogger.LogWarning("PreservableProps",
                $"Reflect properties failed on {type.FullName}: {e.Message}");
        }

        var arr = list.ToArray();
        // 反射结果按组件类型缓存，避免大量预制体扫描时重复分析相同组件类型。
        _preservablePropertyCache[type] = arr;
        return arr;
    }

    /// <summary>
    /// 加载已有的 link.xml，若不存在则创建包含空 linker 根节点的新文档
    /// 优先从 HybridCLRData/Generated/link.xml 加载，不存在时从 Assets/link.xml 加载
    /// 两者都不存在时创建新文档
    /// </summary>
    private static XmlDocument LoadOrCreateLinkXml(string generatedPath)
    {
        string outputPath = Path.Combine(Application.dataPath, "link.xml");
        // 优先加载 HybridCLR 生成的 link.xml
        if (File.Exists(generatedPath))
        {
            try
            {
                var xml = new XmlDocument();
                xml.Load(generatedPath);
                if (xml.DocumentElement != null)
                    return xml;
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogError("LoadOrCreateLinkXml",
                    $"Failed to parse {generatedPath}: {e.Message}");
            }
        }
        // 回退：尝试加载 Assets/link.xml
        if (File.Exists(outputPath))
        {
            try
            {
                // 若 HybridCLR 未生成 link.xml，则复用 Assets/link.xml 中已有的人工保留配置。
                var xml = new XmlDocument();
                xml.Load(outputPath);
                if (xml.DocumentElement != null)
                    return xml;
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogError("LoadOrCreateLinkXml",
                    $"Failed to parse {outputPath}: {e.Message}");
            }
        }
        // 两者都不存在或解析失败，创建新文档
        Debug.unityLogger.Log("[LoadOrCreateLinkXml] No existing link.xml found, creating new one");
        var newXml = new XmlDocument();
        var declaration = newXml.CreateXmlDeclaration("1.0", "utf-8", null);
        newXml.AppendChild(declaration);
        var linker = newXml.CreateElement("linker");
        newXml.AppendChild(linker);
        return newXml;
    }
    
}
}
