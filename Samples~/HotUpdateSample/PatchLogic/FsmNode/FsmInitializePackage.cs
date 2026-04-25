using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Machine;
using YangLing.Hybrid.Runtime;
using YooAsset;

/// <summary>
/// 补丁状态机初始化节点，按运行模式创建并初始化 YooAsset 资源包。
/// </summary>
internal class FsmInitializePackage : IStateNode
{
    private StateMachine _machine;

    private HybridRuntimeSettings _runtimeSettings;

    private string _packageName;

    /// <summary>
    /// 创建节点时保存状态机引用，后续通过黑板读取补丁上下文。
    /// </summary>
    async UniTaskVoid IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }

    /// <summary>
    /// 进入初始化节点时读取运行时配置并开始初始化资源包。
    /// </summary>
    async UniTaskVoid IStateNode.OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("初始化资源包！");
        
        _runtimeSettings=  (HybridRuntimeSettings)_machine.GetBlackboardValue("HybridRuntimeSettings");
        await InitPackage();
    }

    /// <summary>
    /// 初始化节点没有逐帧逻辑，异步流程在 OnEnter 中完成。
    /// </summary>
    async UniTaskVoid IStateNode.OnUpdate()
    {
    }

    /// <summary>
    /// 退出初始化节点时无需额外清理。
    /// </summary>
    async UniTaskVoid IStateNode.OnExit()
    {
    }

    /// <summary>
    /// 按当前运行模式创建 YooAsset 初始化参数并初始化资源包。
    /// </summary>
    async UniTask InitPackage()
    {
        var playMode = (EPlayMode)_machine.GetBlackboardValue("PlayMode");
        _packageName = (string)_machine.GetBlackboardValue("PackageName");

        // 创建资源包裹类
        var package = YooAssets.TryGetPackage(_packageName);
        if (package == null)
            package = YooAssets.CreatePackage(_packageName);

        // 编辑器下的模拟模式
        InitializationOperation initializationOperation = null;
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            // 编辑器模拟模式直接基于 Collector 配置生成模拟构建结果，不访问远端服务器。
            var buildResult = EditorSimulateModeHelper.SimulateBuild(_packageName);
            var packageRoot = buildResult.PackageRootDirectory;
            var createParameters = new EditorSimulateModeParameters();
            createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            initializationOperation = package.InitializeAsync(createParameters);
        }

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            // 单机模式只使用随包内置资源文件系统。
            var createParameters = new OfflinePlayModeParameters();
            createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            initializationOperation = package.InitializeAsync(createParameters);
        }

        // 联机运行模式
        if (playMode == EPlayMode.HostPlayMode)
        {
            // 联机模式使用远端服务和缓存文件系统，适合 CDN 热更新场景。
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var createParameters = new HostPlayModeParameters();
            createParameters.BuildinFileSystemParameters = null;
            createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            initializationOperation = package.InitializeAsync(createParameters);
        }

        // WebGL运行模式
        if (playMode == EPlayMode.WebPlayMode)
        {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
            var createParameters = new WebPlayModeParameters();
			string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE"; //注意：如果有子目录，请修改此处！
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            createParameters.WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices);
            initializationOperation = package.InitializeAsync(createParameters);
#else
            // 普通 WebGL 示例使用 YooAsset 默认 WebServer 文件系统参数。
            var createParameters = new WebPlayModeParameters();
            createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            initializationOperation = package.InitializeAsync(createParameters);
#endif
        }

        await initializationOperation;

        // 如果初始化失败弹出提示界面
        if (initializationOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogWarning($"{initializationOperation.Error}");
            PatchEventDefine.InitializeFailed.SendEventMessage();
        }
        else
        {
            // 初始化成功后进入远端版本请求节点。
            _machine.ChangeState<FsmRequestPackageVersion>();
        }
    }

    /// <summary>
    /// 获取资源服务器地址
    /// </summary>
    private string GetHostServerURL()
    {
        //string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
        string hostServerIP = _runtimeSettings.HostServerIP;
        string appVersion =_runtimeSettings.ReleaseBuildVersion.ToString();
        var packageVersion=(string)_machine.GetBlackboardValue("Version");
        

#if UNITY_EDITOR
        // 编辑器下用当前激活构建目标拼接远端目录，便于在同一工程中测试不同平台资源包。
        var activeBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        var hostPath=Path.Combine(hostServerIP,"Bundles", appVersion, activeBuildTarget.ToString(),_packageName,packageVersion);
        Debug.unityLogger.Log(hostPath);
        return hostPath;
#else
        // 真机运行时使用 Application.platform 作为平台目录。
        var activeBuildTarget = Application.platform;
         return Path.Combine(hostServerIP,"Bundles", appVersion, activeBuildTarget.ToString(),_packageName,packageVersion.ToString());
#endif
    }

    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        /// <summary>
        /// 创建远端资源服务，分别保存主下载地址和备用下载地址。
        /// </summary>
        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }

        /// <summary>
        /// 返回主远端下载地址。
        /// </summary>
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }

        /// <summary>
        /// 返回备用远端下载地址。
        /// </summary>
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }
}
