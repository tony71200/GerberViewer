---
name: StitchingImage Project Knowledge
description: Architecture overview and development instructions for the StitchingImage C# WinForms project.
---

# Code Change and Development Rules
- Always comment before any changes are made using the format [Name of person/Agent] [Change time: YYMMdd] [Purpose of change]
- Do not delete code; only comment out the line/section of code to be modified and add it below.
- Always report the percentage of code changes made.
- Check the StitchingImage Overview to ensure the overall goal is met.

# Overview of StitchingImage

The [StitchingImage](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/StitchingImage.cs#20-865) project is a C# .NET Framework 4.8 Windows Forms application designed to stitch multiple microscopic or robotic captured images into a large coherent mosaic. It uses both OpenCV (via OpenCvSharp4) and HALCON for image processing.

## Core Architecture

### 1. RobotManager
Handles graphing, sorting, and orchestrating the stitching pipeline.
- [StitchingImage.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/StitchingImage.cs): The core entry point for final mosaic composition. It supports OpenCV-based arbitrary scaling and affine/homography stitching, as well as an alternative [StitchHalcon](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/StitchingImage.cs#244-478) using `gen_projective_mosaic`.
- [RobotOrderer.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.cs): Sorts a list of [ImageInfo](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/Domain.cs#9-30) (representing each tile) into an [OrderGraph](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/Domain.cs#97-104). It connects nodes horizontally (HNext) and vertically (VNext) based on Euclidean distance, row clustering, or a fallback "Fly-node" grid strategy.
- [Domain.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/Domain.cs): Contains data models like [ImageInfo](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/Domain.cs#9-30), [OrderComponent](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/Domain.cs#76-87), [OrderedRow](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/Domain.cs#105-112), [OrderGraph](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/Domain.cs#97-104).

### 2. Matcher
Implements pairwise image matching to compute edge transforms (homography `HBToA`).
- [PairMatching.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/Matcher/PairMatching.cs): Base abstract class providing evaluation logic, ROI handling, overlap ratios, and RMSE validation.
- [FeatureMatcher.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/Matcher/FeatureMatcher.cs): Base for descriptor-based matching. Detects keypoints in ROIs, matches them (KNN), filters by distance and perpendicular constraints, then computes a rigid/homography transformation via RANSAC.
- `ORBMatcher.cs`, `SiftMatcher.cs`, `BriskMatcher.cs`, [PhaseCorrMatcher.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/Matcher/PhaseCorrMatcher.cs): Concrete implementations for matching. [PhaseCorrMatcher](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/Matcher/PhaseCorrMatcher.cs#15-371) specifically uses FFT-based phase correlation which is extremely fast for pure translations.

## Common Operations & Tips

- **Debugging**: Ensure Visual Studio is installed with ".NET desktop development" and ".NET Framework 4.8 SDK". The CLI commands or MSBuild can be used, but debugging inside Visual Studio provides the best UI visualization.
- **Handling Coordinates**: Many files compute distances with `XRobot`/`YRobot`. Always check `double.IsNaN` or fallback gracefully to prevent crashes.

## Current Known Design Caveats
- If coordinate/position data is missing, the system uses a blind `FlyNodeInterval` interval stringing approach which completely fails if images are irregularly captured.
- Overlap calculation relies heavily on valid target coordinate deltas (`dxRobot`, `dyRobot`).
