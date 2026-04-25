using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

/// <summary>
/// 热更新资源加载示例，演示网络图片下载、纹理创建以及 YooAsset 预制体异步加载。
/// </summary>
public class LoadImage : MonoBehaviour
{
    /// <summary>
    /// 启动时下载图片并加载测试预制体。
    /// </summary>
    async UniTask Start()
    {
        // 从场景中查找 RawImage，并把下载到的图片字节写入 Texture2D。
        RawImage image = GameObject.Find("RawImage").GetComponent<RawImage>();
        byte[] data = await HttpHelper.Request("http://192.168.0.146:8888/teestTxtures.PNG");
        Texture2D texture = new Texture2D(50, 50);
        texture.LoadImage(data);
        image.texture = texture;
        image.SetNativeSize();
        // 通过 YooAsset 地址加载并实例化热更新资源包中的测试预制体。
        var handle = YooAssets.LoadAssetAsync<GameObject>("TestCube");
        await handle.ToUniTask();
        if (handle.Status == EOperationStatus.Succeed)
        {
            var obj = handle.InstantiateAsync();
            await obj.ToUniTask();
            if (obj.Result == null)
            {
                Debug.Log("加载预制体为空");
            }
        }
    }

    /// <summary>
    /// 每帧更新入口，当前示例未添加逐帧逻辑。
    /// </summary>
    void Update()
    {
        
    }
}
