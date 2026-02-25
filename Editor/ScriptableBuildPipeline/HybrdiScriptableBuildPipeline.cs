using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset.Editor;

public class HybrdiScriptableBuildPipeline : IBuildPipeline
{
    public BuildResult Run(BuildParameters buildParameters, bool enableLog)
    {
        if (buildParameters is HybridScriptableBuildParameters)
        {
            var hybridBuildParameters = buildParameters as HybridScriptableBuildParameters;

            AssetBundleBuilder builder = new AssetBundleBuilder();
            return builder.Run(buildParameters, GetHybridBuildPipeline(),
                enableLog);
        }
        else
        {
            throw new Exception($"Invalid build parameter type : {buildParameters.GetType().Name}");
        }
    }

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