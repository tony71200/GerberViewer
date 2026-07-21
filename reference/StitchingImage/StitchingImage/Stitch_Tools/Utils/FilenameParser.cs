using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using StitchingImage.Stitch_Tools.RobotManager;

namespace StitchingImage.Stitch_Tools.Utils
{
    public static class FilenameParser
    {
        private const string DefaultRegex = @"CameraImage(?<id>\d+)_(?<position>\d+|nan)#(?<group>\d+)_(?<y>-?\d+(?:\.\d+)?|nan)_(?<x>-?\d+(?:\.\d+)?|nan)(?:\.\w+)$";
        // [Codex] [Change time: 260318] [Centralize consistent missing-value defaults for optional position/x/y fields]
        private static readonly int? MissingPositionId = null;
        private const double MissingCoordinate = double.NaN;
        private static Regex _activeRegex = new Regex(DefaultRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static string CurrentPattern { get; private set; } = DefaultRegex;

        public static bool ConfigurePattern(string pattern)
        {
            _activeRegex = BuildRegex(pattern);
            CurrentPattern = string.IsNullOrWhiteSpace(pattern) ? DefaultRegex : pattern;
            return true;
        }

        public static bool TryParseWithPattern(string fileName, string pattern, bool invertX, out ImageInfo info, out string error)
        {
            info = null;
            error = null;
            try
            {
                var rx = BuildRegex(pattern);
                return TryParseInternal(fileName, invertX, rx, out info);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static Regex BuildRegex(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return new Regex(DefaultRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            bool isTemplate = pattern.IndexOf('<') >= 0
                && pattern.IndexOf('>') >= 0
                && pattern.IndexOf("(?<", StringComparison.Ordinal) < 0;

            var regexPattern = isTemplate ? BuildRegexFromTemplate(pattern) : pattern;
            return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        private static string BuildRegexFromTemplate(string template)
        {
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["prefix"] = @"(?:[^\d]+)", // NEW: match any non-digit prefix right before <id>
                ["group_id"] = @"(?<group>\d+)",
                ["group"] = @"(?<group>\d+)",
                ["position"] = @"(?<position>\d+|none|nan)",
                ["id"] = @"(?<id>\d+)",
                ["image_id"] = @"(?<id>\d+)",
                ["x"] = @"(?<x>-?\d+(?:\.\d+)?|none|nan)",
                ["y"] = @"(?<y>-?\d+(?:\.\d+)?|none|nan)",
                ["extension"] = @"(?:\.\w+)",
                ["ignore"] = @"(?:\d+)",
            };

            var sb = new StringBuilder("^");
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] == '<')
                {
                    int close = template.IndexOf('>', i + 1);
                    if (close > i)
                    {
                        var key = template.Substring(i + 1, close - i - 1).Trim();
                        if (tokens.TryGetValue(key, out var tokenRegex))
                        {
                            sb.Append(tokenRegex);
                            i = close;
                            continue;
                        }
                    }
                }

                sb.Append(Regex.Escape(template[i].ToString()));
            }
            sb.Append("$");
            return sb.ToString();
        }

        public static bool TryParse(string filePath, bool invertX, out ImageInfo info)
        {
            var name = Path.GetFileName(filePath);
            if (!TryParseInternal(name, invertX, _activeRegex, out info))
                return false;

            info = new ImageInfo(filePath, info.GroupId, info.ImageId, info.PositionId, info.XRobot, info.YRobot);
            return true;
        }

        private static bool TryParseInternal(string fileName, bool invertX, Regex regex, out ImageInfo info)
        {
            info = null;
            var m = regex.Match(fileName ?? string.Empty);
            if (!m.Success) return false;

            int id = 0;
            int group = 0;
            // [Codex] [Change time: 260318] [Initialize optional parser outputs from shared missing-value defaults]
//            int? position = null;
            int? position = MissingPositionId;

            if (m.Groups["id"].Success && !int.TryParse(m.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                return false;
            if (TryGetInt(m, "group", out var tempgroup))
                group = tempgroup;
            // [Codex] [Change time: 260318] [Use parser helpers that return consistent missing sentinels when fields are absent or marked none/nan]
//            if (TryGetNullableInt(m, "position", out var tempPosition))
//                position = tempPosition;
            position = GetOptionalPosition(m, "position");

//            double x = double.NaN;
//            double y = double.NaN;
//            if (TryGetDouble(m, "x", out var px)) x = px;
//            if (TryGetDouble(m, "y", out var py)) y = py;
            double x = GetOptionalCoordinate(m, "x");
            double y = GetOptionalCoordinate(m, "y");

            if (!double.IsNaN(x) && invertX) x = -x;

            info = new ImageInfo(fileName, group, id, position, x, y);
            return true;
        }


        // [Codex] [Change time: 260318] [Normalize missing optional position/x/y values through one parser convention]
        private static int? GetOptionalPosition(Match match, string name)
        {
            return TryGetNullableInt(match, name, out var value)
                ? value
                : MissingPositionId;
        }

        private static double GetOptionalCoordinate(Match match, string name)
        {
            return TryGetDouble(match, name, out var value)
                ? value
                : MissingCoordinate;
        }

        private static bool TryGetInt(Match match, string name, out int value)
        {
            value = 0;
            return match.Groups[name].Success && int.TryParse(match.Groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetNullableInt(Match match, string name, out int? value)
        {
            // [Codex] [Change time: 260318] [Keep absent position groups aligned with the shared missing-position sentinel]
//            value = null;
            value = MissingPositionId;
            if (!match.Groups[name].Success)
                return false;

            var raw = (match.Groups[name].Value ?? string.Empty).Trim();
            if (string.Equals(raw, "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "nan", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        private static bool TryGetDouble(Match match, string name, out double value)
        {
            // [Codex] [Change time: 260318] [Keep absent coordinate groups aligned with the shared missing-coordinate sentinel]
//            value = 0;
            value = MissingCoordinate;
            if (!match.Groups[name].Success)
                return false;

            var raw = (match.Groups[name].Value ?? string.Empty).Trim();
            if (string.Equals(raw, "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "nan", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(raw))
            {
                // [Codex] [Change time: 260318] [Reuse the shared missing-coordinate sentinel for blank/none/nan tokens]
//                value = double.NaN;
                value = MissingCoordinate;
                return true;
            }

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
