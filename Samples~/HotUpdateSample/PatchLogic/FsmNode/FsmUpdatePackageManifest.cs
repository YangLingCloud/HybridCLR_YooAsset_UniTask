using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;
using YooAsset;

public class FsmUpdatePackageManifest : IStateNode
{
    private StateMachine _machine;

    async UniTaskVoid IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }
    async UniTaskVoid IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("更新资源清单！");
        await UpdateManifest();
    }
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }
    async UniTaskVoid IStateNode.OnExit()
    {
    }

    async UniTask UpdateManifest()
    {
        var packageName = (string)_machine.GetBlackboardValue("PackageName");
        var packageVersion = (string)_machine.GetBlackboardValue("PackageVersion");
        var package = YooAssets.GetPackage(packageName);
        var operation = package.UpdatePackageManifestAsync(packageVersion);
        await operation;

        if (operation.Status != EOperationStatus.Succeed)
        {
            Debug.LogWarning(operation.Error);
            PatchEventDefine.PackageManifestUpdateFailed.SendEventMessage();
        }
        else
        {
            _machine.ChangeState<FsmCreateDownloader>();
        }
    }
}