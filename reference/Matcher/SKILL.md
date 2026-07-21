# Matcher Development Skill

## Scope
This skill applies to all files in `PCM_Inspection_Demo/Matcher`.

## Goal
Provide a practical workflow to maintain matcher stability (accuracy, speed, memory) for alignment between Sample/Test ROI images.

## Matcher architecture summary
- `BaseMatcher.cs`: common matcher contract (`Run`, `RunAsync`, `MatchResult`, fail helper).
- `MatcherHelper.cs`: shared OpenCV utilities (ROI conversion, grayscale float, FFT spectrum, transform helpers, ROI validation).
- Matchers by capability:
  - `PharseCorrMatcher*`: translation-centric phase correlation family.
  - `FourierMellinMatcher`: rotation + scale + translation (frequency domain).
  - `PyramidPhaseMatcher`: large-shift translation from coarse-to-fine pyramid.
  - `EccMatcher`: iterative refinement alignment.
  - `FourierMellinEccMatcher`: 2-stage pipeline (rough FM + fine ECC).

## Operational workflow (recommended)
1. **Validate input first**
   - Verify ROI sizes with `MatcherHelper.IsRoiValid`.
   - Ensure all `PhaseCorrelate` input mats share the same width/height.
2. **Normalize image format**
   - Use single-channel `CV_32F` grayscale for correlation-heavy steps.
3. **Use Hanning window correctly**
   - Always pass a real Hanning mat to `Cv2.PhaseCorrelate` (third parameter), not `null`.
4. **Manage memory aggressively**
   - Prefer `using` blocks for temp `Mat`.
   - Reuse intermediate mats/window where possible.
   - Dispose window/temporary mats promptly in loops.
5. **Keep confidence gates explicit**
   - Preserve per-stage confidence thresholds and clear failure messages.
6. **Respect cancellation**
   - Call `token.ThrowIfCancellationRequested()` before expensive steps.

## Performance & memory checklist
- Avoid unnecessary `Clone()` and repeat grayscale conversions.
- Resize once when dimensions mismatch; avoid repeated resampling.
- In pyramid loops, free per-level temporary mats immediately.
- Return concise diagnostic messages for low-confidence cases.

## Debug checklist
- Confirm ROI dimensions in logs when match fails.
- Verify angle/scale stage and translation stage confidences separately.
- Compare coarse result and refined result for 2-stage pipelines.

## Done criteria
- Build succeeds.
- `PhaseCorrelate` never receives `null` window.
- Correlated mats are same size at each call site.
- No obvious temporary mat leaks in hot paths.


## Obsidian references (Important)
- `docs/obsidian/Matcher/BaseMatcher.md`
- `docs/obsidian/Matcher/PyramidPhaseMatcher.md`
- `docs/obsidian/Matcher/EccMatcher.md`
- `docs/obsidian/Matcher/EccMatcher2.md`
- Use these notes to preserve transform direction and reduce regression during matcher rewrites.
