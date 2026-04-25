using UnityEngine;
using UniFramework.Machine;
using UniFramework.Event;
using YangLing.Hybrid.Runtime;
using YooAsset;

/// <summary>
/// YooAsset 自定义补丁操作，使用状态机串联初始化、版本请求、清单更新、下载和缓存清理流程。
/// </summary>
public class PatchOperation : GameAsyncOperation
{
    /// <summary>
    /// 补丁操作内部执行阶段。
    /// </summary>
    private enum ESteps
    {
        None,
        Update,
        Done,
    }

    private readonly EventGroup _eventGroup = new EventGroup();
    private readonly StateMachine _machine;
    private readonly string _packageName;
    private HybridRuntimeSettings _runtimeSettings;
    private ESteps _steps = ESteps.None;

    /// <summary>
    /// 创建指定资源包的补丁操作，并初始化状态机节点与事件监听。
    /// </summary>
    public PatchOperation(string packageName,string version,EPlayMode playMode,HybridRuntimeSettings hybridRuntimeSettings)
    {
        _packageName = packageName;
        _runtimeSettings=hybridRuntimeSettings;
        // 注册监听事件
        _eventGroup.AddListener<UserEventDefine.UserTryInitialize>(OnHandleEventMessage);
        _eventGroup.AddListener<UserEventDefine.UserBeginDownloadWebFiles>(OnHandleEventMessage);
        _eventGroup.AddListener<UserEventDefine.UserTryRequestPackageVersion>(OnHandleEventMessage);
        _eventGroup.AddListener<UserEventDefine.UserTryUpdatePackageManifest>(OnHandleEventMessage);
        _eventGroup.AddListener<UserEventDefine.UserTryDownloadWebFiles>(OnHandleEventMessage);

        // 创建状态机
        _machine = new StateMachine(this);
        _machine.AddNode<FsmInitializePackage>();
        _machine.AddNode<FsmRequestPackageVersion>();
        _machine.AddNode<FsmUpdatePackageManifest>();
        _machine.AddNode<FsmCreateDownloader>();
        _machine.AddNode<FsmDownloadPackageFiles>();
        _machine.AddNode<FsmDownloadPackageOver>();
        _machine.AddNode<FsmClearCacheBundle>();
        _machine.AddNode<FsmEndPatch>();
        
        // 状态机通过黑板传递包名、版本、运行模式和运行时配置，避免节点之间直接持有彼此引用。
        _machine.SetBlackboardValue("PackageName", packageName);
        _machine.SetBlackboardValue("Version",version);
        _machine.SetBlackboardValue("HybridRuntimeSettings", hybridRuntimeSettings);
        _machine.SetBlackboardValue("PlayMode", playMode);
    }

    /// <summary>
    /// YooAsset 操作启动回调，从初始化节点开始执行补丁流程。
    /// </summary>
    protected override void OnStart()
    {
        _steps = ESteps.Update;
        _machine.Run<FsmInitializePackage>();
    }

    /// <summary>
    /// YooAsset 操作更新回调，驱动补丁状态机逐帧执行。
    /// </summary>
    protected override void OnUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
            return;

        if (_steps == ESteps.Update)
        {
            _machine.Update();
        }
    }

    /// <summary>
    /// YooAsset 操作中止回调，示例暂未实现中止后的清理逻辑。
    /// </summary>
    protected override void OnAbort()
    {
    }

    /// <summary>
    /// 标记补丁操作成功完成，并移除事件监听。
    /// </summary>
    public void SetFinish()
    {
        _steps = ESteps.Done;
        _eventGroup.RemoveAllListener();
        Status = EOperationStatus.Succeed;
        Debug.Log($"Package {_packageName} patch done !");
    }

    /// <summary>
    /// 接收事件
    /// </summary>
    private void OnHandleEventMessage(IEventMessage message)
    {
        if (message is UserEventDefine.UserTryInitialize)
        {
            // 初始化失败后的用户重试入口。
            _machine.ChangeState<FsmInitializePackage>();
        }
        else if (message is UserEventDefine.UserBeginDownloadWebFiles)
        {
            // 用户确认下载后进入实际下载节点。
            _machine.ChangeState<FsmDownloadPackageFiles>();
        }
        else if (message is UserEventDefine.UserTryRequestPackageVersion)
        {
            // 版本请求失败后的用户重试入口。
            _machine.ChangeState<FsmRequestPackageVersion>();
        }
        else if (message is UserEventDefine.UserTryUpdatePackageManifest)
        {
            // 清单更新失败后的用户重试入口。
            _machine.ChangeState<FsmUpdatePackageManifest>();
        }
        else if (message is UserEventDefine.UserTryDownloadWebFiles)
        {
            // 下载失败后重新创建下载器，确保 YooAsset 下载状态重新初始化。
            _machine.ChangeState<FsmCreateDownloader>();
        }
        else
        {
            throw new System.NotImplementedException($"{message.GetType()}");
        }
    }
}
