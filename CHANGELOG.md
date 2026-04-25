# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.1.2] - 2026-04-25

### Fixed

- 文件名大小写：`Editor/HybridBuilderWIndow.uxml` → `HybridBuilderWindow.uxml`，避免 Linux/Mac CI 上找不到文件
- `BuildHelper`：剩余 3 处裸 `Debug.Log` 改为 `Debug.unityLogger.Log("BuildHelper", ...)`，与全文件统一
- `BuildHelper.GetCachedPreservableProperties`：原 `catch (Exception)` 静默吞异常改为带类型名和错误信息的 `LogWarning`，便于排查

### Changed

- `TaskBuildScript_SBP`：用 `AssetDatabase.StartAssetEditing/StopAssetEditing` 包裹两次 Copy，将串联场景下的两次全工程 Refresh 合并为一次；两个 Copy 方法独立调用时仍各自刷新
- `HybridBuilderSettings`：抽取 `SetField<T>(ref T, T)` 私有方法，14 个属性 setter 改为单行委托；同时跳过相等赋值，避免无意义的 SetDirty
- `HybridBuilderWindow`：合并 `FindAllHybridBuilderSettings` / `FindAllHybridRuntimeSettings` 为泛型 `FindAllAssets<T>`
- `HybridScriptableBuildPipelineViewer.BuildFinish`：提取 `IncrementScriptPackage` / `IncrementAssetPackages` 两个本地方法，4 个 case 不再重复 `UpdatePackageVersion` + `Version++` 模式

## [3.1.1] - 2026-04-25

### Added

- `Editor/HybridBuilderWindow.uss`：现代化 UI 样式表，包含卡片式布局、统一配色方案、按钮样式、状态标签等

### Changed

- `Editor/HybridBuilderWindow.uxml`：重构为简洁的工具栏布局，引入 USS 样式表
- `Editor/HybridBuildPipeViewerBase.uxml`：采用卡片式布局，应用统一样式类
- `Editor/HybridBuilderWindow.cs`：优化空状态提示，使用友好的错误/信息卡片替代简单文本

### Improved

- 界面采用深色主题配色方案（主色 #3A79BB，成功 #28A745，警告 #FFC107，错误 #DC3545）
- 所有卡片使用 6px 圆角和统一内边距，增强视觉层次
- 表单字段统一标签宽度 140px，输入框带 Hover/Focus 状态反馈
- 按钮分为主要/次要/成功/危险四种样式，高度统一为 40px/28px
- 错误/警告/信息标签带左侧彩色边框，提升可读性

## [3.1.0] - 2026-04-24

### Breaking Changes

- 为所有源码新增命名空间：`YangLing.Hybrid.Editor`（及 `.BuildPipelineTask` / `.ScriptableBuildPipeline` 子命名空间）、`YangLing.Hybrid.Runtime`。使用类型的地方需 `using` 对应命名空间。已通过 `[MovedFrom]` 保留 ScriptableObject 资产的反序列化兼容性。
- `HybridRuntimeSettings.Packages` 由 JSON 字符串改为结构化 `List<PackageVersion>`。旧字段通过 `[FormerlySerializedAs("Packages")]` + `_packagesLegacyJson` 保留，并由 `HybridRuntimeSettingsMigrator` 在 Editor 启动时一次性迁移。
- 拼写修复（会影响外部调用方）：
  - `HybrdiScriptableBuildPipeline` → `HybridScriptableBuildPipeline`（文件 + 类名）
  - `HybridBuilderWIndow.uxml` → `HybridBuilderWindow.uxml`
  - `assetEncyptionClassName` → `assetEncryptionClassName`（旧序列化字段通过 `FormerlySerializedAs` 兼容）
  - `CheckScriptPathExsist` → `CheckScriptPathExist`
  - 版本显示字符串：`Realse` / `AssetPakcage` / `ScriptPackge` → `Release` / `AssetPackage` / `ScriptPackage`

### Added

- `Editor/HybridPaths.cs`：集中管理 AOT / 热更 DLL 目录、link.xml、清单文件名等常量。
- `Editor/HybridRuntimeSettingsMigrator.cs`：首次加载时自动将旧版 `Packages` JSON 字段迁移到新的结构化列表。
- `BuildApplication` 非 Android 平台的显式错误日志（替换原本的静默失败）。
- 反射属性结果缓存（`BuildHelper.CollectPreserveType`）。

### Fixed

- `BuildHelper.CheckAccessMissingMetadata`：修复热更新程序集列表为空时误判为通过的逻辑；现在空列表返回 false 并发出警告，实际检查失败也会累积标记而非首次失败即跳出。
- `HybrdiScriptableBuildPipeline.Run`：修复强转后未使用变量的问题，真正的 `HybridScriptableBuildParameters` 传入 `builder.Run`。
- `HybridScriptableBuildPipelineViewer.BuildAsset`：移除 `CompressOption` 重复赋值。
- `BuildHelper.SupplementPrefabDependent`：使用 try/finally 保证进度条清理；将全量 `AssetDatabase.Refresh()` 替换为针对 link.xml 的 `ImportAsset`。
- `HybridBuilderWindow`：`FindAll*Settings` 不再抛异常，显示友好的空状态提示。
- `HybridBuilderSettings` setters 不再静默忽略空值；错误日志使用英文 tag 并去除中文提示。
- `BuildFinish`：写 RuntimeSettings.json 前确保目录存在；仅在实际发生变更时写入。
- `SceneHelper`：移除死代码（`ValidatePlayModeUseFirstScene` / `UpdatePlayModeUseFirstScene`）与无意义 `playModeStateChanged += null;`；菜单文本改英文。

### Changed

- `ExecuteBuild` 重构为调用 `ValidateMetadata / ValidateScriptPath / ValidateAssetPackages` 三个校验方法，消除四个分支的重复代码。
- `CheckScriptPathExist`：两个 GUID 都找到后立即 return，避免继续遍历剩余收集器。
- `BuildHelper.ProjectPath` 改为属性，避免静态字段在域重载时陈旧。
- 移除 `System.Drawing` 等未使用 using。
- 移除未使用的 `GetBuiltinShaderBundleName` 方法。
- 同步更新 `HybridBuildPipelineTests.cs` 的断言以匹配新版本字符串、类名与命名空间。
- **测试套件精简（25 → 8）**：删除恒真断言、上游 API 测试（`SettingsUtil`/HybridCLR 字段存在性）、宿主工程配置型断言（`BuildScenes`/`HostServerIP` 等），以及死代码 `ResolveSnapshotPath`。重复的路径解析、版本字符串、空路径防御测试合并为参数化测试。最终保留 8 个高价值用例：路径解析、版本字符串构建/展示格式、空路径防御、源目录缺失、流水线类型校验、端到端 GenerateAll、端到端 CopyHotUpdateDll。`Platform_*` 参数化 SlowTest 移除（单机上仅当前平台真跑、其余 Inconclusive，价值低于成本）。

## [3.0.1] - 2026-03-07

### Added

- 新增 `VersionLogic` 测试组：版本自增正确性、`GetCurrentVersion` 构建/展示双格式、版本递增后输出路径同步变化。
- 新增 `PipelineTypeValidation` 测试：`HybrdiScriptableBuildPipeline` 传入非法参数类型时抛出异常。
- 新增 `CopyDllEdgeCases` 测试组：`CopyPatchedAOTDll` / `CopyHotUpdateDll` 空路径防御、`CopyDllFileToByte` 源目录不存在时返回空列表。
- 新增 `version-update` Agent Skill（`.opencode/skills/version-update/SKILL.md`）：标准化版本发布流程，自动递增 patch 版本、同步 CHANGELOG / package.json / AGENTS.md，提交前询问是否推送。
- 新增 `Samples~/HotUpdateSample/AGENTS.md`：样例级 AI 代理上下文，覆盖 FSM 架构、程序集分离、运行时启动流程及样例专属反模式。

### Changed

- 更新 README.md 测试覆盖范围表格，新增 VersionLogic / PipelineTypeValidation / CopyDllEdgeCases 三组测试说明。
- 更新 README.md 测试边界说明：明确本包测试仅验证自身功能，YooAsset 资源打包与 Unity 构建管线由各自测试保证。
- 扩展 README.md / README_EN.md 项目结构树：补充 `README/` 目录、`SceneHelper.cs`、UXML 布局文件、asmdef 及子目录详情。
- 扩展 README.md / README_EN.md `HybridBuilderSettings` 代码片段：从 8 个字段扩展至全部 16 个字段（含 `buildOutputPath`、YooAsset 打包选项、`hybridBuildOption` 等）。
- README.md / README_EN.md 编辑器菜单补充 `Hybrid Builder` 条目。
- README_EN.md 测试覆盖表格补齐 VersionLogic / PipelineTypeValidation / CopyDllEdgeCases 三组及测试边界说明（与中文版对齐）。
- 更新根 AGENTS.md：补充 `README/` 目录、`animate/` 小写命名注记、占位 GUID 警告、文档资产维护指引。

## [3.0.0] - 2026-02-25

### Added

- 新增 `package.json`，将仓库定义为 UPM 包 `com.yanglingyun.hyu`（最低 Unity 版本 2022.3）。
- 新增顶层 `Editor/` 目录，包含构建流水线核心编辑器代码：
  - `BuildHelper.cs` — AOT 元数据检查、DLL 拷贝辅助。
  - `HybridBuilderWindow.cs` — UI Toolkit 构建窗口。
  - `HybridBuilderSettings.cs` — 构建配置 ScriptableObject。
  - `HybridBuildPipeViewerBase.cs` / `HybridScriptableBuildPipelineViewer.cs` — 构建流水线查看器。
  - `SceneHelper.cs` — 场景工具。
  - `BuildPipelineTask/TaskBuildScript_SBP.cs` — SBP 自定义构建任务。
  - `ScriptableBuildPipeline/` — SBP 流水线实现及参数定义。
- 新增顶层 `Runtime/` 目录：
  - `HybridRuntimeSettings.cs` — 运行时配置（`HostServerIP`、`ReleaseBuildVersion`、`Packages`），从原 `Assets/AOTScripts/` 提升为包级运行时程序集。
- 新增 `Samples~/HotUpdateSample/` 可导入示例，包含完整热更新演示：
  - `AOTScripts/` — AOT 侧运行时脚本（`HttpHelper.cs`、`SampleBundleEncryption.cs`）。
  - `HotUpdateScripts/` — 热更新程序集（`HotUpdateLauncher.cs`、`LoadImage.cs`、`ModelRotate.cs` 等）。
  - `PatchLogic/` — YooAsset 补丁下载状态机（8 个 FSM 节点）。
  - `EventDefine/` — UniEvent 事件定义（Battle / Patch / Scene / User）。
  - `Scripts/` — AOT 主场景脚本（`GameManager.cs`、`HybridLauncher.cs`）。
  - `HotUpdateAssets/` — 待打包热更资源（Prefabs、Scenes、Textures、Materials 等）。
  - `ThirdParty/` — 轻量依赖库（UniEvent、UniMachine、UniUtility）。
  - `Settings/` — 预配置的 ScriptableObject 资产（`AssetBundleCollectorSetting`、`HybridBuilderSettings`、`HybridRuntimeSettings`）。
  - `Editor/HybridCLRSettingsSnapshot.json` — HybridCLR 设置快照，用于快速还原配置。
  - `Editor/HybridSettingsImporter.cs` — 自动/手动设置导入器。
- 新增 `Samples~/BuildTests/Editor/HybridBuildPipelineTests.cs` EditMode 测试：
  - 构建配置验证（HybridCLR 配置、程序集列表、场景、路径）。
  - BuilderSettings / RuntimeSettings 资产校验。
  - 平台参数化测试（Windows / Android / iOS）。
  - 首次构建前置条件检查（AOT strip 目录、`GenerateAll` 完整性、`MetadataCheck` 通过性）。
- 新增首次构建引导行为：当 AOT strip 输出缺失时，自动提示通过 `PrebuildCommand.GenerateAll()` 生成前置依赖。
- 新增 `link.xml` 防御性处理：`SupplementPrefabDependent` 在 `HybridCLRData/Generated/link.xml` 缺失时自动创建合法 XML 文档。
- 新增示例侧 Collector 路径规范化菜单：`HybridTool/Sample-HotUpdateSample/Normalize Collector Paths`。
- 新增 `CHANGELOG.md`、`README_EN.md`（英文文档）、`LICENSE`（MIT 协议）、`AGENTS.md`（AI 代理工作流上下文）。

### Changed

- **仓库结构从 Unity 工程迁移为纯 UPM 包根目录**：
  - 根目录不再包含 `Assets/`、`ProjectSettings/`、`Packages/` 等 Unity 工程文件夹，
  - 根目录现为标准 UPM 布局：`package.json` + `Editor/` + `Runtime/` + `Samples~/`。
- **依赖管理方式变更**：
  - 原项目通过 `Packages/` 目录内嵌 NuGet 包（`System.Buffers`、`System.Memory`、`System.Collections.Immutable` 等）和 `Assets/Plugins/Newtonsoft.Json.dll`，
  - 现通过 `package.json` 的 `dependencies` 字段声明，由 UPM 自动解析：
    - `com.code-philosophy.hybridclr`: 8.2.0
    - `com.tuyoogame.yooasset`: 2.3.9
    - `com.unity.scriptablebuildpipeline`: 1.21.21
    - `com.unity.nuget.newtonsoft-json`: 3.2.1
    - `com.cysharp.unitask`: 2.5.10
- **安装方式变更**：
  - 旧版需使用 `?path=Packages/com.yanglingyun.hyu` 子路径引用，
  - 新版直接使用仓库 Git URL 安装即可。
- **文件位置迁移**（原路径 → 新路径）：
  - `Assets/Editor/*` → `Editor/*`（包级编辑器程序集）。
  - `Assets/AOTScripts/HybridRuntimeSettings.cs` → `Runtime/HybridRuntimeSettings.cs`（包级运行时程序集）。
  - `Assets/AOTScripts/` 其余文件 → `Samples~/HotUpdateSample/AOTScripts/`。
  - `Assets/HotUpdateScripts/` → `Samples~/HotUpdateSample/HotUpdateScripts/`。
  - `Assets/PatchLogic/` → `Samples~/HotUpdateSample/PatchLogic/`。
  - `Assets/EventDefine/` → `Samples~/HotUpdateSample/EventDefine/`。
  - `Assets/ThirdParty/` → `Samples~/HotUpdateSample/ThirdParty/`。
  - `Assets/GameManager.cs`、`Assets/HybridLauncher.cs` → `Samples~/HotUpdateSample/Scripts/`。
  - `Assets/HotUpdateAssets/` → `Samples~/HotUpdateSample/HotUpdateAssets/`。
  - `Assets/Resources/PatchWindow.prefab` → 随示例一起分发。
- `HybridBuilderSettings.buildOutputPath` 改为支持项目根目录相对路径（默认 `Bundles`），运行时通过 `ResolveBuildOutputPath()` 解析。
- 示例 Collector 路径改为存储示例相对路径，导入后由规范化工具转换为绝对路径。
- 设置快照导入/导出路径优先使用已导入示例的本地路径（`Assets/Samples/.../Hot Update Sample/Editor`）。
- 所有编辑器菜单标签统一为英文。
- 所有 UI/对话框/日志中的中文字符串统一为英文。
- 重写 `README.md` 和 `README_EN.md`，适配纯包结构。

### Fixed

- 修复首次构建时因前置依赖不完整导致元数据检查崩溃或失败的问题。
- 修复 `SupplementPrefabDependent` 健壮性问题：
  - 空组件（Missing Script）安全处理，
  - 更安全的反射遍历，
  - 基于 Set 的去重合并，防止重复类型插入，
  - 稳定的 XML 节点创建与合并行为。
- 修复示例名称不一致（`Hot Update Sample` vs `HotUpdateSample`）导致示例根目录发现失败的问题。
- 修复纯包迁移后残留的硬编码路径（`Packages/com.yanglingyun.hyu/...`）。

### Removed

- 移除仓库中的 Unity 工程结构：
  - 删除 `Assets/`（含所有子目录：AOTScripts、Editor、HotUpdateScripts、PatchLogic、EventDefine、HotUpdateAssets、HybridCLRData、Plugins、Resources、Scenes、ThirdParty 等）。
  - 删除 `ProjectSettings/`（含全部 Unity 项目设置文件）。
  - 删除 `Packages/manifest.json`、`Packages/packages-lock.json` 及内嵌 NuGet 包（`System.Buffers`、`System.Memory`、`System.Collections.Immutable`、`System.Numerics.Vectors`、`System.Reflection.Metadata`、`System.Runtime.CompilerServices.Unsafe`）。
  - 删除 `Assets/Plugins/Newtonsoft.Json.dll` 和 `Assets/Plugins/Android/unityandroid-debug.aar`。
  - 删除 URP 相关资产（`UniversalRenderPipelineGlobalSettings`、Render Pipeline Asset 等）。
  - 删除 `UserSettings/` 及生成的 `.sln` / `.csproj` 工程文件。

### Breaking Changes

- **仓库不再是 Unity 工程**，而是纯 UPM 包源码仓库，无法直接用 Unity Hub 打开。
- 安装方式变更：不再需要 `?path=Packages/com.yanglingyun.hyu` 后缀。
- 所有依赖原项目根目录结构（`Assets/`、`ProjectSettings/`）的自动化脚本需要更新。
- 内嵌的 NuGet DLL 和 Newtonsoft.Json 插件已移除，改由 `package.json` 依赖声明自动安装。

### Migration Guide (from 2.x)

1. 更新包引用为仓库根 URL（移除 `?path=` 参数）。
2. 在宿主项目的 Package Manager 中重新导入 `Hot Update Sample` 示例。
3. 导入后运行一次示例设置菜单：
   - `HybridTool/Sample-HotUpdateSample/Restore HybridCLR Settings from Snapshot`
   - `HybridTool/Sample-HotUpdateSample/Normalize Collector Paths`
4. 首次构建时，按提示允许自动执行 `GenerateAll` 以生成前置依赖。
5. 若项目中已手动引用 `Newtonsoft.Json.dll`，可移除——现由 `com.unity.nuget.newtonsoft-json` 包自动提供。

## [2.0.0] - previous release

- Previous major release baseline (tag: `V2.0.0`).

## [1.0.0] - initial release

- Initial public release baseline (tag: `V1.0.0`).
