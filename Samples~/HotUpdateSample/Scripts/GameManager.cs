using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Event;
using YooAsset;

/// <summary>
/// 示例全局游戏管理器，提供协程代理并监听场景事件以触发 YooAsset 场景加载。
/// </summary>
public class GameManager
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameManager();
            return _instance;
        }
    }

    private readonly EventGroup _eventGroup = new EventGroup();

    /// <summary>
    /// 协程启动器
    /// </summary>
    public MonoBehaviour Behaviour;


    /// <summary>
    /// 初始化全局事件监听，接收场景切换事件并转发到 YooAsset 场景加载。
    /// </summary>
    private GameManager()
    {
        // 注册监听事件
        _eventGroup.AddListener<SceneEventDefine.ChangeToHomeScene>(OnHandleEventMessage);
        _eventGroup.AddListener<SceneEventDefine.ChangeToBattleScene>(OnHandleEventMessage);
    }

    /// <summary>
    /// 开启一个协程
    /// </summary>
    public void StartCoroutine(IEnumerator enumerator)
    {
        Behaviour.StartCoroutine(enumerator);
    }

    /// <summary>
    /// 接收事件
    /// </summary>
    private void OnHandleEventMessage(IEventMessage message)
    {
        if (message is SceneEventDefine.ChangeToHomeScene)
        {
            // 示例中主界面场景通过 YooAsset 地址加载，验证热更新资源包加载链路。
            YooAssets.LoadSceneAsync("HotUpdateScene");
        }
    }
}
