# Tab 2 Test Report

## Commands run

| Command | Result | Notes |
|---|---|---|
| `find /workspace -name AGENTS.md -print` | Pass | No scoped AGENTS.md instructions were found. |
| `rg -n "CreateGerberSample|GerberSampleConfig|SampleConfig|SampleGrid|Crop|Manifest|WorkflowContext|EWindowControl|btnLoadConfig|btnCreateSample|sampleWindow" -S .` | Pass | Located Tab 2 flow and supporting classes. |
| `msbuild GerberViewer.sln /p:Configuration=Debug /p:Platform=x64 /m` | Warning | Container does not include `msbuild`. |
| `msbuild GerberViewer.sln /p:Configuration=Release /p:Platform=x64 /m` | Warning | Container does not include `msbuild`. |
| `dotnet --info || true; xbuild /version || true; mcs --version || true` | Warning | No .NET SDK, Mono xbuild, or Mono C# compiler is installed in the container. |
| `rg -n "catch \{ \}|catch\{\}" GerberStitching.Core GerberViewer EWindowControl || true` | Pass | No empty catch blocks remain in the searched project paths. |

## Manual/runtime tests not executed

- WinForms designer open/resize checks.
- PNG/BMP/TIFF visual checks in `sampleWindow`.
- HALCON runtime checks.
- x64 Debug/Release builds.
