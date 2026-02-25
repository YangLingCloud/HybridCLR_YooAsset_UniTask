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
using YooAsset;

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

    void Awake()
    {
        Debug.Log($"资源系统运行模式：{PlayMode}");
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        DontDestroyOnLoad(this.gameObject);
    }

    async UniTask Start()
    {
        if (PlayMode == EPlayMode.HostPlayMode)
        {
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

        var packages = JsonConvert.DeserializeObject<Dictionary<string, string>>(RuntimeSettings.Packages);

        foreach (var package in packages)
        {
            // 开始补丁更新流程
            var operation = new PatchOperation(package.Key,package.Value, PlayMode, RuntimeSettings);
            YooAssets.StartOperation(operation);
            await operation;
            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.unityLogger.LogError("ScriptPackage", "InitializeStatus is Falied");
                return;
            }
        }
        var scriptPackage = YooAssets.GetPackage("SampleScript");
        
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


    public async UniTask LoadHybridRuntimeSettings()
    {
        if (string.IsNullOrEmpty(RuntimeSettingsPath))
        {
            Debug.unityLogger.LogError("LoadHybridRuntimeSettings", "RuntimeSettingsPath == Null");
            return;
        }
        UnityWebRequest request = UnityWebRequest.Get(RuntimeSettingsPath);
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