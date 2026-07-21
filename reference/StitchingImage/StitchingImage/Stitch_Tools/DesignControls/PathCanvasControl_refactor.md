# Refactor `PathCanvasControl` (WinForms .NET 4.8) — Top/Canvas/Bottom + 2 graphs

Tài liệu này mô tả các chỉnh sửa cần làm để `PathCanvasControl` đáp ứng các yêu cầu UI mới:
- **Top**: 2 checkbox bật/tắt mũi tên **ArrangeGraph** và **TraversalGraph**
- **Middle**: vùng vẽ points + path
- **Bottom**: **Legend** (status tip riêng) + **Info** (status tip hiện trạng/mode)

> Hiện tại control đang vẽ mọi thứ trong `OnPaint` của `UserControl` và chỉ có `statusStripTraversal` (đang đặt ở vị trí (0,0)). fileciteturn7file0 fileciteturn7file1  
> `OnPaint` đang vẽ “zigzag layout (blue)” + “stitch traversal (red)”. fileciteturn7file2

---

## 0) Mục tiêu kỹ thuật

1) Tách layout UI **Top/Canvas/Bottom** bằng `Dock`.
2) Vẽ 2 graph:
   - **TraversalGraph** = **đỏ**
   - **ArrangeGraph** = **xanh (green)**
3) Checkbox:
   - Mỗi checkbox bật/tắt hiển thị mũi tên tương ứng
   - Checkbox có màu text tương ứng (green/red)
4) Nếu có tọa độ (`XRobot`, `YRobot`) thì:
   - map theo tọa độ thật
   - hiển thị trục + chia độ
   - (tuỳ chọn) hiển thị grid lines theo tick để dễ nhìn
5) MainForm: cập nhật cách set data cho PathCanvasControl theo API mới.

---

## 1) Thay đổi UI layout trong `PathCanvasControl.Designer.cs`

### 1.1. Thêm các control mới

**Thêm:**
- `Panel panelTop` (Dock = Top)
- `FlowLayoutPanel flowTop` (Dock = Fill) để đặt 2 checkbox gọn
- `CheckBox chkShowArrange` (green)
- `CheckBox chkShowTraversal` (red)
- `Panel panelCanvas` (Dock = Fill) — *vùng vẽ*
- `Panel panelBottom` (Dock = Bottom)
- `StatusStrip statusStripLegend` + `ToolStripStatusLabel statusLabelLegend`
- **Giữ** `statusStripTraversal` (đổi Dock xuống Bottom) làm status tip “Info”

> Gợi ý: dùng `panelCanvas.Paint` để vẽ (thay vì override `UserControl.OnPaint`) để khỏi phải tự trừ chiều cao top/bottom.

### 1.2. Skeleton Designer (đặt Dock đúng thứ tự)

> Đây là skeleton minh họa, bạn copy ý tưởng và để Designer regen phần chi tiết.

```csharp
// Fields (Designer)
private System.Windows.Forms.Panel panelTop;
private System.Windows.Forms.FlowLayoutPanel flowTop;
private System.Windows.Forms.CheckBox chkShowArrange;
private System.Windows.Forms.CheckBox chkShowTraversal;

private System.Windows.Forms.Panel panelCanvas;

private System.Windows.Forms.Panel panelBottom;
private System.Windows.Forms.StatusStrip statusStripLegend;
private System.Windows.Forms.ToolStripStatusLabel statusLabelLegend;

// existing:
private System.Windows.Forms.StatusStrip statusStripTraversal;
private System.Windows.Forms.ToolStripStatusLabel statusLabelTraversal;
```

```csharp
// InitializeComponent (Designer) — docking order matters
this.panelTop = new System.Windows.Forms.Panel();
this.flowTop = new System.Windows.Forms.FlowLayoutPanel();
this.chkShowArrange = new System.Windows.Forms.CheckBox();
this.chkShowTraversal = new System.Windows.Forms.CheckBox();

this.panelCanvas = new System.Windows.Forms.Panel();

this.panelBottom = new System.Windows.Forms.Panel();
this.statusStripLegend = new System.Windows.Forms.StatusStrip();
this.statusLabelLegend = new System.Windows.Forms.ToolStripStatusLabel();

this.statusStripTraversal = new System.Windows.Forms.StatusStrip();
this.statusLabelTraversal = new System.Windows.Forms.ToolStripStatusLabel();

// --- panelTop
this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
this.panelTop.Height = 36;

this.flowTop.Dock = System.Windows.Forms.DockStyle.Fill;
this.flowTop.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
this.flowTop.WrapContents = false;
this.flowTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 0);

this.chkShowArrange.Text = "Arrange arrows";
this.chkShowArrange.Checked = true;
this.chkShowArrange.AutoSize = true;
this.chkShowArrange.ForeColor = System.Drawing.Color.ForestGreen;

this.chkShowTraversal.Text = "Traversal arrows";
this.chkShowTraversal.Checked = true;
this.chkShowTraversal.AutoSize = true;
this.chkShowTraversal.ForeColor = System.Drawing.Color.Red;

this.flowTop.Controls.Add(this.chkShowArrange);
this.flowTop.Controls.Add(this.chkShowTraversal);
this.panelTop.Controls.Add(this.flowTop);

// --- panelBottom (2 status strips stacked)
this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
this.panelBottom.Height = 44;

this.statusStripLegend.Dock = System.Windows.Forms.DockStyle.Top;
this.statusStripLegend.SizingGrip = false;
this.statusStripLegend.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.statusLabelLegend });

this.statusStripTraversal.Dock = System.Windows.Forms.DockStyle.Bottom;
this.statusStripTraversal.SizingGrip = false;
this.statusStripTraversal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.statusLabelTraversal });

this.panelBottom.Controls.Add(this.statusStripTraversal);
this.panelBottom.Controls.Add(this.statusStripLegend);

// --- panelCanvas
this.panelCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
this.panelCanvas.BackColor = System.Drawing.SystemColors.ControlLightLight;

// --- PathCanvasControl
this.Controls.Add(this.panelCanvas);
this.Controls.Add(this.panelBottom);
this.Controls.Add(this.panelTop);
```

### 1.3. Sự kiện (khai báo trong code-behind, không để trong designer)
- `panelCanvas.Paint += PanelCanvas_Paint`
- `chkShowArrange.CheckedChanged += (_,__) => InvalidateCanvas();`
- `chkShowTraversal.CheckedChanged += (_,__) => InvalidateCanvas();`

---

## 2) Thay đổi `PathCanvasControl.cs` (logic vẽ + checkbox)

### 2.1. Trạng thái hiển thị
Thêm fields:

```csharp
private bool _showArrange = true;
private bool _showTraversal = true;
```

Khi checkbox đổi:
- cập nhật `_showArrange/_showTraversal`
- `panelCanvas.Invalidate()`
- update legend text

### 2.2. Chuyển vẽ sang `panelCanvas.Paint`
Hiện tại bạn override `OnPaint` và dùng `ClientSize`. fileciteturn7file0  
Đổi thành:

- `private void PanelCanvas_Paint(object sender, PaintEventArgs e)` gọi `DrawCanvas(e.Graphics, panelCanvas.ClientRectangle)`
- `DrawCanvas(...)` là hàm chứa toàn bộ logic cũ (map/bounds/axes/arrows/nodes).

### 2.3. Đổi màu & bật/tắt mũi tên
Hiện tại:
- “zigzagLayoutPen” đang **blue**
- “stitchTraversePen” đang **red** fileciteturn7file2

Đổi thành:
- **Arrange** pen = green (ForestGreen/LimeGreen)  
- **Traversal** pen = red

Trong vẽ:
```csharp
if (_showArrange) DrawArrangeArrows(..., arrangeGreenPen);
if (_showTraversal) DrawTraversalArrows(..., traversalRedPen);
```

> Mẹo: để 2 loại mũi tên không đè lên nhau, dùng offset vuông góc:
- Arrange offset = +6
- Traversal offset = -6

### 2.4. Vẽ theo “kết quả 2 graph”
Bạn có 2 lựa chọn:

#### Option A (khuyến nghị): vẽ theo `PathSegment` (dễ nhất)
- Arrange: dùng `ArrangeComponent.Path : List<PathSegment>` (Domain có sẵn) fileciteturn8file4  
- Traversal: dùng `TraversalGraph.PathSegments` (nếu builder có), hoặc tự build tương tự từ `LinksById`.

Ưu điểm:
- vẽ thẳng `FromId -> ToId` theo order
- direction sẵn (Left/Right/Up/Down/Jump) để offset đẹp

#### Option B: vẽ theo `LinksById` (HNext/VNext, Prev/Next,…)
- TraversalGraph: `LinksById[from].HNext/VNext`
- ArrangeGraph: `LinksById[from].Next/LineNext/InterLineNext`
Ưu điểm: không cần path list; nhược: khó preserve “order”

---

## 3) Map điểm theo Robot coordinates + hiển thị chia độ

Hiện tại đã có:
- nếu thiếu tọa độ thì `useVirtualLayout = true`
- nếu có tọa độ thì `DrawAxes(...)` vẽ trục + ticks fileciteturn7file3 fileciteturn7file6

Chỉnh nhẹ theo yêu cầu:
1) **Chỉ vẽ axes/ticks khi tọa độ đủ** (giữ như hiện tại).
2) (tuỳ chọn) thêm **grid lines** theo ticks:
   - trong `DrawAxes`, sau khi tính tickX/tickY, vẽ line mờ chạy dọc/ngang trong plot.
3) Tick label: giữ format `"0.##"`.

---

## 4) Legend + Info ở bottom

### 4.1. Legend strip (fixed / semi-fixed)
`statusLabelLegend.Text` ví dụ:
- `"Legend: Green=ArrangeGraph, Red=TraversalGraph, Black=Node, Gray=Layout"`

### 4.2. Info strip (dynamic)
Giữ logic `UpdateTraversalStatus()` nhưng sửa text theo màu mới:
- bỏ “Blue”
- dùng “Green/Red”
- có thể thêm trạng thái checkbox (ON/OFF)

Hiện tại `UpdateTraversalStatus()` đang set `"Gray... Blue... Red..."` fileciteturn7file3  
Đổi thành:
- `"Gray=layout, Green=ArrangeGraph, Red=TraversalGraph (Mode=...) ..."`

---

## 5) API mới cho `PathCanvasControl` (để MainForm set data)

Hiện `SetData(OrderedGroupResult data)` chỉ nhận traversal (OrderGraph). fileciteturn7file0  
Bạn cần thêm 1 API nhận cả **Arrange** và **Traversal**.

### 5.1. Đề xuất API
```csharp
public void SetData(ArrangeBatchResult arrange, OrderedGroupResult traversal)
```

Hoặc nếu bạn chỉ show 1 component:
```csharp
public void SetData(ArrangeComponent arrangeComp, OrderComponent traversalComp)
```

> Tip: join theo index: `ArrangeComponent.Index` ↔ `OrderComponent.ComponentIndex`. fileciteturn8file4 fileciteturn8file1

### 5.2. Backward-compatible
Giữ overload cũ:
```csharp
public void SetData(OrderedGroupResult data) { ... } // set traversal only
```

---

## 6) Cập nhật MainForm

Vì bạn chưa gửi `MainForm.cs`, hướng sửa sẽ là “pattern”:

1) Nơi bạn build data (click button / load folder):
   - build `ArrangeBatchResult arrange = robotArrange.Arrange(images, opt);`
   - build `OrderedGroupResult traversal = RobotOrderer.BuildOrdersForGroup(groupId, imagesArray, opt);`
     (hoặc traversal từ TraversalGraph mới — tuỳ pipeline)

2) Call PathCanvasControl:
```csharp
pathCanvasControl.SetData(arrange, traversal);
```

3) Nếu bạn đang bind control theo “component selected” (combobox/list):
   - đổi handler selection để set theo đúng component index.

---

## 7) Checklist nhanh

- [ ] Designer: top/center/bottom dock đúng thứ tự
- [ ] Canvas vẽ ở `panelCanvas.Paint`
- [ ] Checkbox toggle làm `panelCanvas.Invalidate()`
- [ ] Arrange arrows = green, Traversal arrows = red
- [ ] Khi có XRobot/YRobot: axes + ticks (grid optional)
- [ ] Bottom có 2 status strips: Legend + Info
- [ ] MainForm gọi API mới

---

## 8) Gợi ý nhỏ (tuỳ chọn)
- Thêm `ToolTip` cho checkbox (giải thích graph nào là gì)
- Thêm “Ctrl+MouseWheel” để zoom, drag để pan (sau)
