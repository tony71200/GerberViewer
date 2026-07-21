# Matcher

## English
### Files & Roles
- `PairMatching.cs`: Pair matching orchestration and shared helpers.
- `FeatureMatcher.cs`: Base matcher behavior.
- `CoarseFineMatcher.cs`: Coarse-to-fine matching strategy.
- `ORBMatcher.cs`, `ORBPharseMatcher.cs`: ORB-based matching variants.
- `SiftMatcher.cs`, `BriskMatcher.cs`: SIFT/BRISK matching implementations.
- `PhaseCorrMatcher.cs`: Phase correlation matching (supports special gap handling).
- `ManualMatcher.cs`: Manual offset matching fallback.

---

## 中文
### 文件与作用
- `PairMatching.cs`：配对匹配的流程与通用辅助函数。
- `FeatureMatcher.cs`：匹配器基础行为。
- `CoarseFineMatcher.cs`：粗到细匹配策略。
- `ORBMatcher.cs`、`ORBPharseMatcher.cs`：ORB 匹配变体。
- `SiftMatcher.cs`、`BriskMatcher.cs`：SIFT/BRISK 匹配实现。
- `PhaseCorrMatcher.cs`：相位相关匹配（支持特殊间隙）。
- `ManualMatcher.cs`：手动偏移的匹配兜底。
