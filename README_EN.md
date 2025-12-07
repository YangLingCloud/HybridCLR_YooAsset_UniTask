# HybridCLR + YooAsset + UniTask Integrated Solution

<div align="center">

[![Unity 2022.3](https://img.shields.io/badge/Unity-2022.3-brightgreen)](https://unity.com/) [![HybridCLR](https://img.shields.io/badge/HybridCLR-v8.2.0-blue)](https://github.com/focus-creative-games/hybridclr) [![YooAsset](https://img.shields.io/badge/YooAsset-v2.3.9-orange)](https://github.com/tuyoogame/YooAsset) [![UniTask](https://img.shields.io/badge/UniTask-v2.5.10-purple)](https://github.com/Cysharp/UniTask) [![License](https://img.shields.io/badge/License-MIT-green)](LICENSE) [![中文](https://img.shields.io/badge/中文-文档-red)](https://github.com/YangLingCloud/HybridCLR_YooAsset_UniTask/blob/main/README.md)

**Professional Unity Hot Update and Resource Management Integrated Solution**

*Enterprise-grade Hot Update Framework · High-performance Resource Management · Modern Asynchronous Programming*

</div>

---

## Table of Contents

- [Project Introduction](#project-introduction)
- [Core Concepts](#core-concepts)
- [Environment Setup](#environment-setup)
- [Quick Start](#quick-start)
- [Integration Tools](#integration-tools)
- [Build Process](#build-process)
- [Project Structure](#project-structure)
- [FAQ](#faq)
- [Best Practices](#best-practices)

---

## Project Introduction

<div align="center">

**HybridCLR + YooAsset + UniTask Integrated Solution** is a high-performance hot update and resource management framework specifically designed for Unity developers. By perfectly integrating three industry-leading frameworks, it provides enterprise-level hot update capabilities for your projects.

**Integrated based on Unity 2022.3.62f2c1, HybridCLR 8.2.0, YooAsset 2.3.9, UniTask 2.5.10 versions**

</div>

### Core Features

- **Complete Hot Update Capability** - Full C# hot update solution based on HybridCLR
- **Professional Resource Management** - Efficient resource packaging and loading using YooAsset
- **High-performance Asynchronous Programming** - Excellent asynchronous operation performance with UniTask
- **Integrated Toolchain** - Complete editor integration and automated build process

### Framework Advantages Comparison

| Framework Component | Function Description | Core Advantage |
|--------------------|---------------------|----------------|
| **HybridCLR** | Complete C# hot update solution | Supports dynamic code execution in IL2CPP environment |
| **YooAsset** | Professional resource management system | Efficient AssetBundle management and loading |
| **UniTask** | High-performance asynchronous programming framework | Zero-allocation async operations, performance improvement |

---

## Core Concepts

### Assembly-CSharp.dll

`Assembly-CSharp` is the DLL automatically integrated by Unity. Any code in the Unity project that is not separately compiled will be integrated into this `Assembly-CSharp.dll`.

### Assembly Definition

`Assembly Definition` is a feature introduced after Unity 2017.3, mainly addressing the compilation efficiency issues of large assemblies.

Creating an `Assembly Definition` in any folder under the Assets directory will cause all code in that folder to be compiled into a separate DLL. When modifying code in that folder, only that DLL will be recompiled, not the `Assembly-CSharp.dll`.

### AOT and Hot Update Assemblies

#### Hot Update Assemblies

Hot update assemblies can theoretically be the `Assembly-CSharp` assembly, but to ensure clear project logic and convenient resource management, the current framework uses `AssemblyDefinition` to divide separate DLLs as hot update assemblies. Hot update assemblies should not be processed by IL2CPP and compiled into the final package.

HybridCLR handles the `IFilterBuildAssemblies` callback, removing hot update DLLs from the `build assemblies` list.

#### AOT Assemblies

AOT assemblies are code that is packaged with the build and will not be updated. Under the current framework definition, `Assembly-CSharp` is the main AOT assembly, and `AssemblyDefinition` is used to divide other AOT assemblies.

When using `Assembly-CSharp` as an AOT assembly, it is strongly recommended to disable the `auto reference` option for hot update assemblies. Because `Assembly-CSharp` is the top-level assembly, it automatically references all remaining assemblies, making it easy to accidentally reference hot update assemblies.

### UniTask

UniTask is an open-source library on GitHub that provides high-performance asynchronous solutions for Unity. It can replace coroutines to implement asynchronous operations, while being compatible with Unity's lifecycle, allowing methods like Awake, Start, and coroutines to execute asynchronously, but still running on the main thread.

### Hot Update DLL Loading

HybridCLR officially recommends directly attaching code to prefabs and loading hot updates through AssetBundle prefab loading. Alternatively, hot update classes can be reflected directly from loaded hot update DLLs and attached to objects using the AddComponent method to achieve hot updates. Regardless of the method, hot update DLLs need to be loaded in advance before loading prefabs or classes.

---

## Environment Setup

### System Requirements

- **Unity Version**: 2022.3 LTS or higher
- **Target Platforms**: Android, iOS, Windows, and other mainstream platforms
- **Development Environment**: Visual Studio 2019+ or Rider
- **Recommended Configuration**: 8GB+ RAM, SSD storage

### Installation Steps

1. **Install Unity 2022.3 LTS**
   - Download and install the latest LTS version from Unity Hub
   - Ensure Android/iOS build support is included

2. **Install the following packages via Package Manager**:
   - HybridCLR - Hot update core framework
   - YooAsset - Resource management system
   - UniTask - High-performance asynchronous programming

3. **Configure HybridCLR Environment**
   - Set up HybridCLR runtime environment
   - Configure hot update assembly paths

---

## Quick Start

### Initial Configuration

#### 1. Create Hot Update Assembly

Create an Assembly Definition file in the project for hot update code:

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

#### 2. Configure HybridCLR Settings

Configure hot update assemblies in HybridCLR Settings:

```csharp
// Add hot update assemblies in HybridCLR Settings
HotUpdateAssemblies = new List<string> { "HotUpdate" }
```

#### 3. Set YooAsset Resource Collection Rules

Configure AssetBundle collection rules to ensure hot update resources are correctly packaged.

### Basic Usage

#### Initialize Framework

```csharp
using UnityEngine;
using System.Threading.Tasks;

public class GameLauncher : MonoBehaviour
{
    private async void Start()
    {
        // Initialize HybridCLR runtime
        await HybridCLRHelper.InitializeAsync();
        
        // Load hot update assembly
        var assembly = await HybridCLRLauncher.LoadHotUpdateAssemblyAsync("HotUpdateScripts.dll");
        
        // Use UniTask for asynchronous operations
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        
        Debug.Log("Framework initialization completed!");
    }
}
```

#### Hot Update Code Example

```csharp
// Hot update assembly code example
public class HotUpdateLogic
{
    public static async UniTask<int> CalculateDamageAsync(int baseDamage, float multiplier)
    {
        // Asynchronously calculate damage
        await UniTask.DelayFrame(1);
        return (int)(baseDamage * multiplier);
    }
    
    public static void ShowWelcomeMessage()
    {
        Debug.Log("Welcome to hot update functionality!");
    }
}
```

---

## Integration Tools

### HybridTool Integration Tool

Since both YooAsset and HybridCLR are loaded through Unity PackageManager, many codes are not user-friendly and cannot be modified. Therefore, an integration tool was written through editor code to make the two third-party libraries work better together.

#### Main Functions

| Function Module | Function Description | Usage Scenario |
|----------------|---------------------|----------------|
| **Verify Metadata Supplement Requirements** | Compare AOT and Hot Update DLLs | Pre-build check |
| **APK Build Process** | Automated packaging and dependency analysis | Complete build process |
| **AOT Metadata Generation** | Automatically generate supplementary files | Solve trimming issues |
| **Hot Update DLL Compilation** | Compile and generate hot update code | Development phase |
| **Prefab Dependency Completion** | Automatically complete link.xml | Solve component reference issues |

### HybridBuilderWindow

Modern packaging tool window based on **UI Toolkit**, rewritten YooAsset.AssetBundleBuilderWindow, and added code packaging related settings.

#### Core Components

- **HybridBuilderWindow** - Main window controller
- **HybridBuilderWindow.uxml** - UI layout definition file  
- **HybridBuildPipeViewerBase** - Core functionality implementation base class

### HybridScriptableBuildPipeline

The main packaging logic is implemented in HybridScriptableBuildPipelineViewer, only distinguishing between packaging Assets or Scripts at runtime.

#### Modifications to YooAsset Packaging Process

1. **Runtime Packaging Type Distinction** - Use different build pipelines for Assets or Scripts
2. **Enhanced RawFileBuildPipeline** - Added TaskBuildScript_SBP process
3. **Batch Packaging Support** - Configure multiple packages at once through package name list
4. **APK Packaging Optimization** - Optimized build process and error checking
5. **Trimming Check** - Check if hot update code accesses trimmed code before building

---

## Build Process

### Build Process Overview

The build process for HybridCLR + YooAsset + UniTask is divided into two main stages: **APK Build Stage** and **Hot Update Package Build Stage**. Through this separation design, efficient incremental update mechanism is achieved.

#### Build Process Diagram

```
APK Build Stage (Stable and Unchanging)
├── Compile AOT Assemblies
├── Generate Bridge Functions
├── Generate Trimmed AOT DLLs
├── Generate AOT Supplementary Metadata
└── Build Final APK Package

Hot Update Package Build Stage (Frequent Updates)
├── Compile Hot Update Assemblies
├── Package Hot Update DLLs
├── Package Resource Files
└── Generate Version Information
```

### Detailed Build Steps

#### Stage One: APK Build (First Release or Major Updates)

**Applicable Scenarios**: First release, AOT code changes, bridge function changes

1. **Environment Preparation**
   - Execute `HybridCLR-Installer` to install HybridCLR environment
   - Execute `Generate-All` to generate bridge functions and initialization files

2. **AOT Metadata Generation**
   ```csharp
   // Automatically executed process
   Il2CppDefGeneratorCommand.GenerateIl2CppDef();
   LinkGeneratorCommand.GenerateLinkXml();
   StripAOTDllCommand.GenerateStripedAOTDlls();
   ```

3. **APK Build**
   - Build APK package containing AOT code
   - Generate trimmed AOT DLLs for subsequent hot updates

#### Stage Two: Hot Update Package Build (Daily Updates)

**Applicable Scenarios**: Hot update code changes, resource file updates

1. **Hot Update DLL Compilation**
   ```csharp
   // Compile hot update code
   CompileDllCommand.CompileDllActiveBuildTarget();
   ```

2. **Resource Package Build**
   - Package hot update DLLs as RawFile
   - Package art resources, configuration files, etc.
   - Generate version control information

3. **Incremental Packaging Optimization**
   - Utilize YooAsset's incremental packaging mechanism
   - Only update changed files, improving build speed

### Build Configuration Management

#### HybridBuilderSettings Configuration

```csharp
// Build configuration example
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

#### HybridRuntimeSettings Configuration

```csharp
// Runtime configuration example
public class HybridRuntimeSettings : ScriptableObject
{
    public string HostServerIP;
    public int ReleaseBuildVersion;
    public string Packages;
}
```

### Build Decision Mechanism

#### When to Rebuild APK?

Determine through `BuildHelper.CheckAccessMissingMetadata()` method:

- **Situations requiring APK rebuild**:
  - Hot update code references trimmed types
  - Bridge functions change
  - Major AOT code changes

- **Situations requiring only hot update package update**:
  - Only modify hot update logic code
  - Update resource files
  - Fix hot update layer bugs

#### Bridge Function Stability Explanation

According to the principle of bridge functions, for a fixed AOT part, the bridge function set is determined. Subsequent hot updates will not require new additional bridge functions. **Therefore, there's no need to worry about sudden bridge function missing issues after hot updates go live.**

### First Run Project Steps

1. **Environment Initialization**
   - Execute `HybridCLR-Installer` to install environment
   - Execute `Generate-All` to generate necessary files

2. **Resource Configuration**
   - Configure resource and code packages in `YooAsset-AssetBundleCollector`
   - Create `HybridBuilderSettings` and `HybridRuntimeSettings` ScriptableObject

3. **Scene Configuration**
   - Configure YooAsset running mode on Boot object in StartScene
   - If using `HostPlayMode`, add `HybridRuntimeSettings` reference

4. **First Build**
   - Configure packaging through `Top Menu Bar-HybridTool-HybridBuilder`
   - Execute complete APK build process

### Incremental Packaging Optimization

YooAsset provides efficient incremental packaging mechanism:

- **Clear Build Cache** option controls whether to clear build cache
- When this option is not checked, engine enables incremental packaging mode, greatly improving build speed
- Only rebuild changed resource packages, avoiding full build

---

## Project Structure

```
Project/
├── Assets/
│   ├── AOTScripts/           # Classes used by both AOT and HotUpdateDLL
│   ├── Editor/               # Editor code
│   │   ├── BuildPipelineTask/ # Rewritten build pipeline Task classes
│   │   └── ScriptableBuildPipeline/ # Rewritten build pipeline
│   ├── HotUpdateAssets/      # All art and code assets for AB packaging
│   └── HotUpdateScripts/     # Hot update code divided by AssemblyDefinition
├── HybridCLRData/           # HybridCLR generated folders
│   ├── AssembliesPostIl2CppStrip/ # AOTDLLs automatically copied from Library after build
│   └── HotUpdateDlls/       # HybridCLR generated hot update DLLs
├── Bundles/                 # YooAsset default packaging path
├── README.md
└── .gitignore
```

---

## FAQ

### Q: What to do when hot update code cannot access generic methods in AOT code?

**A:** This is because generic methods require additional metadata support. Solutions:

1. **Explicit Call** - Explicitly call the generic method in hot update code
2. **Manual Configuration** - Add related type retention settings in link.xml
3. **Tool Assistance** - Use integration tool's "Complete Hot Update Prefab Dependencies" function

### Q: What to do when "Missing AOT Metadata" error appears during packaging?

**A:** Solution steps:

1. **Verify Requirements** - Use "Verify Metadata Supplement Requirements" function in HybridTool
2. **Generate Files** - Execute "Generate AOT Supplementary Files and Copy to Folder" operation
3. **Rebuild** - Rebuild APK

### Q: What to do when "Method Not Found" error occurs during hot update code runtime?

**A:** Possible causes and solutions:

- **Version Mismatch** - Ensure hot update DLL and AOT metadata versions match
- **Configuration Issue** - Check if link.xml configuration is correct
- **Rebuild** - Rebuild APK to update metadata

---

## Best Practices

### Assembly Division Recommendations

#### **AOT Assemblies** (Stable and Unchanging)
- Core business logic
- Third-party library encapsulation
- Unity API abstraction layer
- Interface definitions
- Data structures
- Event system

#### **Hot Update Assemblies** (Frequent Updates)
- Gameplay logic
- UI interface implementation  
- Configuration data parsing

---

<div align="center">

## Getting Started

Now you have learned all the features of the HybridCLR + YooAsset + UniTask Integrated Solution!

**Start building your next-generation hot update game now!**

---

*This documentation is AI-generated*

*If you have questions, please refer to the previous documentation or submit an Issue*  

*Happy Coding!*

</div>