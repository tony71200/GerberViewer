# RobotManager

## English
### Files & Roles
- `Domain.cs`: Domain models for images, orders, components, and options.
- `RobotOrderer.cs`: Builds ordering graphs and components from parsed tiles.
- `OrderStitchRunner.cs`: Executes matching for an order and coordinates stitching.
- `StitchingImage.cs`: Composes and saves the stitched image with timing breakdown.

---

## 中文
### 文件与作用
- `Domain.cs`：图像、订单、组件与参数的领域模型。
- `RobotOrderer.cs`：根据瓦片信息生成排序图与组件。
- `OrderStitchRunner.cs`：执行订单匹配并协调拼接流程。
- `StitchingImage.cs`：图像合成与保存，提供拼接/保存耗时统计。
