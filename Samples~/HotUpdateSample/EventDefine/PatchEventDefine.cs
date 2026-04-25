using UniFramework.Event;
using YooAsset;

/// <summary>
/// 补丁更新流程事件定义集合，用于驱动下载窗口、错误重试和下载进度展示。
/// </summary>
public class PatchEventDefine
{
    /// <summary>
    /// 补丁包初始化失败
    /// </summary>
    public class InitializeFailed : IEventMessage
    {
        /// <summary>
        /// 发送资源包初始化失败事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new InitializeFailed();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 补丁流程步骤改变
    /// </summary>
    public class PatchStepsChange : IEventMessage
    {
        public string Tips;

        /// <summary>
        /// 发送补丁流程步骤变化事件。
        /// </summary>
        public static void SendEventMessage(string tips)
        {
            var msg = new PatchStepsChange();
            msg.Tips = tips;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 发现更新文件
    /// </summary>
    public class FoundUpdateFiles : IEventMessage
    {
        public int TotalCount;
        public long TotalSizeBytes;

        /// <summary>
        /// 发送发现待下载文件事件。
        /// </summary>
        public static void SendEventMessage(int totalCount, long totalSizeBytes)
        {
            var msg = new FoundUpdateFiles();
            msg.TotalCount = totalCount;
            msg.TotalSizeBytes = totalSizeBytes;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 下载进度更新
    /// </summary>
    public class DownloadUpdate : IEventMessage
    {
        public int TotalDownloadCount;
        public int CurrentDownloadCount;
        public long TotalDownloadSizeBytes;
        public long CurrentDownloadSizeBytes;

        /// <summary>
        /// 发送下载进度更新事件。
        /// </summary>
        public static void SendEventMessage(DownloadUpdateData updateData)
        {
            var msg = new DownloadUpdate();
            msg.TotalDownloadCount = updateData.TotalDownloadCount;
            msg.CurrentDownloadCount = updateData.CurrentDownloadCount;
            msg.TotalDownloadSizeBytes = updateData.TotalDownloadBytes;
            msg.CurrentDownloadSizeBytes = updateData.CurrentDownloadBytes;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 资源版本请求失败
    /// </summary>
    public class PackageVersionRequestFailed : IEventMessage
    {
        /// <summary>
        /// 发送资源版本请求失败事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new PackageVersionRequestFailed();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 资源清单更新失败
    /// </summary>
    public class PackageManifestUpdateFailed : IEventMessage
    {
        /// <summary>
        /// 发送资源清单更新失败事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new PackageManifestUpdateFailed();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 网络文件下载失败
    /// </summary>
    public class WebFileDownloadFailed : IEventMessage
    {
        public string FileName;
        public string Error;

        /// <summary>
        /// 发送网络文件下载失败事件。
        /// </summary>
        public static void SendEventMessage(DownloadErrorData errorData)
        {
            var msg = new WebFileDownloadFailed();
            msg.FileName = errorData.FileName;
            msg.Error = errorData.ErrorInfo;
            UniEvent.SendMessage(msg);
        }
    }
}
