using System;
using System.Collections.Generic;
using UnityEngine;
using YangLing.Hybrid.Editor.BuildPipelineTask;
using YooAsset.Editor;

namespace YangLing.Hybrid.Editor.ScriptableBuildPipeline
{

/// <summary>
/// Hybrid 脚本包构建管线，基于 YooAsset RawFileBuildPipeline 插入热更新 DLL 编译与拷贝任务。
/// </summary>
public class HybridScriptableBuildPipeline : IBuildPipeline
{
    /// <summary>
    /// 执行 Hybrid 脚本包构建流程，仅接受 HybridScriptableBuildParameters 参数。
    /// </summary>
    public BuildResult Run(BuildParameters buildParameters, bool enableLog)
    {
        if (buildParameters is HybridScriptableBuildParameters hybridBuildParameters)
        {
            // AssetBundleBuilder 仍由 YooAsset 提供，只替换任务列表以插入脚本构建步骤。
            AssetBundleBuilder builder = new AssetBundleBuilder();
            return builder.Run(hybridBuildParameters, GetHybridBuildPipeline(), enableLog);
        }

        throw new Exception($"Invalid build parameter type : {buildParameters.GetType().Name}");
    }

    /// <summary>
    /// 获取脚本包构建任务列表，在 RawFile 构建前插入热更新 DLL 编译与拷贝任务。
    /// </summary>
    private List<IBuildTask> GetHybridBuildPipeline()
    {
        List<IBuildTask> pipeline = new List<IBuildTask>();

        //如果需要同时构建资源和代码
        //需要确保代码在资源构建之前就已经在AssetBundle文件夹中
        pipeline.AddRange(new List<IBuildTask>
        {
            new TaskPrepare_RFBP(),
            new TaskBuildScript_SBP(),
            new TaskGetBuildMap_RFBP(),
            new TaskBuilding_RFBP(),
            new TaskEncryption_RFBP(),
            new TaskUpdateBundleInfo_RFBP(),
            new TaskCreateManifest_RFBP(),
            new TaskCreateReport_RFBP(),
            new TaskCreatePackage_RFBP(),
            new TaskCopyBuildinFiles_RFBP(),
            new TaskCreateCatalog_RFBP()
        });

        return pipeline;
    }
}
}

