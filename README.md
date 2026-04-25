# HybridCLR + YooAsset + UniTask 集成解决方案

<div align="center">

[![Unity 2022.3](https://img.shields.io/badge/Unity-2022.3-brightgreen)](https://unity.com/) [![HybridCLR](https://img.shields.io/badge/HybridCLR-v8.2.0-blue)](https://github.com/focus-creative-games/hybridclr) [![YooAsset](https://img.shields.io/badge/YooAsset-v2.3.9-orange)](https://github.com/tuyoogame/YooAsset) [![UniTask](https://img.shields.io/badge/UniTask-v2.5.10-purple)](https://github.com/Cysharp/UniTask) [![License](https://img.shields.io/badge/License-MIT-green)](LICENSE) [![English](https://img.shields.io/badge/English-Document-blue)](./README_EN.md)


</div>

---

## 目录导航

- [项目简介](#项目简介)
- [核心概念](#核心概念)
- [安装与依赖](#安装与依赖)
- [快速开始](#快速开始)
- [集成工具](#集成工具)
- [构建流程](#构建流程)
- [编辑器菜单](#编辑器菜单)
- [项目结构](#项目结构)
- [Sample 使用说明](#sample-使用说明)
- [测试说明](#测试说明)
- [常见问题](#常见问题)
- [最佳实践](#最佳实践)

---

## 项目简介

**HybridCLR + YooAsset + UniTask **是一个专为 Unity 开发者设计的高性能热更新与资源管理框架。

- 热更新 DLL 编译与拷贝
- AOT 元数据检查与补充流程
- 资源打包与脚本打包联动
- Sample 快照导入与路径规范化
- 一体化编辑器构建窗口

基于 **Unity 2022.3、HybridCLR 8.2.0、YooAsset 2.3.9、UniTask 2.5.10** 版本整合。

---

## 核心概念

### Assembly-CSharp.dll

`Assembly-CSharp` 为 Unity 自动整合的 DLL，在 Unity 工程中任何没有被单独编译的代码都会被整合进这个 `Assembly-CSharp.dll` 中。

### Assembly Definition

`Assembly Definition` 是 Unity 2017.3 以后推出的功能，主要解决庞大程序集的编译时效问题。

在 Assets 目录下任意文件夹创建 `Assembly Definition`，会使该文件夹下所有代码单独编译成 DLL，修改该文件夹下代码时，只会重新编译该 DLL，而不会重新编译 `Assembly-CSharp.dll`。

### AOT 与热更新程序集

#### 热更新程序集

热更新程序集理论上可以是 `Assembly-CSharp` 程序集，但为保证项目逻辑清晰、资源管理方便，当前框架使用 `AssemblyDefinition` 划分单独的 DLL 作为热更新程序集。热更新 Assembly 不应被 IL2CPP 处理并编译到最终包体中。

HybridCLR 处理了 `IFilterBuildAssemblies` 回调，将热更新 DLL 从 `build assemblies` 列表移除。

#### AOT 程序集

AOT 程序集是随包一起打出，不会被更新的代码。在当前框架定义下，`Assembly-CSharp` 为主 AOT 程序集，使用 `AssemblyDefinition` 划分其他 AOT 程序集。

将 `Assembly-CSharp` 作为 AOT 程序集时强烈建议关闭热更新程序集的 `auto reference` 选项，因为 `Assembly-CSharp` 是最顶层 Assembly，会自动引用剩余所有 Assembly，容易出现失误引用热更新程序集的情况。

### UniTask

UniTask 是 GitHub 上的开源库，为 Unity 提供高性能异步解决方案，可以代替协程实现异步操作，同时兼容 Unity 生命周期，使得 Awake、Start、协程等方法都可以异步执行，但仍运行在主线程上。

### 热更新 DLL 的加载

HybridCLR 官方推荐将代码直接挂载在预制体上，通过 AssetBundle 加载预制体的方法进行热更新加载。也可以通过从加载的热更新 DLL 中直接反射出热更新类并使用 AddComponent 方法挂载到物体上实现热更新。无论哪种方式，都需要在加载预制体或加载类之前，提前加载好热更新的 DLL。

### HybridCLR 首次构建前置链

首次在新工程通过 `HybridBuilder` 执行构建时，需要保证完整前置链：

1. 编译热更新 DLL
2. 生成 IL2CPP 定义
3. 生成 link.xml
4. 生成裁剪后的 AOT DLL

`HybridBuilder` 在必要时会触发 `PrebuildCommand.GenerateAll()` 自动补齐。

---

## 安装与依赖

### 系统要求

- **Unity 版本**: 2022.3 LTS 或更高
- **目标平台**: Windows、Android、iOS
- **开发环境**: Visual Studio 2019+ 或 Rider

### 安装步骤

1. 通过[![HybridCLR](https://img.shields.io/badge/HybridCLR-v8.2.0-blue)](https://github.com/focus-creative-games/hybridclr) [![YooAsset](https://img.shields.io/badge/YooAsset-v2.3.9-orange)](https://github.com/tuyoogame/YooAsset) [![UniTask](https://img.shields.io/badge/UniTask-v2.5.10-purple)](https://github.com/Cysharp/UniTask)安装第三方包
2. 通过 `Package Manager → Add Package From URL` 添加：

```
https://github.com/YangLingCloud/HybridCLR_YooAsset_UniTask.git
```

### 包依赖

`package.json` 已声明以下依赖（自动安装）：

- `com.code-philosophy.hybridclr` — HybridCLR 热更新核心
- `com.tuyoogame.yooasset` — YooAsset 资源管理
- `com.cysharp.unitask` — UniTask 异步编程
- `com.unity.scriptablebuildpipeline` — SBP 构建管线
- `com.unity.nuget.newtonsoft-json` — JSON 序列化

---

## 快速开始

### 1. 安装包

通过 Package Manager 安装（见上方安装步骤）。

### 2. 导入 Samples

通过 Package Manager 找到 `com.yanglingyun.hyu`，点击 **Samples** 标签导入：

- **Hot Update Sample** — 热更新示例
- **Build Pipeline Tests** — 构建管线测试

导入后路径为：`Assets/Samples/com.yanglingyun.hyu/<version>/Hot Update Sample/`

### 3. 初始化 Sample 设置

执行菜单：

1. `HybridTool/Sample-HotUpdateSample/Restore HybridCLR Settings from Snapshot` — 快速导入 HybridCLR 配置
2. `HybridTool/Sample-HotUpdateSample/Normalize Collector Paths` — 快速设置 AssetBundleCollectorSetting

### 4. 配置运行时参数

打开 `HybridRuntimeSettings` 资产，填写 `HostServerIP`（CDN / 资源服务器地址）。

### 5. 执行构建

1. 通过 `HybridTool/` 菜单执行首次构建（自动触发 `PrebuildCommand.GenerateAll()` 生成 AOT 裁剪 DLL）
2. 后续迭代仅需执行热更新 DLL 编译与资源打包

---

## 集成工具

### HybridTool 整合工具

由于 YooAsset 和 HybridCLR 都是通过 Unity Package Manager 加载的，导致很多代码不够好用又无法修改，因此通过编辑器代码写了一套整合工具，使两个第三方库可以更好地配合工作。

#### 主要功能

| 功能模块 | 功能描述 | 使用场景 |
|---------|---------|---------|
| **验证元数据补充需求** | 对比 AOT 与热更新 DLL | 打包前检查 |
| **APK 打包流程** | 自动化打包与依赖分析 | 完整构建流程 |
| **AOT 元数据生成** | 自动生成补充文件 | 解决裁剪问题 |
| **热更新 DLL 编译** | 编译生成热更新代码 | 开发阶段 |
| **预制体依赖补全** | 自动补全 link.xml | 解决组件引用问题 |

### HybridBuilderWindow

基于 **UI Toolkit** 的现代化打包工具窗口，重写了 YooAsset.AssetBundleBuilderWindow，并增加了代码打包相关设置项。

#### 核心组件

- **HybridBuilderWindow** — 窗口主控制器
- **HybridBuilderWindow.uxml** — UI 布局定义文件
- **HybridBuildPipeViewerBase** — 核心功能实现基类

#### 使用 HybridBuilderWindow 打包

在 Unity 编辑器中，点击菜单栏：`HybridTool/Hybrid Builder` 打开窗口。

**配置打包设置：**

1. 选择 HybridBuilderSetting：窗口中会列出项目中所有的 HybridBuilderSetting 文件，选择要使用的配置
2. 选择 HybridRuntimeSetting：选择运行时设置文件，该文件定义了资源包和版本信息
3. 选择打包选项：可以选择打包资产、脚本或全部

**执行打包：**

点击"构建"按钮开始打包。打包过程会自动处理以下步骤：
- 验证元数据补充需求
- 编译热更新 DLL
- 生成 AOT 元数据
- 打包 AssetBundle 资源

### HybridScriptableBuildPipeline

主要打包逻辑在 HybridScriptableBuildPipelineViewer 中实现，仅在运行时对打包 Asset 或 Script 进行区分。

#### 对 YooAsset 打包流程的修改

1. **运行时区分打包类型** — Asset 或 Script 使用不同的构建管道
2. **增强 RawFileBuildPipeline** — 增加 TaskBuildScript_SBP 流程
3. **批量打包支持** — 通过包名列表配置一次性打多个包
4. **APK 打包优化** — 优化构建流程与错误检查
5. **裁剪检查** — 构建前检查热更新代码是否访问了被裁切代码

### HybridBuilderSettings 配置

```csharp
public class HybridBuilderSettings : ScriptableObject
{
    public HybridRuntimeSettings RuntimeSettings;  // 关联的运行时配置
    public List<string> AssetPackages;              // 资源包包名列表
    public string ScriptPackageName;                // 脚本包包名
    public DefaultAsset PatchedAOTDLLFolder;        // AOT 补充元数据 DLL 目录
    public DefaultAsset HotUpdateDLLFolder;         // 热更新 DLL 目录
    public int ReleaseBuildVersion;                 // 发行版本号
    public int AssetBuildVersion;                   // 资源构建版本号
    public int ScriptBuildVersion;                  // 脚本构建版本号
    public string buildOutputPath;                  // 构建输出路径（支持相对路径）
    public bool isClearBuildCache;                  // 是否清除构建缓存
    public bool isUseAssetDependDB;                 // 是否使用资源依赖数据库（加速构建）
    public bool isUseSelfIncrementingVersions;      // 是否使用自增版本号
    public ECompressOption assetCompressOption;     // AB 包压缩方式
    public EFileNameStyle assetFileNameStyle;       // AB 包命名方式
    public string assetEncyptionClassName;          // AB 包加密类名
    public EBuildinFileCopyOption assetBuildinFileCopyOption; // 首包 copy 选项
    public string assetBuildinFileCopyParams;       // copy 选项参数
    public HybridBuildOption hybridBuildOption;     // 混合构建选项
}
```

### HybridRuntimeSettings 配置

```csharp
public class HybridRuntimeSettings : ScriptableObject
{
    public string HostServerIP;
    public int ReleaseBuildVersion;
    public string Packages;
}
```

---

## 构建流程

HybridCLR + YooAsset + UniTask 的构建流程分为两个主要阶段：**主包构建阶段**和**热更新包构建阶段**。通过分离式设计，实现高效的增量更新机制。

### 构建流程图

```
主包构建阶段（低频，首次或重大更新时）
├── 编译 AOT 程序集
├── 生成桥接函数
├── 生成裁剪后的 AOT DLL
├── 生成 AOT 补充元数据
└── 构建最终 APK 包

热更新包构建阶段（高频，日常更新）
├── 编译热更新程序集
├── 打包热更新 DLL
├── 打包资源文件
└── 生成版本信息
```

### 阶段一：主包构建

**适用场景**：首次发布、AOT 代码变更、桥接函数变化

1. **环境准备**
   - 执行 `HybridCLR-Installer` 安装 HybridCLR 环境
   - 执行 `Generate-All` 生成桥接函数和初始化文件

2. **AOT 元数据生成**
   ```csharp
   // 自动执行的流程
   Il2CppDefGeneratorCommand.GenerateIl2CppDef();
   LinkGeneratorCommand.GenerateLinkXml();
   StripAOTDllCommand.GenerateStripedAOTDlls();
   ```

3. **APK 构建**
   - 构建包含 AOT 代码的 APK 包
   - 生成裁剪后的 AOT DLL 用于后续热更新

### 阶段二：热更新包构建

**适用场景**：热更新代码变更、资源文件更新

1. **热更新 DLL 编译**
   ```csharp
   CompileDllCommand.CompileDllActiveBuildTarget();
   ```

2. **资源包构建**
   - 将热更新 DLL 作为 RawFile 打包
   - 打包美术资源、配置文件等
   - 生成版本控制信息

3. **增量打包优化**
   - 利用 YooAsset 的增量打包机制
   - 仅重新构建变更的资源包，避免全量构建
   - `Clear Build Cache` 选项控制是否清理构建缓存

### 构建决策机制

通过 `BuildHelper.CheckAccessMissingMetadata()` 方法判断：

- **需要重新构建 APK 的情况**：
  - 热更新代码引用了被裁剪的类型
  - 桥接函数发生变化
  - AOT 代码有重大变更

- **仅需更新热更新包的情况**：
  - 仅修改热更新逻辑代码
  - 更新资源文件
  - 修复热更新层 bug

#### 桥接函数稳定性说明

根据桥接函数的原理，对于固定的 AOT 部分，桥接函数集是确定的。后续无论进行任何热更新，都不会需要新的额外桥接函数。**因此不用担心热更上线后突然出现桥接函数缺失的问题。**

---

## 编辑器菜单

### Package 菜单（`HybridTool/`）

- `Check AOT Metadata` — 验证 AOT 元数据是否需要补充
- `Build APK` — 构建 APK 包
- `Get Patched AOT Assembly List` — 获取需要补充的 AOT 程序集列表
- `Generate AOT DLLs and Copy` — 生成 AOT DLL 并拷贝到资源目录
- `Generate Hot-Update DLLs and Copy` — 编译热更新 DLL 并拷贝到资源目录
- `Supplement Prefab Dependencies` — 补全预制体依赖到 link.xml
- `Hybrid Builder` — 打开 UI Toolkit 一体化构建窗口

### Sample 菜单（`HybridTool/Sample-HotUpdateSample/`）

- `Export HybridCLR Settings Snapshot` — 导出当前 HybridCLR 配置快照
- `Restore HybridCLR Settings from Snapshot` — 从快照恢复 HybridCLR 配置
- `Normalize Collector Paths` — 规范化 YooAsset 收集器路径

---

## 项目结构

```text
.
├── package.json                # UPM 包定义（依赖、示例）
├── CHANGELOG.md                # 变更日志
├── README.md / README_EN.md    # 双语文档（中文 / 英文）
├── LICENSE                     # MIT
├── README/                     # 文档附件（.png、.xmind、.pdf、.docx）
│
├── Editor/                     # 编辑器程序集：com.yanglingyun.hyu.Editor
│   ├── HybridEditor.asmdef     # 编辑器 asmdef
│   ├── BuildHelper.cs          # AOT 元数据检查、DLL 拷贝、APK 构建、link.xml 补全
│   ├── HybridBuilderWindow.cs  # UI Toolkit 打包窗口主控制器
│   ├── HybridBuilderWindow.uxml # 窗口 UI 布局
│   ├── HybridBuilderSettings.cs # 构建配置 ScriptableObject + HybridBuildOption 枚举
│   ├── HybridBuildPipeViewerBase.cs  # 构建管线查看器基类
│   ├── HybridBuildPipeViewerBase.uxml # 查看器 UI 布局
│   ├── HybridScriptableBuildPipelineViewer.cs # SBP 构建管线查看器
│   ├── SceneHelper.cs          # 场景工具
│   ├── BuildPipelineTask/      # 重写的打包流水线 Task
│   │   └── TaskBuildScript_SBP.cs  # SBP 自定义构建任务（脚本打包）
│   └── ScriptableBuildPipeline/ # 重写的打包流水线
│       ├── HybridScriptableBuildPipeline.cs     # SBP 管线实现
│       └── HybridScriptableBuildParameters.cs   # SBP 构建参数
│
├── Runtime/                    # 运行时程序集：com.yanglingyun.hyu.Runtime
│   ├── com.yanglingyun.hyu.Runtime.asmdef
│   └── HybridRuntimeSettings.cs # 运行时配置（CDN 地址、版本号、包名）
│
└── Samples~/                   # 可导入的示例（UPM 约定，不参与编译）
    ├── HotUpdateSample/        # 完整热更新示例
    │   ├── AOTScripts/         # AOT 运行时脚本（AOTPublic.asmdef）
    │   ├── Editor/             # 示例编辑器工具（快照导入、路径规范化）
    │   ├── EventDefine/        # UniEvent 事件定义（Battle/Patch/Scene/User）
    │   ├── HotUpdateAssets/    # 待打包资源（Prefabs/Scenes/Textures 等）
    │   ├── HotUpdateScripts/   # 热更新程序集（HotUpdate.asmdef）
    │   ├── PatchLogic/         # YooAsset 补丁下载状态机（8 个 FSM 节点）
    │   ├── Resources/          # 内置资源（PatchWindow 预制体等）
    │   ├── Scripts/            # 主场景 AOT 脚本（GameManager、HybridLauncher）
    │   ├── Settings/           # 预配置 ScriptableObject 资产
    │   └── ThirdParty/         # 轻量依赖（UniEvent/UniMachine/UniUtility）
    └── BuildTests/             # 构建管线测试
        └── Editor/
            ├── com.yanglingyun.hyu.Tests.Editor.asmdef
            └── HybridBuildPipelineTests.cs  # NUnit EditMode 测试
```

---

## Sample 使用说明

本包提供两个可导入的 Sample：**HotUpdateSample**（热更新示例）和 **BuildTests**（构建测试）。

### HotUpdateSample — 热更新完整示例

一个开箱即用的热更新演示工程，包含从资源下载到热更新代码执行的完整流程。

#### 目录结构

```text
HotUpdateSample/
├── AOTScripts/                # AOT 端运行时脚本（随主包发布，不可热更新）
│   ├── AOTPublic.asmdef
│   ├── HttpHelper.cs          # HTTP 工具类
│   └── SampleBundleEncryption.cs  # YooAsset 资源包加密示例
├── Editor/                    # 编辑器导入工具
│   ├── HybridCLRSettingsSnapshot.json  # HybridCLR 预置配置快照
│   └── HybridSettingsImporter.cs       # 自动/手动设置导入器
├── EventDefine/               # UniEvent 事件定义
│   ├── BattleEventDefine.cs
│   ├── PatchEventDefine.cs    # 热更新流程事件
│   ├── SceneEventDefine.cs
│   └── UserEventDefine.cs
├── HotUpdateAssets/           # 需要打入 AssetBundle 的资源
│   ├── HotUpdateDll/          # 编译后的热更新 DLL 存放目录
│   ├── PatchedAOTDLL/         # AOT 补充元数据 DLL（.bytes 格式）
│   ├── Prefabs/               # 预制体
│   ├── Scenes/                # 热更新场景
│   ├── Textures/ Materials/ UIPrefabs/ audios/
├── HotUpdateScripts/          # 热更新程序集（运行时由 HybridCLR 加载）
│   ├── HotUpdate.asmdef
│   ├── HotUpdateLauncher.cs   # 热更新入口脚本
│   ├── LoadImage.cs           # YooAsset 加载贴图示例
│   ├── ModelRotate.cs         # YooAsset 加载模型示例
│   └── animate/Rotating.cs    # 旋转动画组件
├── PatchLogic/                # YooAsset 热更新下载状态机
│   ├── FsmNode/               # 8 个 FSM 状态节点
│   ├── PatchOperation.cs      # 状态机调度器
│   └── PatchWindow.cs         # 下载进度 UI 控制器
├── Scripts/                   # 主场景 AOT 脚本
│   ├── GameManager.cs         # 游戏启动管理器
│   └── HybridLauncher.cs     # HybridCLR + YooAsset 启动器
├── Settings/                  # 配置文件
│   ├── AssetBundleCollectorSetting.asset
│   ├── HybridBuilderSettings.asset
│   └── HybridRuntimeSettings.asset
└── ThirdParty/                # 内置轻量工具库
    ├── UniEvent/              # 事件总线
    ├── UniMachine/            # 有限状态机
    └── UniUtility/            # 通用工具
```

#### 使用步骤

**第一步：导入示例**

在 Package Manager 中找到 `com.yanglingyun.hyu`，点击 **Samples** 标签，导入 **Hot Update Sample**。

**第二步：自动初始化**

导入后首次打开编辑器时，`HybridSettingsImporter` 会自动检测 HybridCLR 配置状态：
- 若 HybridCLR Settings 中热更新程序集列表为空，弹窗询问是否从快照恢复
- 点击 **Restore from Snapshot** 将自动配置：
  - `hotUpdateAssemblyDefinitions` → `[HotUpdate]`
  - `patchAOTAssemblies` → `[UniTask, UnityEngine.CoreModule, YooAsset, mscorlib]`
- 同时自动创建 Settings 资产并规范化收集器路径

**第三步：手动初始化（可选）**

如果自动初始化未触发，可手动执行菜单：

1. `HybridTool/Sample-HotUpdateSample/Restore HybridCLR Settings from Snapshot`
2. `HybridTool/Sample-HotUpdateSample/Normalize Collector Paths`

**第四步：配置并构建**

1. 在 `HybridRuntimeSettings` 中填写 `HostServerIP`
2. 通过 `HybridTool/` 菜单执行构建

#### 运行时流程

```text
HybridLauncher → GameManager → PatchOperation（8 步状态机）
    → 初始化 YooAsset 包
    → 请求远端版本号
    → 更新资源清单
    → 下载资源包
    → 加载 AOT 元数据（为热更新泛型函数提供支持）
    → 加载热更新 DLL（HybridCLR）
    → 实例化 HotUpdateLauncher，进入热更新逻辑
```

### BuildTests — 构建管线测试

用于验证构建配置与管线正确性的 NUnit EditMode 测试集。

#### 使用步骤

1. 在 Package Manager 中导入 **Build Pipeline Tests** 示例
2. 打开 Unity Test Runner（`Window > General > Test Runner`）
3. 测试程序集 `com.yanglingyun.hyu.Tests.Editor` 仅在 `UNITY_INCLUDE_TESTS` 定义时编译

#### 测试覆盖范围

| 测试类别 | 说明 |
|---|---|
| **BuildConfig** | HybridCLR 配置存在性、热更新/AOT 程序集列表、构建场景列表、工程路径合法性 |
| **BuilderSettings** | `HybridBuilderSettings` 资产存在性、`RuntimeSettings` 关联、构建输出路径解析、版本号格式 |
| **RuntimeSettings** | `HybridRuntimeSettings` 资产存在性、`HostServerIP` 配置 |
| **Platform Tests** | 平台参数化测试（Windows / Android / iOS）：DLL 输出路径、AOT 裁剪路径、跨平台路径唯一性 |
| **FirstBuildPrerequisites** | 首次构建前置验证：AOT 裁剪目录、`GenerateAll` 完整性、`MetadataCheck` 通过性 |
| **VersionLogic** | 版本自增正确性、`GetCurrentVersion` 构建/展示双格式、版本递增后输出路径同步变化 |
| **PipelineTypeValidation** | `HybridScriptableBuildPipeline` 传入非法参数类型时抛出异常 |
| **CopyDllEdgeCases** | `CopyPatchedAOTDll` / `CopyHotUpdateDll` 空路径防御、`CopyDllFileToByte` 源目录不存在时返回空列表 |

> 标记 `[Category("SlowTest")]` 的测试会实际执行构建命令，耗时较长；当活跃平台不匹配时会自动跳过。

#### 测试边界说明

测试仅验证本包自身功能（环境配置、Editor 方法、版本逻辑、DLL 编译拷贝等）的正确性。YooAsset 资源打包（`ScriptableBuildPipeline.Run()`）、APK 构建（`BuildHelper.BuildAPK()`）等属于第三方框架或 Unity 构建管线的职责，由各自的测试保证，不在本测试范围内。

---

## 常见问题

### Q1: 热更新代码无法访问 AOT 代码中的泛型方法怎么办？

这是因为泛型方法需要额外的元数据支持。解决方案：

1. **显式调用** — 在热更新代码中显式调用该泛型方法
2. **手动配置** — 在 link.xml 中添加相关类型的保留设置
3. **工具辅助** — 使用 `HybridTool/Supplement Prefab Dependencies` 功能

### Q2: 打包时提示"缺少 AOT 元数据"错误怎么办？

1. 使用 `HybridTool/Check AOT Metadata` 验证元数据是否需要补充
2. 执行 `HybridTool/Generate AOT DLLs and Copy` 生成 AOT 补充文件
3. 重新构建 APK

### Q3: 热更新代码运行时出现"方法未找到"错误怎么办？

可能原因及解决方案：
- **版本不匹配** — 确保热更新 DLL 和 AOT 元数据版本一致
- **配置问题** — 检查 link.xml 配置是否正确
- **重新构建** — 重新构建 APK 以更新元数据

### Q4: 首次构建时 MetadataCheck 失败怎么办？

先执行 Sample 菜单中的 Snapshot 恢复和 Collector 规范化，然后触发 `GenerateAll` 前置链，再进行热更新构建。

### Q5: Sample 导入后 Collector 路径不对？

执行：`HybridTool/Sample-HotUpdateSample/Normalize Collector Paths`

---

## 最佳实践

### 程序集划分建议

#### AOT 程序集（稳定不变）
- 核心业务逻辑
- 第三方库封装
- Unity API 抽象层
- 接口定义与数据结构
- 事件系统

#### 热更新程序集（频繁更新）
- 游戏玩法逻辑
- UI 界面实现
- 配置数据解析

### 构建优化建议

- 利用 YooAsset 增量打包机制，不勾选 `Clear Build Cache` 可大幅提升构建速度
- AOT 部分稳定后，日常迭代仅需执行热更新包构建
- 桥接函数对于固定 AOT 部分是确定的，热更不会引入新的桥接函数需求

---

## License

MIT

---

<div align="center">

*如有问题，请提交 [Issue](https://github.com/YangLingCloud/HybridCLR_YooAsset_UniTask/issues)*

*Happy Coding!*

</div>
