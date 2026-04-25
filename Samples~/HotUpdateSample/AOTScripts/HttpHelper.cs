using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 示例用 HTTP 请求工具，运行在 AOT 程序集中，为热更新逻辑提供基础网络下载能力。
/// </summary>
public class HttpHelper 
{
    public static string HttpHost = "http://192.168.0.107:8888/";

    /// <summary>
    /// 使用 UnityWebRequest 发起 GET 请求并返回响应字节数据。
    /// </summary>
    public static async UniTask<byte[]> Request(string path)
    {
        // 示例直接用外部传入 URL 构建请求，下载结果通过 DownloadHandlerBuffer 缓存在内存。
        UnityWebRequest request = new UnityWebRequest(path);
        DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
        request.downloadHandler = dH;

        // 通过 UniTask 的取消令牌实现请求超时控制，避免网络异常时无限等待。
        var cts = new CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(3));
        try
        {
            Debug.Log("发起请求" + path);
            await request.SendWebRequest().WithCancellation(cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            if (ex.CancellationToken == cts.Token)
            {
                Debug.Log("Timeout!");
            }
        }
        if (request.result != UnityWebRequest.Result.Success)
        {
            // 请求失败时释放 UnityWebRequest 并返回 null，由调用方决定如何兜底。
            request.Dispose();
            return null;
        }
        var data = request.downloadHandler.data;
        request.Dispose();
        return data;
    }
}
