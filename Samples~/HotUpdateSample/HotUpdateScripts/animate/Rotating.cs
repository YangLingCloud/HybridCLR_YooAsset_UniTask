using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单旋转动画组件，用于热更新预制体上的可视化运行验证。
/// </summary>
public class Rotating : MonoBehaviour
{
    public float rotSpeed = 0.1f;

    /// <summary>
    /// 组件启动入口，当前示例无需初始化。
    /// </summary>
    void Start()
    {
        
    }

    /// <summary>
    /// 每帧根据 rotSpeed 累加 Y 轴欧拉角。
    /// </summary>
    void Update()
    {
        // 读取当前欧拉角、调整 Y 分量后重新写回旋转。
        Vector3 rot = transform.rotation.eulerAngles;
        rot.y += rotSpeed;
        transform.rotation = Quaternion.Euler(rot);
    }
}
