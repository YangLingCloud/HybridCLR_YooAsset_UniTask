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
            // 检测构建包�?
            var packageNames = GetBuildPackageNames();

            var hasTwoAndMorePackages = packageNames.Count > 1;
            packageErrorLabel.visible = !hasTwoAndMorePackages;
            // 构建包裹
            if (!hasTwoAndMorePackages)
            {
                return false;
            }


            #region 构建资源�?

            var assetBundlePackageContainer = Root.Q("AssetBundlePackageContainer");

            int assetPackageIndex = 0;
            var scriptPackageOption = new PopupField<string>(packageNames, assetPackageIndex);
            scriptPackageOption.label = "Script Package";
            scriptPackageOption.style.width = StyleWidth;

            scriptPackageOption.RegisterValueChangedCallback(evt =>
            {
                _hybridBuilderSettings.ScriptPackageName = evt.newValue;
                RefreshAssetPackages(packageNames);
            });
            if (string.IsNullOrEmpty(_hybridBuilderSettings.ScriptPackageName))
            {
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
                    var encryptionClassName = _hybridBuilderSettings.assetEncryptionClassName;
                    int defaultIndex = encryptionClassTypes.FindIndex(x => x.FullName.Equals(encryptionClassName));
                    if (defaultIndex < 0)
                        defaultIndex = 0;
                    _encryptionField = new PopupField<Type>(encryptionClassTypes, defaultIndex);
                    _encryptionField.label = "Encryption";
                    _encryptionField.style.width = StyleWidth;
                    _encryptionField.RegisterValueChangedCallback(evt =>
                    {
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

            // 使用资源依赖数据�?
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
                        _hybridBuilderSettings.AssetPackages.Add(package);
                    }
                    else
                    {
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

        private void RefreshBuildinFileCopyOptionView()
        {
            var buildinFileCopyOption = _hybridBuilderSettings.assetBuildinFileCopyOption;
            bool tagsFiledVisible = buildinFileCopyOption == EBuildinFileCopyOption.ClearAndCopyByTags ||
                                    buildinFileCopyOption == EBuildinFileCopyOption.OnlyCopyByTags;
            _copyBuildinFileTagsField.visible = tagsFiledVisible;
        }

        private void RefreshScriptCollectorGroupNameView()
        {
            var buildOption = _hybridBuilderSettings.hybridBuildOption;
            bool nameFiledVisible = buildOption == HybridBuildOption.BuildAll ||
                                    buildOption == HybridBuildOption.BuildApplication ||
                                    buildOption == HybridBuildOption.BuildScript;

            _patchedAOTDLLFolderField.visible = nameFiledVisible;
            _hotUpdateDLLFolderField.visible = nameFiledVisible;
        }

        private void BuildButton_clicked()
        {
            if (EditorUtility.DisplayDialog("Confirm", $"Start building asset bundles with [{_hybridBuilderSettings.name}] configuration?", "Yes", "No"))
            {
                EditorTools.ClearUnityConsole();
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
        /// 创建加密类实�?
        /// </summary>
        protected IEncryptionServices CreateEncryptionInstance()
        {
            var encryptionClassName = _hybridBuilderSettings.assetEncryptionClassName;
            var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            var classType = encryptionClassTypes.Find(x => x.FullName.Equals(encryptionClassName));
            if (classType != null)
                return (IEncryptionServices) Activator.CreateInstance(classType);
            else
                return null;
        }

        private string GetDefaultPackageVersion()
        {
            var now = DateTime.Now;
            int totalMinutes = now.Hour * 60 + now.Minute;
            return now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }


        private void BrowserFolder(string defaultPath, Action<string> callBack)
        {
            string selectFolder = EditorUtility.OpenFolderPanel("Select Output Path", defaultPath, string.Empty);
            if (!string.IsNullOrEmpty(selectFolder))
            {
                callBack?.Invoke(selectFolder);
            }
        }

        private List<string> GetBuildPackageNames()
        {
            List<string> result = new List<string>();
            foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
            {
                result.Add(package.PackageName);
            }

            return result;
        }
    }
}
#endif
