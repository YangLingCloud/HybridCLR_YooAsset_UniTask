using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;
using YooAsset;

/// <summary>
/// 补丁状态机缓存清理节点，清理未使用的资源包缓存文件。
/// </summary>
internal class FsmClearCacheBundle : IStateNode
{
    private StateMachine _machine;

    /// <summary>
    /// 创建节点时保存状态机引用。
    /// </summary>
    async UniTaskVoid IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }

    /// <summary>
    /// 进入节点时清理当前资源包未使用的缓存文件。
    /// </summary>
    async UniTaskVoid IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("清理未使用的缓存文件！");
        var packageName = (string)_machine.GetBlackboardValue("PackageName");
        var package = YooAssets.GetPackage(packageName);
        // 仅清理未使用 Bundle，保留当前版本仍然需要的缓存文件。
        var operation = package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
        operation.Completed += Operation_Completed;
    }

    /// <summary>
    /// 缓存清理节点没有逐帧逻辑。
    /// </summary>
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }

    /// <summary>
    /// 退出缓存清理节点时无需清理。
    /// </summary>
    async UniTaskVoid IStateNode.OnExit()
    {
    }

    /// <summary>
    /// 缓存清理完成回调，推进到补丁结束节点。
    /// </summary>
    private void Operation_Completed(YooAsset.AsyncOperationBase obj)
    {
        _machine.ChangeState<FsmEndPatch>();
    }
}
