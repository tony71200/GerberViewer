# Utils

## English
### Files & Roles
- `AppSettings.cs`: Loads/saves app settings and config file paths.
- `SystemConfig.cs`: System-level configuration model and YAML parsing.
- `StitchingConfig.cs`: Matching configuration model and YAML parsing.
- `Logger.cs`: UI + file logging utilities.
- `ImageStore.cs`: In-memory storage of parsed images and groups.
- `FilenameParser.cs`: Parse filename into metadata (group, position, etc.).
- `ImageRead.cs`: Image IO helpers.
- `AlignmentRefinement.cs`: Alignment and refinement utilities.
- `IEditableConfig.cs` / `IReadOnlyConfigKeys.cs`: Config interfaces for UI grids.

---

## 中文
### 文件与作用
- `AppSettings.cs`：加载/保存应用设置与配置文件路径。
- `SystemConfig.cs`：系统配置模型与 YAML 解析。
- `StitchingConfig.cs`：匹配配置模型与 YAML 解析。
- `Logger.cs`：界面与文件日志工具。
- `ImageStore.cs`：解析图像与分组的内存存储。
- `FilenameParser.cs`：从文件名解析元数据（组号、位置等）。
- `ImageRead.cs`：图像读写辅助。
- `AlignmentRefinement.cs`：对齐与优化工具。
- `IEditableConfig.cs` / `IReadOnlyConfigKeys.cs`：配置接口，用于配置表格显示。
