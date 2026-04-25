using System.IO;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;
using YangLing.Hybrid.Editor.ScriptableBuildPipeline;
using YooAsset.Editor;

namespace YangLing.Hybrid.Editor.BuildPipelineTask
{

public class TaskBuildScript_SBP : IBuildTask
{
    public void Run(BuildContext context)
    {
        var buildParametersContext = context.GetContextObject<BuildParametersContext>();
        var buildParameters = buildParametersContext.Parameters as HybridScriptableBuildParameters;


        CompileDllCommand.CompileDllActiveBuildTarget();


        var projectPath = Directory.GetParent(Application.dataPath).FullName;
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
