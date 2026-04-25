using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniFramework.Event;

/// <summary>
/// 补丁更新窗口控制器，监听补丁流程事件并展示进度、错误提示和用户重试入口。
/// </summary>
public class PatchWindow : MonoBehaviour
{
    /// <summary>
    /// 对话框封装类
    /// </summary>
    private class MessageBox
    {
        private GameObject _cloneObject;
        private Text _content;
        private Button _btnOK;
        private System.Action _clickOK;

        public bool ActiveSelf
        {
            get
            {
                return _cloneObject.activeSelf;
            }
        }

        /// <summary>
        /// 绑定克隆出来的对话框对象和按钮回调。
        /// </summary>
        public void Create(GameObject cloneObject)
        {
            _cloneObject = cloneObject;
            _content = cloneObject.transform.Find("txt_content").GetComponent<Text>();
            _btnOK = cloneObject.transform.Find("btn_ok").GetComponent<Button>();
            _btnOK.onClick.AddListener(OnClickYes);
        }

        /// <summary>
        /// 显示对话框并记录确认按钮回调。
        /// </summary>
        public void Show(string content, System.Action clickOK)
        {
            _content.text = content;
            _clickOK = clickOK;
            _cloneObject.SetActive(true);
            _cloneObject.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 隐藏对话框并清理本次回调。
        /// </summary>
        public void Hide()
        {
            _content.text = string.Empty;
            _clickOK = null;
            _cloneObject.SetActive(false);
        }

        /// <summary>
        /// 处理确认按钮点击。
        /// </summary>
        private void OnClickYes()
        {
            _clickOK?.Invoke();
            Hide();
        }
    }

    private readonly EventGroup _eventGroup = new EventGroup();
    private readonly List<MessageBox> _msgBoxList = new List<MessageBox>();

    // UGUI相关
    private GameObject _messageBoxObj;
    private Slider _slider;
    private Text _tips;

    /// <summary>
    /// 初始化补丁窗口 UI 引用并注册补丁事件监听。
    /// </summary>
    void Awake()
    {
        _slider = transform.Find("UIWindow/Slider").GetComponent<Slider>();
        _tips = transform.Find("UIWindow/Slider/txt_tips").GetComponent<Text>();
        _tips.text = "Initializing the game world !";
        _messageBoxObj = transform.Find("UIWindow/MessgeBox").gameObject;
        _messageBoxObj.SetActive(false);

        _eventGroup.AddListener<PatchEventDefine.InitializeFailed>(OnHandleEventMessage);
        _eventGroup.AddListener<PatchEventDefine.PatchStepsChange>(OnHandleEventMessage);
        _eventGroup.AddListener<PatchEventDefine.FoundUpdateFiles>(OnHandleEventMessage);
        _eventGroup.AddListener<PatchEventDefine.DownloadUpdate>(OnHandleEventMessage);
        _eventGroup.AddListener<PatchEventDefine.PackageVersionRequestFailed>(OnHandleEventMessage);
        _eventGroup.AddListener<PatchEventDefine.PackageManifestUpdateFailed>(OnHandleEventMessage);
        _eventGroup.AddListener<PatchEventDefine.WebFileDownloadFailed>(OnHandleEventMessage);
    }

    /// <summary>
    /// 窗口销毁时移除所有事件监听，避免重复导入示例或切换场景后残留回调。
    /// </summary>
    void OnDestroy()
    {
        _eventGroup.RemoveAllListener();
    }

    /// <summary>
    /// 接收事件
    /// </summary>
    private void OnHandleEventMessage(IEventMessage message)
    {
        if (message is PatchEventDefine.InitializeFailed)
        {
            System.Action callback = () =>
            {
                // 用户点击确认后通知状态机重新执行初始化节点。
                UserEventDefine.UserTryInitialize.SendEventMessage();
            };
            ShowMessageBox($"Failed to initialize package !", callback);
        }
        else if (message is PatchEventDefine.PatchStepsChange)
        {
            var msg = message as PatchEventDefine.PatchStepsChange;
            _tips.text = msg.Tips;
            UnityEngine.Debug.Log(msg.Tips);
        }
        else if (message is PatchEventDefine.FoundUpdateFiles)
        {
            var msg = message as PatchEventDefine.FoundUpdateFiles;
            System.Action callback = () =>
            {
                // 用户确认后才开始下载网络文件，保留下载前提示和磁盘空间检查扩展点。
                UserEventDefine.UserBeginDownloadWebFiles.SendEventMessage();
            };
            float sizeMB = msg.TotalSizeBytes / 1048576f;
            sizeMB = Mathf.Clamp(sizeMB, 0.1f, float.MaxValue);
            string totalSizeMB = sizeMB.ToString("f1");
            ShowMessageBox($"Found update patch files, Total count {msg.TotalCount} Total szie {totalSizeMB}MB", callback);
        }
        else if (message is PatchEventDefine.DownloadUpdate)
        {
            var msg = message as PatchEventDefine.DownloadUpdate;
            // 进度条按文件数量推进，文本同时展示已下载大小和总大小。
            _slider.value = (float)msg.CurrentDownloadCount / msg.TotalDownloadCount;
            string currentSizeMB = (msg.CurrentDownloadSizeBytes / 1048576f).ToString("f1");
            string totalSizeMB = (msg.TotalDownloadSizeBytes / 1048576f).ToString("f1");
            _tips.text = $"{msg.CurrentDownloadCount}/{msg.TotalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
        }
        else if (message is PatchEventDefine.PackageVersionRequestFailed)
        {
            System.Action callback = () =>
            {
                // 版本请求失败通常是网络或服务器目录问题，用户确认后重试版本请求。
                UserEventDefine.UserTryRequestPackageVersion.SendEventMessage();
            };
            ShowMessageBox($"Failed to request package version, please check the network status.", callback);
        }
        else if (message is PatchEventDefine.PackageManifestUpdateFailed)
        {
            System.Action callback = () =>
            {
                // 清单更新失败后重试清单更新，不需要重新初始化资源包。
                UserEventDefine.UserTryUpdatePackageManifest.SendEventMessage();
            };
            ShowMessageBox($"Failed to update patch manifest, please check the network status.", callback);
        }
        else if (message is PatchEventDefine.WebFileDownloadFailed)
        {
            var msg = message as PatchEventDefine.WebFileDownloadFailed;
            System.Action callback = () =>
            {
                // 下载失败后重新走创建下载器流程，避免复用失败状态。
                UserEventDefine.UserTryDownloadWebFiles.SendEventMessage();
            };
            ShowMessageBox($"Failed to download file : {msg.FileName}", callback);
        }
        else
        {
            throw new System.NotImplementedException($"{message.GetType()}");
        }
    }

    /// <summary>
    /// 显示对话框
    /// </summary>
    private void ShowMessageBox(string content, System.Action ok)
    {
        // 尝试获取一个可用的对话框
        MessageBox msgBox = null;
        for (int i = 0; i < _msgBoxList.Count; i++)
        {
            var item = _msgBoxList[i];
            if (item.ActiveSelf == false)
            {
                msgBox = item;
                break;
            }
        }

        // 如果没有可用的对话框，则创建一个新的对话框
        if (msgBox == null)
        {
            msgBox = new MessageBox();
            var cloneObject = GameObject.Instantiate(_messageBoxObj, _messageBoxObj.transform.parent);
            msgBox.Create(cloneObject);
            _msgBoxList.Add(msgBox);
        }

        // 显示对话框
        msgBox.Show(content, ok);
    }
}
