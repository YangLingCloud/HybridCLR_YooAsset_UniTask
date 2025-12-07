# HybridCLR + YooAsset + UniTask 集成解决方案

<div align="center">

[![Unity 2022.3](https://img.shields.io/badge/Unity-2022.3-brightgreen)](https://unity.com/) [![HybridCLR](https://img.shields.io/badge/HybridCLR-v8.2.0-blue)](https://github.com/focus-creative-games/hybridclr) [![YooAsset](https://img.shields.io/badge/YooAsset-v2.3.9-orange)](https://github.com/tuyoogame/YooAsset) [![UniTask](https://img.shields.io/badge/UniTask-v2.5.10-purple)](https://github.com/Cysharp/UniTask) [![License](https://img.shields.io/badge/License-MIT-green)](LICENSE) [![English](https://img.shields.io/badge/English-Document-blue)](https://github.com/YangLingCloud/HybridCLR_YooAsset_UniTask/blob/main/README_EN.md)

**专业级 Unity 热更新与资源管理一体化解决方案**

*企业级热更新框架 · 高性能资源管理 · 现代化异步编程*

</div>

---

## 目录导航

- [项目简介](#项目简介)
- [核心概念](#核心概念)
- [环境准备](#环境准备)
- [快速开始](#快速开始)
- [构建流程](#构建流程)
- [项目结构](#项目结构)
- [常见问题](#常见问题)
- [最佳实践](#最佳实践)

---

## 项目简介

<div align="center">

**HybridCLR + YooAsset + UniTask 集成解决方案** 是一个专为 Unity 开发者设计的高性能热更新与资源管理框架。通过将三个业界领先的框架完美整合，为您的项目提供企业级的热更新能力。

**基于 Unity 2022.3.62f2c1、HybridCLR 8.2.0、YooAsset 2.3.9、UniTask 2.5.10 版本进行整合**

</div>

### 核心特性

- **完整的热更新能力** - 基于 HybridCLR 的完整 C# 热更新解决方案
- **专业的资源管理** - 使用 YooAsset 实现高效的资源打包与加载
- **高性能异步编程** - 借助 UniTask 提供卓越的异步操作性能
- **一体化工具链** - 完整的编辑器集成与自动化构建流程

### 框架优势对比

| 框架组件 | 功能描述 | 核心优势 |
|---------|---------|---------|
| **HybridCLR** | 完整的 C# 热更新解决方案 | 支持 IL2CPP 环境下的动态代码执行 |
| **YooAsset** | 专业的资源管理系统 | 高效的 AssetBundle 管理与加载 |
| **UniTask** | 高性能异步编程框架 | 零分配异步操作，提升性能 |

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


---

## 环境准备

### 系统要求

- **Unity 版本**: 2022.3 LTS 或更高
- **目标平台**: Android、iOS、Windows 及其他主流平台
- **开发环境**: Visual Studio 2019+ 或 Rider
- **推荐配置**: 8GB+ 内存，SSD 存储

### 安装步骤

1. **安装 Unity 2022.3 LTS**
   - 从 Unity Hub 下载并安装最新的 LTS 版本
   - 确保包含 Android/iOS 构建支持

2. **通过 Package Manager 安装以下包**:
   - HybridCLR - 热更新核心框架
   - YooAsset - 资源管理系统
   - UniTask - 高性能异步编程

3. **配置 HybridCLR 环境**
   - 设置 HybridCLR 运行时环境
   - 配置热更新程序集路径

---

## 快速开始

### 初始配置

#### 1. 创建热更新程序集

在项目中创建 Assembly Definition 文件用于热更新代码：

```csharp
// HotUpdate.asmdef
{
    "name": "HotUpdate",
    "references": ["AOTPublic"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 2. 配置 HybridCLR 设置

在 HybridCLR Settings 中配置热更新程序集：

```csharp
// 在 HybridCLR Settings 中添加热更新程序集
HotUpdateAssemblies = new List<string> { "HotUpdate" }
```

#### 3. 设置 YooAsset 资源收集规则

配置 AssetBundle 收集规则，确保热更新资源正确打包。

### 基本使用

本部分介绍如何使用HybridBuilderWindow进行资源打包和HybridLauncher进行运行时加载。

#### 1. 使用HybridBuilderWindow打包

HybridBuilderWindow是编辑器工具，用于配置和触发资源打包流程。打包包括资产包和脚本包。

##### 打开HybridBuilderWindow

在Unity编辑器中，点击菜单栏：`HybridTool/Hybrid Builder` 打开窗口。

##### 配置打包设置

1. 选择HybridBuilderSetting：窗口中会列出项目中所有的HybridBuilderSetting文件，选择要使用的配置。
2. 选择HybridRuntimeSetting：选择运行时设置文件，该文件定义了资源包和版本信息。
3. 选择打包选项：可以选择打包资产、脚本或全部。

##### 执行打包

点击"构建"按钮开始打包。打包过程会自动处理以下步骤：
- 验证元数据补充需求
- 编译热更新DLL
- 生成AOT元数据
- 打包AssetBundle资源

打包完成后，资源包将输出到配置的目录中，准备部署。

#### 2. 使用HybridLauncher加载

HybridLauncher是运行时组件，负责初始化资源系统并加载热更新内容。

##### 初始化HybridLauncher

在场景中创建一个GameObject并附加HybridLauncher脚本。设置以下参数：
- `PlayMode`: 资源系统运行模式（如编辑器模拟模式、主机模式等）
- `RuntimeSettingsPath`: （可选）远程RuntimeSettings文件的URL，用于动态加载配置

##### 加载流程

HybridLauncher在启动时自动执行以下步骤：

1. **加载HybridRuntimeSettings**：如果PlayMode是主机模式，将从RuntimeSettingsPath加载运行时设置；否则使用本地配置。
2. **初始化YooAsset资源系统**：根据设置初始化资源包。
3. **加载AOT元数据**：从脚本包中加载AOT DLL的元数据，为热更新泛型函数提供支持。
4. **加载热更新程序集**：从脚本包中加载热更新DLL，并反射加载到应用程序域中。

##### 代码示例

```csharp
// HybridLauncher会自动处理加载流程，无需额外代码。
// 确保HybridLauncher组件在场景中，并正确配置参数。
```

##### 注意事项

- 在编辑器模式下，可以使用模拟模式加速开发。
- 在生产环境中，确保RuntimeSettingsPath指向正确的远程配置。
- 热更新DLL和资源包需要先通过HybridBuilderWindow打包并部署到服务器。

这样，您就完成了从打包到加载的完整流程。

---

## 集成工具

### HybridTool 整合工具

由于 YooAsset 和 HybridCLR 都是通过 Unity PackageManager 加载的，导致很多代码不够好用又无法修改，因此通过编辑器代码写了一套整合工具，使两个第三方库可以更好地配合工作。

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

- **HybridBuilderWindow** - 窗口主控制器
- **HybridBuilderWindow.uxml** - UI 布局定义文件  
- **HybridBuildPipeViewerBase** - 核心功能实现基类

### HybridScriptableBuildPipeline

主要打包逻辑在 HybridScriptableBuildPipelineViewer 中实现，仅在运行时对打包 Asset 或 Script 进行区分。

#### 对 YooAsset 打包流程的修改

1. **运行时区分打包类型** - Asset 或 Script 使用不同的构建管道
2. **增强 RawFileBuildPipeline** - 增加 TaskBuildScript_SBP 流程
3. **批量打包支持** - 通过包名列表配置一次性打多个包
4. **APK 打包优化** - 优化构建流程与错误检查
5. **裁剪检查** - 构建前检查热更新代码是否访问了被裁切代码

### HybridBuilderSettings 配置

```csharp
// 构建配置示例
public class HybridBuilderSettings : ScriptableObject
{
    public HybridRuntimeSettings RuntimeSettings;
    public List<string> AssetPackages = new List<string>();
    public string ScriptPackageName;
    public DefaultAsset PatchedAOTDLLFolder;
    public DefaultAsset HotUpdateDLLFolder;
    public int ReleaseBuildVersion;
    public int AssetBuildVersion;
    public int ScriptBuildVersion;
    public bool isClearBuildCache;
}
```

### HybridRuntimeSettings 配置

```csharp
// 运行时配置示例
public class HybridRuntimeSettings : ScriptableObject
{
    public string HostServerIP;
    public int ReleaseBuildVersion;
    public string Packages;
}
```

### 构建流程概览

HybridCLR + YooAsset + UniTask 的构建流程分为两个主要阶段：**APK 构建阶段**和**热更新包构建阶段**。通过这种分离式设计，实现了高效的增量更新机制。

#### 构建流程图

```
APK 构建阶段 (稳定不变)
├── 编译 AOT 程序集
├── 生成桥接函数
├── 生成裁剪后的 AOT DLL
├── 生成 AOT 补充元数据
└── 构建最终 APK 包

热更新包构建阶段 (频繁更新)
├── 编译热更新程序集
├── 打包热更新 DLL
├── 打包资源文件
└── 生成版本信息
```

### 详细构建步骤

#### 阶段一：APK 构建 (首次或重大更新时)

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

#### 阶段二：热更新包构建 (日常更新)

**适用场景**：热更新代码变更、资源文件更新

1. **热更新 DLL 编译**
   ```csharp
   // 编译热更新代码
   CompileDllCommand.CompileDllActiveBuildTarget();
   ```

2. **资源包构建**
   - 将热更新 DLL 作为 RawFile 打包
   - 打包美术资源、配置文件等
   - 生成版本控制信息

3. **增量打包优化**
   - 利用 YooAsset 的增量打包机制
   - 仅更新变更的文件，提升构建速度

### 构建决策机制

#### 何时需要重新构建 APK？

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

### 初次运行工程步骤

1. **环境初始化**
   - 执行 `HybridCLR-Installer` 安装环境
   - 执行 `Generate-All` 生成必要文件

2. **资源配置**
   - 在 `YooAsset-AssetBundleCollector` 中配置资源与代码包
   - 创建 `HybridBuilderSettings` 与 `HybridRuntimeSettings` ScriptableObject

3. **场景配置**
   - 在 StartScene 的 Boot 物体上配置 YooAsset 运行模式
   - 如使用 `HostPlayMode`，添加 `HybridRuntimeSettings` 引用

4. **首次构建**
   - 通过 `顶部菜单栏-HybridTool-HybridBuilder` 进行打包配置
   - 执行完整的 APK 构建流程

### 增量打包优化

YooAsset 提供了高效的增量打包机制：

- **Clear Build Cache** 选项控制是否清理构建缓存
- 不勾选此项时，引擎开启增量打包模式，极大提高构建速度
- 仅重新构建变更的资源包，避免全量构建

---

## 项目结构

```
Project/
├── Assets/
│   ├── AOTScripts/           # 同时用于AOT和HotUpdateDLL使用的类
│   ├── Editor/               # 编辑器代码
│   │   ├── BuildPipelineTask/ # 重写后的打包流水线Task类
│   │   └── ScriptableBuildPipeline/ # 重写后的打包流水线
│   ├── HotUpdateAssets/      # 用于打成AB包的所有美术和代码资产
│   └── HotUpdateScripts/     # 使用AssemblyDefinition划分的热更新代码
├── HybridCLRData/           # HybridCLR生成的文件夹
│   ├── AssembliesPostIl2CppStrip/ # 打包后自动从Library拷贝出来的AOTDLL
│   └── HotUpdateDlls/       # HybridCLR生成的热更新DLL
├── Bundles/                 # YooAsset默认打包路径
├── README.md
└── .gitignore
```

---

## 常见问题

### Q: 热更新代码无法访问 AOT 代码中的泛型方法怎么办？

**A:** 这是因为泛型方法需要额外的元数据支持。解决方案：

1. **显式调用** - 在热更新代码中显式调用该泛型方法
2. **手动配置** - 在 link.xml 中添加相关类型的保留设置
3. **工具辅助** - 使用整合工具的"补全热更新预制体依赖"功能

### Q: 打包时提示"缺少 AOT 元数据"错误怎么办？

**A:** 解决步骤：

1. **验证需求** - 使用 HybridTool 中的"验证元数据是否需要补充"功能
2. **生成文件** - 执行"生成 AOT 补充文件并复制进文件夹"操作
3. **重新构建** - 重新构建 APK

### Q: 热更新代码运行时出现"方法未找到"错误怎么办？

**A:** 可能原因及解决方案：

- **版本不匹配** - 确保热更新 DLL 和 AOT 元数据版本一致
- **配置问题** - 检查 link.xml 配置是否正确
- **重新构建** - 重新构建 APK 以更新元数据

---

## 最佳实践

### 程序集划分建议

#### **AOT 程序集** (稳定不变)
- 核心业务逻辑
- 第三方库封装
- Unity API 抽象层
- 接口定义
- 数据结构
- 事件系统

#### **热更新程序集** (频繁更新)
- 游戏玩法逻辑
- UI 界面实现  
- 配置数据解析

---

<div align="center">

## 开始使用

现在您已经了解了 HybridCLR + YooAsset + UniTask 集成解决方案的全部特性！

**立即开始构建您的下一代热更新游戏吧！**

---

*该文档基于AI生成*

*如有问题，请参考上一代文档或提交 Issue*  

*Happy Coding!*

</div>