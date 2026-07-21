# Changed Files

| File | Class/Method | Before | After | Reason |
|---|---|---|---|---|
| GerberStitching.Core/Models/SampleManifest.cs | SampleManifest/SampleTileInfo/Validator/Serializer | No canonical v1 contract file | Shared DataContract model and validator | Fix Tab 2/Tab 3 JSON mismatch. |
| GerberStitching.Core/Models/WorkflowModels.cs | SampleManifest/SampleTileInfo | Inline duplicate DTOs | Removed duplicates | Enforce one canonical manifest. |
| GerberStitching.Core/Imaging/PreparedSampleRun.cs | PreparedSampleRun/SamplePreparationService | No run model | Owns source/processed HObject and layout | Single preview/generator source of truth. |
| GerberStitching.Core/Imaging/SampleTileGenerator.cs | GenerateAsync/Generate | Reopened source Bitmap and deleted output root | Consumes prepared run, HALCON crop, temp publication | Avoid Bitmap reread and destructive deletion. |
| GerberViewer/Views/CreateGerberSampleControl.cs | Open/Refresh/Create | UI-thread preparation and generator by config path | Background decode/prepare and prepared-run generation | Responsiveness and consistency. |
| GerberViewer/Views/GerberSampleWindow.cs | SetSourceImage/RenderImageOverlay | Reflection into base private hWindow | Public inherited API with ShowRegions | Preserve base behavior without editing EWindowControl. |
| GerberStitching.Core/GerberStitching.Core.csproj | references/compile items | No HALCON/core new files | Adds HALCON reference and new compile items | Core now owns HObject pipeline. |
