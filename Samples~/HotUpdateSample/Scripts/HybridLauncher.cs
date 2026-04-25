using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using HybridCLR;
using Newtonsoft.Json;
using UnityEngine;
using UniFramework.Event;
using UnityEngine.Networking;
using YangLing.Hybrid.Runtime;
using YooAsset;

/// <summary>
/// 热更新示例启动器，负责初始化事件系统和 YooAsset，执行资源包补丁流程并加载 AOT 元数据与热更新程序集。
/// </summary>
public class HybridLauncher : MonoBehaviour
{
    /// <summary>
    /// 资源系统运行模式
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

    public HybridRuntimeSettings RuntimeSettings;

    /// <summary>
    /// 
    /// </summary>
    public string RuntimeSettingsPath;

    /// <summary>
    /// 初始化 Unity 应用运行参数，并保持启动器跨场景存在。
    /// </summary>
    void Awake()
    {
        Debug.Log($"资源系统运行模式：{PlayMode}");
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// 执行热更新示例启动流程：加载运行时配置、初始化资源系统、更新资源包并加载热更新程序集。
    /// </summary>
    async UniTask Start()
    {
        if (PlayMode == EPlayMode.HostPlayMode)
        {
            // 联机模式需要先从远端读取 RuntimeSettings，获取当前 Release 和各资源包版本。
            try
            {
                await LoadHybridRuntimeSettings();   
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogError("HybridLauncher", $"HybridRuntimeSettings {e}");
                throw;
            }
        }

        if (!RuntimeSettings)
        {
            Debug.unityLogger.LogError("HybridLauncher", "HybridRuntimeSettings is Null");
            return;
        }
        
        // 游戏管理器
        GameManager.Instance.Behaviour = this;

        // 初始化事件系统
        UniEvent.Initalize();

        // 初始化资源系统
        YooAssets.Initialize();

        // 加载更新页面
        var go = Resources.Load<GameObject>("PatchWindow");
        GameObject.Instantiate(go);

        // RuntimeSettings.Packages 是每个 YooAsset 包的名称与版本，补丁流程会逐包执行。
        var packages = RuntimeSettings.Packages;
        if (packages == null || packages.Count == 0)
        {
            // 兼容旧版资产：若已迁移为结构化列表则跳过；否则回退到旧版 JSON 字段
            var legacyJson = RuntimeSettings.PackagesLegacyJson;
            if (!string.IsNullOrEmpty(legacyJson))
            {
                var legacyDict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(legacyJson);
                if (legacyDict != null)
                {
                    foreach (var kv in legacyDict)
                    {
                        RuntimeSettings.SetPackageVersion(kv.Key, kv.Value);
                    }
                    packages = RuntimeSettings.Packages;
                }
            }
        }

        foreach (var package in packages)
        {
            // 开始补丁更新流程
            // PatchOperation 内部使用状态机完成初始化、版本请求、清单更新和资源下载。
            var operation = new PatchOperation(package.Name, package.Version, PlayMode, RuntimeSettings);
            YooAssets.StartOperation(operation);
            await operation;
            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.unityLogger.LogError("ScriptPackage", "InitializeStatus is Falied");
                return;
            }
        }
        var scriptPackage = YooAssets.GetPackage("SampleScript");
        
        // 热更新程序集依赖的 AOT 泛型元数据必须在 Assembly.Load 前加载。
        if (!await LoadMetadataForAOTAssemblies(scriptPackage))
        {
            Debug.unityLogger.LogError("LoadMetadataForAOTAssemblies", "Load Falied");
            return;
        }

        if (!await LoadHotUpdateAssemblies(scriptPackage))
        {
            Debug.unityLogger.LogError("LoadHotUpdateAssemblies", "Load Falied");
        }
        
        // 设置默认的资源包
        var gamePackage = YooAssets.GetPackage("SmapleAsset");
        YooAssets.SetDefaultPackage(gamePackage);

        // 切换到主页面场景
        SceneEventDefine.ChangeToHomeScene.SendEventMessage();
    }


    /// <summary>
    /// 从 RuntimeSettingsPath 指定地址加载远端运行时配置。
    /// </summary>
    public async UniTask LoadHybridRuntimeSettings()
    {
        if (string.IsNullOrEmpty(RuntimeSettingsPath))
        {
            Debug.unityLogger.LogError("LoadHybridRuntimeSettings", "RuntimeSettingsPath == Null");
            return;
        }
        UnityWebRequest request = UnityWebRequest.Get(RuntimeSettingsPath);
        // 示例工程使用较短超时，便于快速暴露本地测试服务器未启动等问题。
        request.timeout = 2;
        request.downloadHandler = new DownloadHandlerBuffer();
        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.unityLogger.LogError("LoadHybridRuntimeSettings", "Load Failed");
            return;
        }

        var data = request.downloadHandler.text;
        if (string.IsNullOrEmpty(data))
        {
            Debug.unityLogger.LogError("LoadHybridRuntimeSettings", "data is Null");
        }
        Debug.unityLogger.Log(data);
        RuntimeSettings = JsonConvert.DeserializeObject<HybridRuntimeSettings>(data);
    }
    
    /// <summary>
    /// 加载补充元数据的AOTDLL
    /// </summary>
    /// <param name="scriptPackage"></param>
    /// <returns></returns>
    public async UniTask<bool> LoadMetadataForAOTAssemblies(ResourcePackage scriptPackage)
    {
        HomologousImageMode mode = HomologousImageMode.SuperSet;

        // AOTDLLs 是构建流程写入的清单文件，内容为需要补充元数据的 AOT 程序集名称列表。
        var handle = scriptPackage.LoadRawFileSync("AOTDLLs");
        await handle;
        if (handle.Status != EOperationStatus.Succeed)
        {
            Debug.unityLogger.LogError("ScriptPackageName", $"AOTDLLs LoadRawFileSync {handle.LastError}");
            return false;
        }

        var data = handle.GetRawFileText();
        if (string.IsNullOrEmpty(data))
        {
            Debug.unityLogger.LogError("ScriptPackageName", "AOTDLLs is null or empty");
            return false;
        }

        var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
        foreach (var name in dllNames)
        {
            // 每个 AOT DLL 以 RawFile 形式加载为字节数组，再交给 HybridCLR 注册补充元数据。
            var dataHandle = scriptPackage.LoadRawFileAsync(name);
            await dataHandle.ToUniTask();
            var dllData = dataHandle.GetRawFileData();
            if (dllData == null || dllData.Length == 0)
            {
                Debug.unityLogger.LogError("ScriptPackageName", $"{name} is null or empty");
                continue;
            }

            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllData, mode);
            Debug.unityLogger.Log($"LoadMetadataForAOTAssembly:{name}. mode:{mode} ret:{err}");
        }

        return true;
    }

    /// <summary>
    /// 加载热更新DLL
    /// </summary>
    /// <param name="scriptPackage"></param>
    /// <returns></returns>
    async UniTask<bool> LoadHotUpdateAssemblies(ResourcePackage scriptPackage)
    {
        // HotUpdateDLLs 是构建流程写入的热更新程序集清单。
        var handle = scriptPackage.LoadRawFileSync("HotUpdateDLLs");
        await handle.ToUniTask();
        var data = handle.GetRawFileText();
        if (string.IsNullOrEmpty(data))
        {
            Debug.unityLogger.LogError("LoadHotUpdateAssemblies", "HotUpdateDLLs is null or empty");
            return false;
        }

        var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
        foreach (var DllName in dllNames)
        {
            // 逐个加载热更新 DLL 字节数据，随后通过 Assembly.Load 注入当前 AppDomain。
            var dataHandle = scriptPackage.LoadRawFileAsync(DllName);
            await dataHandle.ToUniTask();
            if (dataHandle.Status != EOperationStatus.Succeed)
            {
                Debug.unityLogger.LogError("LoadHotUpdateAssemblies", $"资源加载失败 {DllName}");
                return false;
            }

            var dllData = dataHandle.GetRawFileData();
            if (dllData == null || dllData.Length == 0)
            {
                Debug.unityLogger.LogError("LoadHotUpdateAssemblies", $"获取Dll数据失败 {DllName}");
                return false;
            }

            Assembly assembly = Assembly.Load(dllData);

            Debug.unityLogger.Log(assembly.GetTypes());
            Debug.unityLogger.Log($"加载热更新Dll:{DllName}");
        }

        return true;
    }
    
}
