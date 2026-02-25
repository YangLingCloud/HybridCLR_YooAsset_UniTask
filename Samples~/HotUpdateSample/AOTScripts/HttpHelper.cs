using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class HttpHelper 
{
    public static string HttpHost = "http://192.168.0.107:8888/";
    // Start is called before the first frame update
    public static async UniTask<byte[]> Request(string path)
    {
        UnityWebRequest request = new UnityWebRequest(path);
        DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
        request.downloadHandler = dH;
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
            request.Dispose();
            return null;
        }
        var data = request.downloadHandler.data;
        request.Dispose();
        return data;
    }
}
