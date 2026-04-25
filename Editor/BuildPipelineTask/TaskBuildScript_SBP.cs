using System.IO;
using HybridCLR.Editor.Commands;
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
        BuildHelper.CopyPatchedAOTDllToCollectPath(patchedAOTDllFullPath);

        var hotUpdateDLLFullPath = Path.Combine(projectPath, buildParameters.HotUpdateDLLCollectPath);
        BuildHelper.CopyHotUpdateDllToCollectPath(hotUpdateDLLFullPath);
    }
}
}

