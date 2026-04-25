using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;
using YooAsset;

/// <summary>
/// 补丁状态机版本请求节点，向远端查询指定资源包的最新清单版本。
/// </summary>
internal class FsmRequestPackageVersion : IStateNode
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
    /// 进入节点时开始请求远端资源包版本。
    /// </summary>
    async UniTaskVoid IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("请求资源版本 !");
        await  UpdatePackageVersion();
    }

    /// <summary>
    /// 版本请求节点没有逐帧逻辑。
    /// </summary>
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }

    /// <summary>
    /// 退出版本请求节点时无需清理。
    /// </summary>
    async UniTaskVoid IStateNode.OnExit()
    {
    }

    /// <summary>
    /// 请求 YooAsset 资源包版本，成功后将版本写入黑板并进入清单更新节点。
    /// </summary>
    async UniTask UpdatePackageVersion()
    {
        var packageName = (string)_machine.GetBlackboardValue("PackageName");
        var package = YooAssets.GetPackage(packageName);
        var operation = package.RequestPackageVersionAsync();
        await operation;

        if (operation.Status != EOperationStatus.Succeed)
        {
            Debug.LogWarning(operation.Error);
            PatchEventDefine.PackageVersionRequestFailed.SendEventMessage();
        }
        else
        {
            Debug.Log($"Request package version : {operation.PackageVersion}");
            // PackageVersion 是 YooAsset 远端清单版本，后续 UpdatePackageManifestAsync 依赖该值。
            _machine.SetBlackboardValue("PackageVersion", operation.PackageVersion);
            _machine.ChangeState<FsmUpdatePackageManifest>();
        }
    }
}
