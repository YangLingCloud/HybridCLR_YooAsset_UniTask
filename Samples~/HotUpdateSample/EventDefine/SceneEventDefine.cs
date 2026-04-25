using UniFramework.Event;

/// <summary>
/// 场景切换事件定义集合，用于将业务流程与 YooAsset 场景加载逻辑解耦。
/// </summary>
public class SceneEventDefine
{
    /// <summary>
    /// 切换到热更新主界面场景。
    /// </summary>
    public class ChangeToHomeScene : IEventMessage
    {
        /// <summary>
        /// 发送切换到热更新主界面场景事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new ChangeToHomeScene();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 切换到战斗场景。
    /// </summary>
    public class ChangeToBattleScene : IEventMessage
    {
        /// <summary>
        /// 发送切换到战斗场景事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new ChangeToBattleScene();
            UniEvent.SendMessage(msg);
        }
    }
}
