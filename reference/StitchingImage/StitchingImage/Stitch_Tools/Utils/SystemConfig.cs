using StitchingImage.Stitch_Tools.Matcher;
using StitchingImage.Stitch_Tools.RobotManager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace StitchingImage.Stitch_Tools.Utils
{
    public enum SaveMode
    {
        Preview = 0,
        Full = 1
    }

    public sealed class SystemConfig : IEditableConfig, IReadOnlyConfigKeys
    {
        private static readonly string[] ConfigKeys =
        {
            nameof(RobotHost),
            nameof(RobotPort),
            nameof(ConnectAttempts),
            nameof(ConnectDelaySeconds),
            nameof(AutoLoadLastFolder),
            nameof(AutoLoadFolder),
            nameof(ClusterOrderMode),
            nameof(OrderMode),
            nameof(InvertX),
            nameof(StartCorner),
            nameof(RobotMovement),
            nameof(GapFactor),
            nameof(GapRow),
            nameof(SaveMode),
            nameof(ComposeMegapix),
        };

        private static readonly string[] ReadOnlyKeys =
        {
            nameof(OrderMode),
            nameof(ClusterOrderMode),
            nameof(InvertX),
            nameof(StartCorner),
            nameof(RobotMovement),
            nameof(SaveMode),
        };

        public string RobotHost { get; set; } = "127.0.0.1";
        public int RobotPort { get; set; } = 9000;
        public int ConnectAttempts { get; set; } = 5;
        public double ConnectDelaySeconds { get; set; } = 2.0;
        public bool AutoLoadLastFolder { get; set; } = true;
        public string AutoLoadFolder { get; set; } = "";
        public ClusterOrderMode ClusterOrderMode { get; set; } = ClusterOrderMode.Position;
        public OrderMode OrderMode { get; set; } = OrderMode.Branch;
        public bool InvertX { get; set; } = true;
        public StartCorner StartCorner { get; set; } = StartCorner.TopRight;
        public RobotMovement RobotMovement { get; set; } = RobotMovement.Left;
        public double GapFactor { get; set; } = 2.0d;
        public double GapRow { get; set; } = 0.6d;
        public SaveMode SaveMode { get; set; } = SaveMode.Preview;
        public double ComposeMegapix { get; set; } = 10.0d;
        

        public SystemConfig Clone() => (SystemConfig)MemberwiseClone();

        public IEnumerable<string> GetKeys() => ConfigKeys;

        public IEnumerable<string> GetReadOnlyKeys() => ReadOnlyKeys;

        public object GetValue(string key)
        {
            switch (key)
            {
                case nameof(RobotHost):
                    return RobotHost;
                case nameof(RobotPort):
                    return RobotPort;
                case nameof(ConnectAttempts):
                    return ConnectAttempts;
                case nameof(ConnectDelaySeconds):
                    return ConnectDelaySeconds;
                case nameof(AutoLoadLastFolder):
                    return AutoLoadLastFolder;
                case nameof(AutoLoadFolder):
                    return AutoLoadFolder;
                case nameof(ClusterOrderMode):
                    return ClusterOrderMode;
                case nameof(OrderMode):
                    return OrderMode;
                case nameof(InvertX):
                    return InvertX;
                case nameof(StartCorner):
                    return StartCorner;
                case nameof(RobotMovement):
                    return RobotMovement;
                case nameof(GapFactor):
                    return GapFactor;
                case nameof(GapRow):
                    return GapRow;
                case nameof(SaveMode):
                    return SaveMode;
                case nameof(ComposeMegapix):
                    return ComposeMegapix;
                default:
                    return string.Empty;
            }
        }

        public bool UpdateValue(string key, string rawValue, out string error)
        {
            error = null;
            try
            {
                switch (key)
                {
                    case nameof(RobotHost):
                        RobotHost = rawValue?.Trim() ?? string.Empty;
                        return true;
                    case nameof(RobotPort):
                        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                        {
                            RobotPort = port;
                            return true;
                        }
                        error = "Invalid RobotPort value.";
                        return false;
                    case nameof(ConnectAttempts):
                        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var attempts))
                        {
                            ConnectAttempts = attempts;
                            return true;
                        }
                        error = "Invalid ConnectAttempts value.";
                        return false;
                    case nameof(ConnectDelaySeconds):
                        if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay))
                        {
                            ConnectDelaySeconds = delay;
                            return true;
                        }
                        error = "Invalid ConnectDelaySeconds value.";
                        return false;
                    case nameof(AutoLoadLastFolder):
                        if (TryParseBool(rawValue, out var auto))
                        {
                            AutoLoadLastFolder = auto;
                            return true;
                        }
                        error = "Invalid AutoLoadLastFolder value.";
                        return false;
                    case nameof(AutoLoadFolder):
                        AutoLoadFolder = rawValue?.Trim() ?? string.Empty;
                        return true;
                    case nameof(ClusterOrderMode):
                        if (TryParseEnum(rawValue, out ClusterOrderMode clusterMode))
                        {
                            ClusterOrderMode = clusterMode;
                            return true;
                        }
                        error = "Invalid ClusterOrderMode value.";
                        return false;
                    case nameof(OrderMode):
                        if (TryParseEnum(rawValue, out OrderMode orderMode))
                        {
                            OrderMode = orderMode;
                            return true;
                        }
                        error = "Invalid Order Mode value.";
                        return false;
                    case nameof(InvertX):
                        if (TryParseBool(rawValue, out var invertX))
                        {
                            InvertX = invertX;
                            return true;
                        }
                        error = "Invalid InvertX value.";
                        return false;
                    case nameof(StartCorner):
                        if (TryParseEnum(rawValue, out StartCorner corner))
                        {
                            StartCorner = corner;
                            return true;
                        }
                        error = "Invalid StartCorner value.";
                        return false;
                    case nameof(RobotMovement):
                        if (TryParseEnum(rawValue, out RobotMovement movement))
                        {
                            RobotMovement = movement;
                            return true;
                        }
                        error = "Invalid RobotMovement value.";
                        return false;
                    case nameof(GapFactor):
                        if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var gap))
                        {
                            GapFactor = gap;
                            return true;
                        }
                        error = "Invalid GapFactor value.";
                        return false;
                    case nameof(GapRow):
                        if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var gapRow))
                        {
                            GapRow = gapRow;
                            return true;
                        }
                        error = "Invalid GapRow value.";
                        return false;
                    case nameof(SaveMode):
                        if (TryParseEnum(rawValue, out SaveMode saveMode))
                        {
                            SaveMode = saveMode;
                            return true;
                        }
                        error = "Invalid SaveMode value.";
                        return false;
                    case nameof(ComposeMegapix):
                        if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var composeMp))
                        {
                            ComposeMegapix = composeMp;
                            return true;
                        }
                        error = "Invalid ComposeMegapix value.";
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

        public static SystemConfig LoadFromYaml(string path)
        {
            var cfg = new SystemConfig();
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
            value = false;
            return false;
        }

        private static bool TryParseEnum<TEnum>(string rawValue, out TEnum value) where TEnum : struct
        {
            if (!string.IsNullOrWhiteSpace(rawValue) && Enum.TryParse(rawValue.Trim(), true, out value))
                return true;

            value = default;
            return false;
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            switch (value)
            {
                case bool b:
                    return b ? "true" : "false";
                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }
    }
}
