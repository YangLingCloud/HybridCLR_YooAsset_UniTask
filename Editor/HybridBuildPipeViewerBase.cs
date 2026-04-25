#if UNITY_2019_4_OR_NEWER
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YangLing.Hybrid.Editor;

namespace YooAsset.Editor
{
    /// <summary>
    /// Hybrid Builder 构建视图基类，负责 UI Toolkit 表单绑定、配置写回和通用构建参数采集。
    /// </summary>
    internal abstract class HybridBuildPipeViewerBase
    {
        private const int StyleWidth = 400;
        private const int LabelMinWidth = 180;

        protected readonly BuildTarget BuildTarget;
        protected readonly EBuildPipeline BuildPipeline;
        protected TemplateContainer Root;

        private TextField _buildOutputField;
        private TextField _buildVersionField;
        private PopupField<Enum> _buildModeField;
        private PopupField<Type> _encryptionField;
        private EnumField _compressionField;
        private EnumField _outputNameStyleField;
        private EnumField _copyBuildinFileOptionField;
        private TextField _copyBuildinFileTagsField;
        private ObjectField _patchedAOTDLLFolderField;
        private ObjectField _hotUpdateDLLFolderField;
        private Toggle _clearBuildCacheToggle;
        private Toggle _useAssetDependencyDBToggle;


        Foldout packagesFoldout;

        private HybridBuilderSettings _hybridBuilderSettings;

        /// <summary>
        /// 创建构建视图基类并初始化公共 UI 区域。
        /// </summary>
        public HybridBuildPipeViewerBase(EBuildPipeline buildPipeline, BuildTarget buildTarget,
            HybridBuilderSettings hybridBuildSettings,
            VisualElement parent)
        {
            BuildTarget = buildTarget;
            BuildPipeline = buildPipeline;
            _hybridBuilderSettings = hybridBuildSettings;

            if (CreateView(parent))
            {
                RefreshScriptCollectorGroupNameView();
                RefreshBuildinFileCopyOptionView();
            }
        }


        /// <summary>
        /// 创建并绑定构建参数视图，返回视图是否成功初始化。
        /// </summary>
        private bool CreateView(VisualElement parent)
        {
            // 加载布局文件
            var visualAsset = UxmlLoader.LoadWindowUXML<HybridBuildPipeViewerBase>();
            if (visualAsset == null)
                return false;

            Root = visualAsset.CloneTree();
            Root.style.flexGrow = 1f;
            parent.Add(Root);

            // 输出目录
            var assetOutputPar = Root.Q("AssetOutputPar");
            _buildOutputField = assetOutputPar.Q<TextField>("BuildOutput");
            _buildOutputField.SetValueWithoutNotify(_hybridBuilderSettings.GetBuildOutputPath());
            _buildOutputField.SetEnabled(false);

            var buildOutputPathBrowserButton = assetOutputPar.Q<Button>("BrowseButton");
            buildOutputPathBrowserButton.clicked += () =>
            {
                // 选择目录后保存到构建配置，并立即刷新只读展示字段。
                var defaultPath = _hybridBuilderSettings.GetBuildOutputPath();
                BrowserFolder(defaultPath, (selectPath) =>
                {
                    _hybridBuilderSettings.buildOutputPath = selectPath;
                    _buildOutputField.SetValueWithoutNotify(_hybridBuilderSettings.GetBuildOutputPath());
                });
            };


            var versionPar = Root.Q("VersionPar");
            var versionToggle = versionPar.Q<Toggle>("Toggle");
            versionToggle.SetValueWithoutNotify(_hybridBuilderSettings.isUseSelfIncrementingVersions);
            versionToggle.RegisterValueChangedCallback(OnVersionToggleChange);

            // 构建版本
            _buildVersionField = versionPar.Q<TextField>("BuildVersion");
            _buildVersionField.style.width = StyleWidth;
            if (_hybridBuilderSettings.isUseSelfIncrementingVersions)
            {
                _buildVersionField.SetValueWithoutNotify(_hybridBuilderSettings.GetCurrentVersion(false));
            }
            else
            {
                _buildVersionField.SetValueWithoutNotify(GetDefaultPackageVersion());
            }

            _buildVersionField.SetEnabled(false);

            var packageErrorLabel = Root.Q("PackageErrorLabel");
            // 检测构建包数量，Hybrid 构建至少需要区分脚本包与资源包。
            var packageNames = GetBuildPackageNames();

            var hasTwoAndMorePackages = packageNames.Count > 1;
            packageErrorLabel.visible = !hasTwoAndMorePackages;
            // 构建包数量不足时中断视图后续初始化，避免用户启动无效构建。
            if (!hasTwoAndMorePackages)
            {
                return false;
            }


            #region 构建资源包

            var assetBundlePackageContainer = Root.Q("AssetBundlePackageContainer");

            int assetPackageIndex = 0;
            var scriptPackageOption = new PopupField<string>(packageNames, assetPackageIndex);
            scriptPackageOption.label = "Script Package";
            scriptPackageOption.style.width = StyleWidth;

            scriptPackageOption.RegisterValueChangedCallback(evt =>
            {
                // 脚本包切换后，资源包可选列表需要排除新的脚本包。
                _hybridBuilderSettings.ScriptPackageName = evt.newValue;
                RefreshAssetPackages(packageNames);
            });
            if (string.IsNullOrEmpty(_hybridBuilderSettings.ScriptPackageName))
            {
                // 首次打开窗口时默认选择第一个 YooAsset 包作为脚本包，便于用户继续配置。
                _hybridBuilderSettings.ScriptPackageName = packageNames[0];
            }
            else
            {
                scriptPackageOption.SetValueWithoutNotify(_hybridBuilderSettings.ScriptPackageName);
            }

            assetBundlePackageContainer.Add(scriptPackageOption);

            packagesFoldout = new Foldout();
            packagesFoldout.text = "Select Build Asset Packages";
            packagesFoldout.style.width = StyleWidth;
            assetBundlePackageContainer.Add(packagesFoldout);
            RefreshAssetPackages(packageNames);

            #endregion


            // 加密方法
            {
                var encryptionContainer = Root.Q("EncryptionContainer");
                var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
                if (encryptionClassTypes.Count > 0)
                {
                    // 扫描工程内所有 YooAsset 加密服务实现，并按配置中保存的完整类型名恢复选择。
                    var encryptionClassName = _hybridBuilderSettings.assetEncryptionClassName;
                    int defaultIndex = encryptionClassTypes.FindIndex(x => x.FullName.Equals(encryptionClassName));
                    if (defaultIndex < 0)
                        defaultIndex = 0;
                    _encryptionField = new PopupField<Type>(encryptionClassTypes, defaultIndex);
                    _encryptionField.label = "Encryption";
                    _encryptionField.style.width = StyleWidth;
                    _encryptionField.RegisterValueChangedCallback(evt =>
                    {
                        // 保存完整类型名，避免同名类或命名空间调整时产生歧义。
                        _hybridBuilderSettings.assetEncryptionClassName = _encryptionField.value.FullName;
                    });
                    encryptionContainer.Add(_encryptionField);
                }
                else
                {
                    _encryptionField = new PopupField<Type>();
                    _encryptionField.label = "Encryption";
                    _encryptionField.style.width = StyleWidth;
                    encryptionContainer.Add(_encryptionField);
                }
            }

            // 压缩方式选项
            var compressOption = _hybridBuilderSettings.assetCompressOption;
            _compressionField = Root.Q<EnumField>("Compression");
            _compressionField.Init(compressOption);
            _compressionField.SetValueWithoutNotify(compressOption);
            _compressionField.style.width = StyleWidth;
            _compressionField.RegisterValueChangedCallback(evt =>
            {
                _hybridBuilderSettings.assetCompressOption = (ECompressOption) _compressionField.value;
            });

            // 输出文件名称样式
            var fileNameStyle = _hybridBuilderSettings.assetFileNameStyle;
            _outputNameStyleField = Root.Q<EnumField>("FileNameStyle");
            _outputNameStyleField.Init(fileNameStyle);
            _outputNameStyleField.SetValueWithoutNotify(fileNameStyle);
            _outputNameStyleField.style.width = StyleWidth;
            _outputNameStyleField.RegisterValueChangedCallback(evt =>
            {
                _hybridBuilderSettings.assetFileNameStyle = (EFileNameStyle) _outputNameStyleField.value;
            });

            // 首包文件拷贝选项
            var buildinFileCopyOption = _hybridBuilderSettings.assetBuildinFileCopyOption;
            _copyBuildinFileOptionField = Root.Q<EnumField>("CopyBuildinFileOption");
            _copyBuildinFileOptionField.Init(buildinFileCopyOption);
            _copyBuildinFileOptionField.SetValueWithoutNotify(buildinFileCopyOption);
            _copyBuildinFileOptionField.style.width = StyleWidth;
            _copyBuildinFileOptionField.RegisterValueChangedCallback(evt =>
            {
                _hybridBuilderSettings.assetBuildinFileCopyOption =
                    (EBuildinFileCopyOption) _copyBuildinFileOptionField.value;
                RefreshBuildinFileCopyOptionView();
            });

            // 首包文件拷贝参数
            var buildinFileCopyParams = _hybridBuilderSettings.assetBuildinFileCopyParams;
            _copyBuildinFileTagsField = Root.Q<TextField>("CopyBuildinFileParam");
            _copyBuildinFileTagsField.SetValueWithoutNotify(buildinFileCopyParams);
            _copyBuildinFileTagsField.RegisterValueChangedCallback(evt =>
            {
                _hybridBuilderSettings.assetBuildinFileCopyParams = _copyBuildinFileTagsField.value;
            });


            //补充元数据AOTDLL拷贝路径
            _patchedAOTDLLFolderField = Root.Q<ObjectField>(nameof(_hybridBuilderSettings.PatchedAOTDLLFolder));
            _patchedAOTDLLFolderField.objectType = typeof(DefaultAsset);
            _patchedAOTDLLFolderField.SetValueWithoutNotify(_hybridBuilderSettings.PatchedAOTDLLFolder);
            _patchedAOTDLLFolderField.RegisterValueChangedCallback((evt) =>
            {
                // 该字段必须指向目录，因为后续构建任务会向目录中复制多个 .bytes 文件。
                var assetPath = AssetDatabase.GetAssetPath(evt.newValue);
                if (!Directory.Exists(assetPath))
                {
                    Debug.unityLogger.Log($"Selected asset is not a folder ===> {assetPath}");
                    _patchedAOTDLLFolderField.SetValueWithoutNotify(evt.previousValue);
                }

                _hybridBuilderSettings.PatchedAOTDLLFolder = evt.newValue as DefaultAsset;
            });


            //热更新文件夹拷贝路径
            _hotUpdateDLLFolderField = Root.Q<ObjectField>(nameof(_hybridBuilderSettings.HotUpdateDLLFolder));
            _hotUpdateDLLFolderField.objectType = typeof(DefaultAsset);
            _hotUpdateDLLFolderField.SetValueWithoutNotify(_hybridBuilderSettings.HotUpdateDLLFolder);
            _hotUpdateDLLFolderField.RegisterValueChangedCallback((evt) =>
            {
                // 热更新 DLL 收集路径同样必须是目录，并且需要在脚本包 Collector 中存在。
                var assetPath = AssetDatabase.GetAssetPath(evt.newValue);
                if (!Directory.Exists(assetPath))
                {
                    Debug.unityLogger.Log($"Selected asset is not a folder ===> {assetPath}");
                    _hotUpdateDLLFolderField.SetValueWithoutNotify(evt.previousValue);
                    return;
                }

                _hybridBuilderSettings.HotUpdateDLLFolder = evt.newValue as DefaultAsset;
            });


            // 清理构建缓存
            bool clearBuildCache = _hybridBuilderSettings.isClearBuildCache;
            _clearBuildCacheToggle = Root.Q<Toggle>("ClearBuildCache");
            _clearBuildCacheToggle.SetValueWithoutNotify(clearBuildCache);
            _clearBuildCacheToggle.RegisterValueChangedCallback(evt =>
            {
                _hybridBuilderSettings.isClearBuildCache = evt.newValue;
            });

            // 使用资源依赖数据库
            bool useAssetDependencyDB = _hybridBuilderSettings.isUseAssetDependDB;
            _useAssetDependencyDBToggle = Root.Q<Toggle>("UseAssetDependency");
            _useAssetDependencyDBToggle.SetValueWithoutNotify(useAssetDependencyDB);
            _useAssetDependencyDBToggle.RegisterValueChangedCallback(evt =>
            {
                _hybridBuilderSettings.isUseAssetDependDB = evt.newValue;
            });

            //热更新脚本混合构建选项
            var hybridBuildOption = Root.Q<EnumField>("HybridBuildOption");
            hybridBuildOption.Init(_hybridBuilderSettings.hybridBuildOption);
            hybridBuildOption.SetValueWithoutNotify(_hybridBuilderSettings.hybridBuildOption);
            hybridBuildOption.style.width = StyleWidth;
            hybridBuildOption.RegisterValueChangedCallback(evt =>
            {
                _hybridBuilderSettings.hybridBuildOption = (HybridBuildOption) evt.newValue;
                RefreshScriptCollectorGroupNameView();
            });

            // 对齐文本间距
            UIElementsTools.SetElementLabelMinWidth(_buildOutputField, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_buildVersionField, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_compressionField, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_encryptionField, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_outputNameStyleField, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_copyBuildinFileOptionField, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_copyBuildinFileTagsField, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_clearBuildCacheToggle, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_useAssetDependencyDBToggle, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(hybridBuildOption, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(scriptPackageOption, LabelMinWidth);
            
            UIElementsTools.SetElementLabelMinWidth(packagesFoldout, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_patchedAOTDLLFolderField, LabelMinWidth);
            UIElementsTools.SetElementLabelMinWidth(_hotUpdateDLLFolderField, LabelMinWidth);

            // 构建按钮
            var buildButton = Root.Q<Button>("Build");
            buildButton.clicked += BuildButton_clicked;

            return true;
        }

        /// <summary>
        /// 刷新资源包勾选列表，排除当前脚本包并把勾选状态写回构建配置。
        /// </summary>
        private void RefreshAssetPackages(List<string> packageNames)
        {
            packagesFoldout.Clear();
            var selectablePackages = new List<string>(packageNames);
            selectablePackages.Remove(_hybridBuilderSettings.ScriptPackageName);
            foreach (var package in selectablePackages)
            {
                var packageToggle = new Toggle(package);
                packageToggle.style.width = StyleWidth;
                packageToggle.RegisterValueChangedCallback(select =>
                {
                    if (select.newValue)
                    {
                        // 勾选后加入资源包构建列表。
                        _hybridBuilderSettings.AssetPackages.Add(package);
                    }
                    else
                    {
                        // 取消勾选后从资源包构建列表移除。
                        _hybridBuilderSettings.AssetPackages.Remove(package);
                    }
                });
                if (_hybridBuilderSettings.AssetPackages.Contains(package))
                {
                    packageToggle.SetValueWithoutNotify(true);
                }

                packagesFoldout.Add(packageToggle);
            }
        }

        /// <summary>
        /// 自动递增版本开关变化时刷新版本展示文本。
        /// </summary>
        private void OnVersionToggleChange(ChangeEvent<bool> evt)
        {
            _hybridBuilderSettings.isUseSelfIncrementingVersions = evt.newValue;
            if (_hybridBuilderSettings.isUseSelfIncrementingVersions)
            {
                _buildVersionField.SetValueWithoutNotify(_hybridBuilderSettings.GetCurrentVersion(false));
            }
            else
            {
                _buildVersionField.SetValueWithoutNotify(GetDefaultPackageVersion());
            }
        }

        /// <summary>
        /// 根据 YooAsset 内置文件拷贝模式显示或隐藏标签参数输入框。
        /// </summary>
        private void RefreshBuildinFileCopyOptionView()
        {
            var buildinFileCopyOption = _hybridBuilderSettings.assetBuildinFileCopyOption;
            bool tagsFiledVisible = buildinFileCopyOption == EBuildinFileCopyOption.ClearAndCopyByTags ||
                                    buildinFileCopyOption == EBuildinFileCopyOption.OnlyCopyByTags;
            _copyBuildinFileTagsField.visible = tagsFiledVisible;
        }

        /// <summary>
        /// 根据构建模式显示或隐藏脚本 DLL 相关目录配置。
        /// </summary>
        private void RefreshScriptCollectorGroupNameView()
        {
            var buildOption = _hybridBuilderSettings.hybridBuildOption;
            bool nameFiledVisible = buildOption == HybridBuildOption.BuildAll ||
                                    buildOption == HybridBuildOption.BuildApplication ||
                                    buildOption == HybridBuildOption.BuildScript;

            _patchedAOTDLLFolderField.visible = nameFiledVisible;
            _hotUpdateDLLFolderField.visible = nameFiledVisible;
        }

        /// <summary>
        /// 构建按钮点击回调，确认后延迟到下一帧执行实际构建。
        /// </summary>
        private void BuildButton_clicked()
        {
            if (EditorUtility.DisplayDialog("Confirm", $"Start building asset bundles with [{_hybridBuilderSettings.name}] configuration?", "Yes", "No"))
            {
                EditorTools.ClearUnityConsole();
                // 使用 delayCall 避免在 UI 事件回调栈内直接执行长耗时构建任务。
                EditorApplication.delayCall += ExecuteBuild;
            }
            else
            {
                Debug.LogWarning("[Build] Build cancelled");
            }
        }

        /// <summary>
        /// 执行构建任务
        /// </summary>
        protected abstract void ExecuteBuild();

        /// <summary>
        /// 获取构建版本
        /// </summary>
        protected string GetPackageVersion()
        {
            return _buildVersionField.value;
        }

        /// <summary>
        /// 创建加密类实例。
        /// </summary>
        protected IEncryptionServices CreateEncryptionInstance()
        {
            var encryptionClassName = _hybridBuilderSettings.assetEncryptionClassName;
            var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            var classType = encryptionClassTypes.Find(x => x.FullName.Equals(encryptionClassName));
            if (classType != null)
            {
                // YooAsset 构建参数需要实际服务实例，因此通过反射创建选中的加密类。
                return (IEncryptionServices) Activator.CreateInstance(classType);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 生成非自增模式下的默认包版本，格式为日期加当天分钟数。
        /// </summary>
        private string GetDefaultPackageVersion()
        {
            var now = DateTime.Now;
            int totalMinutes = now.Hour * 60 + now.Minute;
            return now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }


        /// <summary>
        /// 打开目录选择面板，并在用户选择后通过回调返回路径。
        /// </summary>
        private void BrowserFolder(string defaultPath, Action<string> callBack)
        {
            string selectFolder = EditorUtility.OpenFolderPanel("Select Output Path", defaultPath, string.Empty);
            if (!string.IsNullOrEmpty(selectFolder))
            {
                callBack?.Invoke(selectFolder);
            }
        }

        /// <summary>
        /// 从 YooAsset Collector 设置中读取当前工程所有资源包名称。
        /// </summary>
        private List<string> GetBuildPackageNames()
        {
            List<string> result = new List<string>();
            foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
            {
                // 包名称来自 YooAsset 设置文件，是构建视图中脚本包和资源包选择的唯一来源。
                result.Add(package.PackageName);
            }

            return result;
        }
    }
}
#endif
