using UniFramework.Event;

/// <summary>
/// 用户交互重试事件定义集合，用于将补丁窗口按钮操作转发给补丁状态机。
/// </summary>
public class UserEventDefine
{
    /// <summary>
    /// 用户尝试再次初始化资源包
    /// </summary>
    public class UserTryInitialize : IEventMessage
    {
        /// <summary>
        /// 发送用户重试初始化事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new UserTryInitialize();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 用户开始下载网络文件
    /// </summary>
    public class UserBeginDownloadWebFiles : IEventMessage
    {
        /// <summary>
        /// 发送用户确认开始下载事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new UserBeginDownloadWebFiles();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 用户尝试再次请求资源版本
    /// </summary>
    public class UserTryRequestPackageVersion : IEventMessage
    {
        /// <summary>
        /// 发送用户重试请求资源版本事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new UserTryRequestPackageVersion();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 用户尝试再次更新补丁清单
    /// </summary>
    public class UserTryUpdatePackageManifest : IEventMessage
    {
        /// <summary>
        /// 发送用户重试更新资源清单事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new UserTryUpdatePackageManifest();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 用户尝试再次下载网络文件
    /// </summary>
    public class UserTryDownloadWebFiles : IEventMessage
    {
        /// <summary>
        /// 发送用户重试下载网络文件事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new UserTryDownloadWebFiles();
            UniEvent.SendMessage(msg);
        }
    }
}
