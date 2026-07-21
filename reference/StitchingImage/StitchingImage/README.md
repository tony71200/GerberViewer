# StitchingImage (Project)

## English
### Folder Structure
```
StitchingImage/
  App.config
  Program.cs
  MainForm.cs / MainForm.Designer.cs
  ProcessDialog.cs / ProcessDialog.Designer.cs
  Connection/
  Stitch_Tools/
  Properties/
  StitchingImage.csproj
  packages.config
```

### File Responsibilities (High Level)
- **Program.cs**: Application entry point.
- **MainForm.cs**: Main UI logic (loading, ordering, stitching, config sync).
- **ProcessDialog.cs**: Progress UI shown during long-running stitching.
- **Connection/**: TCP/IP connection manager.
- **Stitch_Tools/**: Core stitching logic, matchers, ordering, config, and utilities.
- **Properties/**: Assembly info, resources, and settings.

---

## 中文
### 目录结构
```
StitchingImage/
  App.config
  Program.cs
  MainForm.cs / MainForm.Designer.cs
  ProcessDialog.cs / ProcessDialog.Designer.cs
  Connection/
  Stitch_Tools/
  Properties/
  StitchingImage.csproj
  packages.config
```

### 文件职责（概览）
- **Program.cs**：程序入口。
- **MainForm.cs**：主界面逻辑（加载、排序、拼接、配置同步）。
- **ProcessDialog.cs**：拼接时显示进度的对话框。
- **Connection/**：TCP/IP 连接管理。
- **Stitch_Tools/**：拼接核心逻辑、匹配器、排序、配置与工具类。
- **Properties/**：程序集信息、资源与设置。
