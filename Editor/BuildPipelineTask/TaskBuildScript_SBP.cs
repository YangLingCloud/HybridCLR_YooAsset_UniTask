using System.IO;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;
using YangLing.Hybrid.Editor.ScriptableBuildPipeline;
using YooAsset.Editor;

namespace YangLing.Hybrid.Editor.BuildPipelineTask
{

/// <summary>
/// YooAsset RawFile 构建管线中的脚本构建任务，负责编译热更新 DLL 并复制 AOT/HotUpdate 字节文件到收集目录。
/// </summary>
public class TaskBuildScript_SBP : IBuildTask
{
    /// <summary>
    /// 执行脚本构建任务，编译热更新程序集并把 AOT/HotUpdate DLL 复制为 RawFile 可收集的 .bytes 文件。
    /// </summary>
    public void Run(BuildContext context)
    {
        var buildParametersContext = context.GetContextObject<BuildParametersContext>();
        var buildParameters = buildParametersContext.Parameters as HybridScriptableBuildParameters;

        // 先编译当前激活平台的热更新 DLL，确保后续拷贝的是最新产物。
        CompileDllCommand.CompileDllActiveBuildTarget();


        var projectPath = Directory.GetParent(Application.dataPath).FullName;
        // 构建配置保存的是 Assets 相对路径，这里转换为磁盘绝对路径供 File.Copy 使用。
        var patchedAOTDllFullPath = Path.Combine(projectPath, buildParameters.PatchedAOTDLLCollectPath);
        var hotUpdateDLLFullPath = Path.Combine(projectPath, buildParameters.HotUpdateDLLCollectPath);

        // 批量资源编辑：合并两次 Copy 内部的 AssetDatabase.Refresh，避免连续触发全工程刷新。
        // 两个 Copy 方法独立调用时仍各自刷新；此处仅在串联场景合并。
        try
        {
            AssetDatabase.StartAssetEditing();
            BuildHelper.CopyPatchedAOTDllToCollectPath(patchedAOTDllFullPath);
            BuildHelper.CopyHotUpdateDllToCollectPath(hotUpdateDLLFullPath);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
    }
}
}
