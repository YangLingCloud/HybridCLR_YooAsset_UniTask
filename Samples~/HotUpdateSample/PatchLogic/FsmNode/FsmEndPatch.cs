using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;

/// <summary>
/// 补丁状态机结束节点，标记当前资源包补丁流程成功完成。
/// </summary>
internal class FsmEndPatch : IStateNode
{
    private PatchOperation _owner;
    private StateMachine _machine;

    /// <summary>
    /// 创建节点时保存状态机引用并获取补丁操作拥有者。
    /// </summary>
    async UniTaskVoid IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
        _owner = _machine.Owner as PatchOperation;
    }

    /// <summary>
    /// 进入结束节点时标记补丁操作成功完成。
    /// </summary>
    async UniTaskVoid IStateNode.OnEnter()
    {
        var packageName = (string)_machine.GetBlackboardValue("PackageName");
        
        Debug.unityLogger.Log($"{packageName} is patch completed");
        //PatchEventDefine.PatchStepsChange.SendEventMessage("开始游戏！");
        _owner.SetFinish();
    }

    /// <summary>
    /// 结束节点没有逐帧逻辑。
    /// </summary>
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }

    /// <summary>
    /// 退出结束节点时无需清理。
    /// </summary>
    async UniTaskVoid IStateNode.OnExit()
    {
    }
}
