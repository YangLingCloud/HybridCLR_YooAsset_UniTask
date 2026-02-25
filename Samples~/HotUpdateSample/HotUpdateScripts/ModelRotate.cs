using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class ModelRotate : MonoBehaviour
{
    // Start is called before the first frame update
    async UniTask Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up);
    }
}
