using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;
using YooAsset;

/// <summary>
/// 补丁状态机文件下载节点，绑定下载回调并执行 YooAsset 资源文件下载。
/// </summary>
public class FsmDownloadPackageFiles : IStateNode
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
    /// 进入节点时开始下载资源文件。
    /// </summary>
    async UniTaskVoid IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("开始下载资源文件！");
        await BeginDownload();
    }

    /// <summary>
    /// 文件下载节点没有额外逐帧逻辑，进度由 YooAsset 回调推送。
    /// </summary>
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }

    /// <summary>
    /// 退出文件下载节点时无需清理。
    /// </summary>
    async UniTaskVoid IStateNode.OnExit()
    {
    }

    /// <summary>
    /// 绑定下载回调并执行 YooAsset 下载操作。
    /// </summary>
    async UniTask BeginDownload()
    {
        var downloader = (ResourceDownloaderOperation)_machine.GetBlackboardValue("Downloader");
        // 错误和进度回调通过事件系统转发给 PatchWindow。
        downloader.DownloadErrorCallback = PatchEventDefine.WebFileDownloadFailed.SendEventMessage;
        downloader.DownloadUpdateCallback = PatchEventDefine.DownloadUpdate.SendEventMessage;
        downloader.BeginDownload();
        await downloader;

        // 检测下载结果
        if (downloader.Status != EOperationStatus.Succeed)
            return;

        _machine.ChangeState<FsmDownloadPackageOver>();
    }
}
