using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;

internal class FsmEndPatch : IStateNode
{
    private PatchOperation _owner;
    private StateMachine _machine;
    async UniTaskVoid IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
        _owner = _machine.Owner as PatchOperation;
    }
    async UniTaskVoid IStateNode.OnEnter()
    {
        var packageName = (string)_machine.GetBlackboardValue("PackageName");
        
        Debug.unityLogger.Log($"{packageName} is patch completed");
        //PatchEventDefine.PatchStepsChange.SendEventMessage("开始游戏！");
        _owner.SetFinish();
    }
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }
    async UniTaskVoid IStateNode.OnExit()
    {
    }
}