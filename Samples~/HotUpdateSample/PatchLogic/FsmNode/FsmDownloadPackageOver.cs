using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;

/// <summary>
/// 补丁状态机下载完成节点，负责将流程推进到缓存清理阶段。
/// </summary>
internal class FsmDownloadPackageOver : IStateNode
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
    /// 进入节点时提示下载完成并切换到缓存清理节点。
    /// </summary>
    async UniTaskVoid IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("资源文件下载完毕！");
        _machine.ChangeState<FsmClearCacheBundle>();
    }

    /// <summary>
    /// 下载完成节点没有逐帧逻辑。
    /// </summary>
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }

    /// <summary>
    /// 退出下载完成节点时无需清理。
    /// </summary>
    async UniTaskVoid IStateNode.OnExit()
    {
    }
}
