using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;
using YooAsset;

public class FsmDownloadPackageFiles : IStateNode
{
    private StateMachine _machine;

    async UniTaskVoid IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }
    async UniTaskVoid IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("开始下载资源文件！");
        await BeginDownload();
    }
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }
    async UniTaskVoid IStateNode.OnExit()
    {
    }

    async UniTask BeginDownload()
    {
        var downloader = (ResourceDownloaderOperation)_machine.GetBlackboardValue("Downloader");
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