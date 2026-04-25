using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;
using YooAsset;

/// <summary>
/// 补丁状态机清单更新节点，根据请求到的包版本更新本地资源清单。
/// </summary>
public class FsmUpdatePackageManifest : IStateNode
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
    /// 进入节点时按远端版本更新资源清单。
    /// </summary>
    async UniTaskVoid IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("更新资源清单！");
        await UpdateManifest();
    }

    /// <summary>
    /// 清单更新节点没有逐帧逻辑。
    /// </summary>
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }

    /// <summary>
    /// 退出清单更新节点时无需清理。
    /// </summary>
    async UniTaskVoid IStateNode.OnExit()
    {
    }

    /// <summary>
    /// 调用 YooAsset 更新本地资源清单，成功后进入下载器创建节点。
    /// </summary>
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
            // 清单更新完成后才能创建下载器，否则无法准确统计差异文件。
            _machine.ChangeState<FsmCreateDownloader>();
        }
    }
}
