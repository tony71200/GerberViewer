# EWindowControl

EWindowControl provides a reusable HALCON-backed image viewer with ROI editing and mouse interaction support.

| File | Class/Function | Purpose |
| --- | --- | --- |
| EWindowControl.cs | EWindowControl | User control that displays images and manages interactive ROI operations. |
| EWindowControl.cs | EWindowControl.Load / display methods | Loads image content and refreshes the HALCON window display. |
| EWindowControl.cs | EWindowControl ROI methods | Adds, updates, selects, and removes ROI entries shown on the viewer. |
| PrviateFunc.cs | EWindowControl private helpers | Implements internal drawing, coordinate conversion, and mouse-handling logic. |
| ERoiList.cs | ERoiList | Maintains the collection of ROI definitions associated with the viewer. |
| PubliceStructure.cs | ROIParm | Stores ROI geometry, type, direction, and display attributes. |
| PubliceStructure.cs | EMouseEventArgs | Carries viewer mouse coordinates and interaction details to event subscribers. |
| PubliceStructure.cs | RoiType / ROI_Direction / BrushEraseType | Enumerate ROI shape, resize direction, and brush-erase behavior. |
| ImportSystemDLL.cs | ImportSystemDLL | Declares native Windows APIs used by hot-key and window interaction code. |
