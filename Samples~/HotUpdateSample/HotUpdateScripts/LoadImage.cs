using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

public class LoadImage : MonoBehaviour
{
    // Start is called before the first frame update
    async UniTask Start()
    {
        RawImage image = GameObject.Find("RawImage").GetComponent<RawImage>();
        byte[] data = await HttpHelper.Request("http://192.168.0.146:8888/teestTxtures.PNG");
        Texture2D texture = new Texture2D(50, 50);
        texture.LoadImage(data);
        image.texture = texture;
        image.SetNativeSize();
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
