using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System.Threading;
using System;
using YooAsset;
using System.IO;
using System.Reflection;
using HybridCLR;
using Newtonsoft.Json;
using UnityEngine.Networking;

/// <summary>
/// 热更新程序集入口示例，展示热更新代码读取 YooAsset 包版本并更新界面文本的基础流程。
/// </summary>
public class HotUpdateLauncher : MonoBehaviour
{
    public Text SampleText;

    /// <summary>
    /// 热更新入口预留方法，可用于通过反射调用热更新程序集初始化逻辑。
    /// </summary>
    public static void Run()
    {
        
    }

    /// <summary>
    /// 启动后读取脚本包和资源包版本，并展示到 UI 文本上。
    /// </summary>
    public async UniTaskVoid Start()
    {
        // 通过 YooAssets.GetPackage 验证热更新代码可以访问已初始化的资源包。
        var gamePackage = YooAssets.GetPackage("SmapleAsset");
        var scriptPackage = YooAssets.GetPackage("SampleScript");
        SampleText.text=$"CurrentAssetVersion:{gamePackage.GetPackageVersion()},CurrentScriptVersion:{scriptPackage.GetPackageVersion()}";
    }

    /// <summary>
    /// 示例预留测试方法。
    /// </summary>
    public void test()
    {
        
    }

    /// <summary>
    /// 每帧更新入口，当前示例未添加逐帧逻辑。
    /// </summary>
    void Update()
    {
    }
    
}
