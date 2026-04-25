using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>
/// 热更新行为示例组件，挂载后持续旋转目标模型以验证热更新脚本执行效果。
/// </summary>
public class ModelRotate : MonoBehaviour
{
    /// <summary>
    /// 组件启动入口，当前示例无需初始化。
    /// </summary>
    async UniTask Start()
    {

    }

    /// <summary>
    /// 每帧绕 Y 轴旋转模型。
    /// </summary>
    void Update()
    {
        transform.Rotate(Vector3.up);
    }
}
