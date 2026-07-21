using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using StitchingImage.Stitch_Tools.Matcher;

namespace StitchingImage.Stitch_Tools.Utils
{
    public sealed class StitchingConfig : IEditableConfig
    {
        private static readonly string[] ConfigKeys =
        {
            nameof(Method),
            nameof(OrbNFeatures),
            nameof(RatioTest),
            nameof(AllowCloseSecondBest),
            nameof(CloseDiff),
            nameof(MaxHomoMatches),
            nameof(WorkMegapix),
            nameof(CoarseWorkMegapix),
            nameof(FineWorkMegapix),
            nameof(RoiMatchFraction),
            nameof(RoiMinPx),
            nameof(RansacThresh),
            nameof(RansacConf),
            nameof(RansacMaxIters),
            nameof(MinInliers),
            nameof(MinInlierRatio),
            nameof(MaxRmse),
            nameof(MinOverlapRatio),
            nameof(MaxAbsRotationDeg),
            nameof(EnforceRobotDirection),
            nameof(MinAbsTranslationForRobotCheck),
            nameof(MaxPerpOffsetPx),
            nameof(PreferPerpOffsetConstraint),
            nameof(ConvertAlpha),
            nameof(ConvertBeta),
            nameof(ManualOffsetHorizontalTx),
            nameof(ManualOffsetHorizontalTy),
            nameof(ManualOffsetVerticalTx),
            nameof(ManualOffsetVerticalTy),
            nameof(PhaseCorrFractionW),
            nameof(PhaseCorrFractionH),
            nameof(PhaseCorrFractionSpecial),
            nameof(PhaseCorrMinResponse)
        };

        public Method Method { get; set; } = Method.CoarseFine;
        public int OrbNFeatures { get; set; } = 500;

        public double RatioTest { get; set; } = 0.75;
        public bool AllowCloseSecondBest { get; set; } = true;
        public double CloseDiff { get; set; } = 3.0;
        public int MaxHomoMatches { get; set; } = 45;

        public double WorkMegapix { get; set; } = 10.0;
        public double CoarseWorkMegapix { get; set; } = 2.0;
        public double FineWorkMegapix { get; set; } = 10.0;

        public double RoiMatchFraction { get; set; } = 0.03;
        public int RoiMinPx { get; set; } = 64;

        public double RansacThresh { get; set; } = 5.0;
        public double RansacConf { get; set; } = 0.995;
        public int RansacMaxIters { get; set; } = 4000;

        public int MinInliers { get; set; } = 4;
        public double MinInlierRatio { get; set; } = 0.1;
        public double MaxRmse { get; set; } = 10.0;
        public double MinOverlapRatio { get; set; } = 0.01;
        public double MaxAbsRotationDeg { get; set; } = 8.0;

        public bool EnforceRobotDirection { get; set; } = true;
        public double MinAbsTranslationForRobotCheck { get; set; } = 1.0;
        public double MaxPerpOffsetPx { get; set; } = 10.0;
        public bool PreferPerpOffsetConstraint { get; set; } = true;

        public double ConvertAlpha { get; set; } = 1.0;
        public double ConvertBeta { get; set; } = -15.0;
        public double ManualOffsetHorizontalTx { get; set; }
        public double ManualOffsetHorizontalTy { get; set; }
        public double ManualOffsetVerticalTx { get; set; }
        public double ManualOffsetVerticalTy { get; set; }
        public double PhaseCorrFractionW { get; set; } = 0.03;
        public double PhaseCorrFractionH { get; set; } = 0.03;
        public double PhaseCorrFractionSpecial { get; set; } = 0.45;
        public double PhaseCorrMinResponse { get; set; } = 0.1;

        public StitchingConfig Clone() => (StitchingConfig)MemberwiseClone();

        public IEnumerable<string> GetKeys() => ConfigKeys;

        public object GetValue(string key)
        {
            switch (key)
            {
                case nameof(Method):
                    return Method;
                case nameof(OrbNFeatures):
                    return OrbNFeatures;
                case nameof(RatioTest):
                    return RatioTest;
                case nameof(AllowCloseSecondBest):
                    return AllowCloseSecondBest;
                case nameof(CloseDiff):
                    return CloseDiff;
                case nameof(MaxHomoMatches):
                    return MaxHomoMatches;
                case nameof(WorkMegapix):
                    return WorkMegapix;
                case nameof(CoarseWorkMegapix):
                    return CoarseWorkMegapix;
                case nameof(FineWorkMegapix):
                    return FineWorkMegapix;
                case nameof(RoiMatchFraction):
                    return RoiMatchFraction;
                case nameof(RoiMinPx):
                    return RoiMinPx;
                case nameof(RansacThresh):
                    return RansacThresh;
                case nameof(RansacConf):
                    return RansacConf;
                case nameof(RansacMaxIters):
                    return RansacMaxIters;
                case nameof(MinInliers):
                    return MinInliers;
                case nameof(MinInlierRatio):
                    return MinInlierRatio;
                case nameof(MaxRmse):
                    return MaxRmse;
                case nameof(MinOverlapRatio):
                    return MinOverlapRatio;
                case nameof(MaxAbsRotationDeg):
                    return MaxAbsRotationDeg;
                case nameof(EnforceRobotDirection):
                    return EnforceRobotDirection;
                case nameof(MinAbsTranslationForRobotCheck):
                    return MinAbsTranslationForRobotCheck;
                case nameof(MaxPerpOffsetPx):
                    return MaxPerpOffsetPx;
                case nameof(PreferPerpOffsetConstraint):
                    return PreferPerpOffsetConstraint;
                case nameof(ConvertAlpha):
                    return ConvertAlpha;
                case nameof(ConvertBeta):
                    return ConvertBeta;
                case nameof(ManualOffsetHorizontalTx):
                    return ManualOffsetHorizontalTx;
                case nameof(ManualOffsetHorizontalTy):
                    return ManualOffsetHorizontalTy;
                case nameof(ManualOffsetVerticalTx):
                    return ManualOffsetVerticalTx;
                case nameof(ManualOffsetVerticalTy):
                    return ManualOffsetVerticalTy;
                case nameof(PhaseCorrFractionW):
                    return PhaseCorrFractionW;
                case nameof(PhaseCorrFractionH):
                    return PhaseCorrFractionH;
                case nameof(PhaseCorrFractionSpecial):
                    return PhaseCorrFractionSpecial;
                case nameof(PhaseCorrMinResponse):
                    return PhaseCorrMinResponse;
                default:
                    return null;
            }
        }

        public bool UpdateValue(string key, string rawValue, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                error = "Key is empty.";
                return false;
            }

            if (rawValue == null)
                rawValue = string.Empty;

            try
            {
                switch (key)
                {
                    case nameof(Method):
                        if (Enum.TryParse(rawValue, true, out Method method))
                        {
                            Method = method;
                            return true;
                        }
                        error = "Invalid Method value.";
                        return false;
                    case nameof(OrbNFeatures):
                        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var nfeatures))
                        {
                            OrbNFeatures = nfeatures;
                            return true;
                        }
                        error = "Invalid OrbNFeatures value.";
                        return false;
                    case nameof(RatioTest):
                        if (TryParseDouble(rawValue, out var ratioTest))
                        {
                            RatioTest = ratioTest;
                            return true;
                        }
                        error = "Invalid RatioTest value.";
                        return false;
                    case nameof(AllowCloseSecondBest):
                        if (TryParseBool(rawValue, out var allowClose))
                        {
                            AllowCloseSecondBest = allowClose;
                            return true;
                        }
                        error = "Invalid AllowCloseSecondBest value.";
                        return false;
                    case nameof(CloseDiff):
                        if (TryParseDouble(rawValue, out var closeDiff))
                        {
                            CloseDiff = closeDiff;
                            return true;
                        }
                        error = "Invalid CloseDiff value.";
                        return false;
                    case nameof(MaxHomoMatches):
                        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxHomo))
                        {
                            MaxHomoMatches = maxHomo;
                            return true;
                        }
                        error = "Invalid MaxHomoMatches value.";
                        return false;
                    case nameof(WorkMegapix):
                        if (TryParseDouble(rawValue, out var workMp))
                        {
                            WorkMegapix = workMp;
                            return true;
                        }
                        error = "Invalid WorkMegapix value.";
                        return false;
                    case nameof(CoarseWorkMegapix):
                        if (TryParseDouble(rawValue, out var coarseMp))
                        {
                            CoarseWorkMegapix = coarseMp;
                            return true;
                        }
                        error = "Invalid CoarseWorkMegapix value.";
                        return false;
                    case nameof(FineWorkMegapix):
                        if (TryParseDouble(rawValue, out var fineMp))
                        {
                            FineWorkMegapix = fineMp;
                            return true;
                        }
                        error = "Invalid FineWorkMegapix value.";
                        return false;
                    case nameof(RoiMatchFraction):
                        if (TryParseDouble(rawValue, out var roiFrac))
                        {
                            RoiMatchFraction = roiFrac;
                            return true;
                        }
                        error = "Invalid RoiMatchFraction value.";
                        return false;
                    case nameof(RoiMinPx):
                        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var roiMin))
                        {
                            RoiMinPx = roiMin;
                            return true;
                        }
                        error = "Invalid RoiMinPx value.";
                        return false;
                    case nameof(RansacThresh):
                        if (TryParseDouble(rawValue, out var ransacThresh))
                        {
                            RansacThresh = ransacThresh;
                            return true;
                        }
                        error = "Invalid RansacThresh value.";
                        return false;
                    case nameof(RansacConf):
                        if (TryParseDouble(rawValue, out var ransacConf))
                        {
                            RansacConf = ransacConf;
                            return true;
                        }
                        error = "Invalid RansacConf value.";
                        return false;
                    case nameof(RansacMaxIters):
                        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ransacIters))
                        {
                            RansacMaxIters = ransacIters;
                            return true;
                        }
                        error = "Invalid RansacMaxIters value.";
                        return false;
                    case nameof(MinInliers):
                        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minInliers))
                        {
                            MinInliers = minInliers;
                            return true;
                        }
                        error = "Invalid MinInliers value.";
                        return false;
                    case nameof(MinInlierRatio):
                        if (TryParseDouble(rawValue, out var minInlierRatio))
                        {
                            MinInlierRatio = minInlierRatio;
                            return true;
                        }
                        error = "Invalid MinInlierRatio value.";
                        return false;
                    case nameof(MaxRmse):
                        if (TryParseDouble(rawValue, out var maxRmse))
                        {
                            MaxRmse = maxRmse;
                            return true;
                        }
                        error = "Invalid MaxRmse value.";
                        return false;
                    case nameof(MinOverlapRatio):
                        if (TryParseDouble(rawValue, out var minOverlapRatio))
                        {
                            MinOverlapRatio = minOverlapRatio;
                            return true;
                        }
                        error = "Invalid MinOverlapRatio value.";
                        return false;
                    case nameof(MaxAbsRotationDeg):
                        if (TryParseDouble(rawValue, out var maxRotation))
                        {
                            MaxAbsRotationDeg = maxRotation;
                            return true;
                        }
                        error = "Invalid MaxAbsRotationDeg value.";
                        return false;
                    case nameof(EnforceRobotDirection):
                        if (TryParseBool(rawValue, out var enforceRobot))
                        {
                            EnforceRobotDirection = enforceRobot;
                            return true;
                        }
                        error = "Invalid EnforceRobotDirection value.";
                        return false;
                    case nameof(MinAbsTranslationForRobotCheck):
                        if (TryParseDouble(rawValue, out var minAbs))
                        {
                            MinAbsTranslationForRobotCheck = minAbs;
                            return true;
                        }
                        error = "Invalid MinAbsTranslationForRobotCheck value.";
                        return false;
                    case nameof(MaxPerpOffsetPx):
                        if (TryParseDouble(rawValue, out var maxPerp))
                        {
                            MaxPerpOffsetPx = maxPerp;
                            return true;
                        }
                        error = "Invalid MaxPerpOffsetPx value.";
                        return false;
                    case nameof(PreferPerpOffsetConstraint):
                        if (TryParseBool(rawValue, out var preferPerp))
                        {
                            PreferPerpOffsetConstraint = preferPerp;
                            return true;
                        }
                        error = "Invalid PreferPerpOffsetConstraint value.";
                        return false;
                    case nameof(ConvertAlpha):
                        if (TryParseDouble(rawValue, out var convertAlpha))
                        {
                            ConvertAlpha = convertAlpha;
                            return true;
                        }
                        error = "Invalid ConvertAlpha value.";
                        return false;
                    case nameof(ConvertBeta):
                        if (TryParseDouble(rawValue, out var convertBeta))
                        {
                            ConvertBeta = convertBeta;
                            return true;
                        }
                        error = "Invalid ConvertBeta value.";
                        return false;
                    case nameof(ManualOffsetHorizontalTx):
                        if (TryParseDouble(rawValue, out var htx))
                        {
                            ManualOffsetHorizontalTx = htx;
                            return true;
                        }
                        error = "Invalid ManualOffsetHorizontalTx value.";
                        return false;
                    case nameof(ManualOffsetHorizontalTy):
                        if (TryParseDouble(rawValue, out var hty))
                        {
                            ManualOffsetHorizontalTy = hty;
                            return true;
                        }
                        error = "Invalid ManualOffsetHorizontalTy value.";
                        return false;
                    case nameof(ManualOffsetVerticalTx):
                        if (TryParseDouble(rawValue, out var vtx))
                        {
                            ManualOffsetVerticalTx = vtx;
                            return true;
                        }
                        error = "Invalid ManualOffsetVerticalTx value.";
                        return false;
                    case nameof(ManualOffsetVerticalTy):
                        if (TryParseDouble(rawValue, out var vty))
                        {
                            ManualOffsetVerticalTy = vty;
                            return true;
                        }
                        error = "Invalid ManualOffsetVerticalTy value.";
                        return false;
                    case nameof(PhaseCorrFractionW):
                        if (TryParseDouble(rawValue, out var pcw))
                        {
                            PhaseCorrFractionW = pcw;
                            return true;
                        }
                        error = "Invalid PhaseCorrFractionW value.";
                        return false;
                    case nameof(PhaseCorrFractionH):
                        if (TryParseDouble(rawValue, out var pch))
                        {
                            PhaseCorrFractionH = pch;
                            return true;
                        }
                        error = "Invalid PhaseCorrFractionH value.";
                        return false;
                    case nameof(PhaseCorrFractionSpecial):
                        if (TryParseDouble(rawValue, out var pcs))
                        {
                            PhaseCorrFractionSpecial = pcs;
                            return true;
                        }
                        error = "Invalid PhaseCorrFractionSpecial value.";
                        return false;
                    case nameof(PhaseCorrMinResponse):
                        if (TryParseDouble(rawValue, out var pcMin))
                        {
                            PhaseCorrMinResponse = pcMin;
                            return true;
                        }
                        error = "Invalid PhaseCorrMinResponse value.";
                        return false;
                    default:
                        error = $"Unknown key: {key}.";
                        return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static StitchingConfig LoadFromYaml(string path)
        {
            var cfg = new StitchingConfig();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return cfg;

            try
            {
                var lines = File.ReadAllLines(path);
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var idx = line.IndexOf(':');
                    if (idx <= 0)
                        continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();
                    cfg.UpdateValue(key, value, out _);
                }
            }
            catch
            {
                return cfg;
            }

            return cfg;
        }

        public void SaveToYaml(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                var lines = new List<string>();
                foreach (var key in ConfigKeys)
                {
                    var value = GetValue(key);
                    lines.Add($"{key}: {FormatValue(value)}");
                }
                File.WriteAllLines(path, lines);
            }
            catch
            {
                // ignore
            }
        }

        private static bool TryParseDouble(string raw, out double value)
        {
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            if (bool.TryParse(raw, out value))
                return true;
            if (string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            if (string.Equals(raw, "0", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }
            return false;
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            switch (value)
            {
                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case bool b:
                    return b ? "true" : "false";
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }
    }
}
