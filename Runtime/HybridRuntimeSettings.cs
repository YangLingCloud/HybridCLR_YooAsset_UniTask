using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using YooAsset;

[CreateAssetMenu(fileName = "HybridRuntimeSettings", menuName = "Scriptable Objects/HybridRuntimeSettings")]
public class HybridRuntimeSettings : ScriptableObject
{
    
    /// <summary>
    /// 资源服务器地址
    /// </summary>
    public string HostServerIP;
    
    /// <summary>
    /// 发行版本
    /// </summary>
    public int ReleaseBuildVersion;


    /// <summary>
    /// 所有需要加载的包名以及对应的版本
    /// </summary>
    public string Packages;
}