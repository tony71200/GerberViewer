# PLP (StitchingImage)
## Detail Version: 0.0.5: This is the version apply HalconDoNet library (Please install Halcon 25.05 from MVTech first use)
## English
### Overview
This repository contains a Windows Forms application for loading robot-captured images, generating stitch orders, matching tiles, and composing a stitched output image. It also includes tooling for robot order generation, matching algorithms, and configuration management.

### Repository Structure
```
PLP.sln
README.md
PLP_Handshake_Protocol_v0.2.md
StitchingImage/
  App.config
  Program.cs
  MainForm.cs / MainForm.Designer.cs
  ProcessDialog.cs / ProcessDialog.Designer.cs
  Connection/
  Stitch_Tools/
  Properties/
```

### UI Features (High Level)
- **Folder loading & parsing**: Load image folders and parse filenames into groups and tiles.
- **Order visualization**: Preview ordering and movement path.
- **Matching configuration**: Configure feature matching settings and preview offsets.
- **System configuration**: View/edit system parameters from a config grid; config file reload is supported.
- **Run stitching**: Execute matching + stitching and save output.
- **TCP/IP connection panel**: Connect/disconnect to host and view protocol logs.
- **Logs**: Runtime logs are written to `Log_YYYYMMDD.txt` in the working directory.

### Build & Run
**Requirements**
- Windows
- .NET Framework 4.8
- Visual Studio 2019+ (or MSBuild compatible with .NET Framework 4.8)
- NuGet packages restore (OpenCvSharp4)

**Build**
- Open `PLP.sln` in Visual Studio.
- Restore NuGet packages.
- Build the `StitchingImage` project.

**Run**
- Start the `StitchingImage` project (F5) or run the built `StitchingImage.exe` from `bin/`.
- Use the UI to load images, configure settings, and run stitching.

---

## 中文
## 詳細版本：0.0.5：此版本應用了 HalconDoNet 程式庫（請先從 MVTech 安裝 Halcon 25.05）
### 概述
该仓库包含一个 Windows Forms 应用，用于加载机器人拍摄的图像、生成拼接顺序、执行匹配并输出拼接结果图像。同时包含排序、匹配算法与配置管理等工具代码。

### 仓库结构
```
PLP.sln
README.md
PLP_Handshake_Protocol_v0.2.md
StitchingImage/
  App.config
  Program.cs
  MainForm.cs / MainForm.Designer.cs
  ProcessDialog.cs / ProcessDialog.Designer.cs
  Connection/
  Stitch_Tools/
  Properties/
```

### 界面主要功能
- **文件夹加载与解析**：加载图片目录并解析为分组与瓦片。
- **顺序可视化**：预览排序路径与移动方向。
- **匹配配置**：调整匹配算法参数并查看预览。
- **系统配置**：通过配置表查看/编辑系统参数，支持检测配置文件更新并自动加载。
- **运行拼接**：执行匹配与拼接并保存输出。
- **TCP/IP 连接面板**：连接/断开主机并查看协议日志。
- **日志**：运行日志输出到工作目录中的 `Log_YYYYMMDD.txt`。

### 构建与运行
**环境要求**
- Windows
- .NET Framework 4.8
- Visual Studio 2019+（或支持 .NET Framework 4.8 的 MSBuild）
- 需还原 NuGet 包（OpenCvSharp4）

**构建**
- 使用 Visual Studio 打开 `PLP.sln`。
- 还原 NuGet 包。
- 编译 `StitchingImage` 项目。

**运行**
- 启动 `StitchingImage` 项目（F5）或运行 `bin/` 目录下的 `StitchingImage.exe`。
- 通过界面加载图片、配置参数并运行拼接。
