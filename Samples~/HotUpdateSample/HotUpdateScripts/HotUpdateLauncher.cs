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

public class HotUpdateLauncher : MonoBehaviour
{
    public Text SampleText;
    public static void Run()
    {
        
    }
    // Start is called before the first frame update
    public async UniTaskVoid Start()
    {
        var gamePackage = YooAssets.GetPackage("SmapleAsset");
        var scriptPackage = YooAssets.GetPackage("SampleScript");
        SampleText.text=$"CurrentAssetVersion:{gamePackage.GetPackageVersion()},CurrentScriptVersion:{scriptPackage.GetPackageVersion()}";
    }

    public void test()
    {
        
    }
    void Update()
    {
    }
    
}
