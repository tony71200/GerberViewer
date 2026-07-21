using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StitchingImage.Stitch_Tools.Utils
{
    public sealed class AppSettings
    {
        public string LastFolderPath { get; set; } = string.Empty;
        public string FilenamePattern { get; set; } = string.Empty;
        public StitchingConfig MatchConfig { get; set; } = new StitchingConfig();
        public SystemConfig SystemConfig { get; set; } = new SystemConfig();
        public int NodeInterval { get; set; } = 21;

        public static string SettingsDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StitchingOrderViewer48");

        private static string SettingsPath => Path.Combine(SettingsDir, "last_folder.txt");
        private static string MatchConfigPath => Path.Combine(SettingsDir, "match_config.yaml");
        private static string FilenamePatternPath => Path.Combine(SettingsDir, "filename_pattern.txt");
        private static string LegacyMatchConfigPath => Path.Combine(SettingsDir, "match_config.txt");
        public static string SystemConfigPath => Path.Combine(SettingsDir, "system_config.yaml");
        private static string NodeIntervalPath => Path.Combine(SettingsDir, "node_interval.txt");

        public static AppSettings Load()
        {
            var s = new AppSettings();

            try
            {
                if (File.Exists(SettingsPath))
                {
                    var txt = File.ReadAllText(SettingsPath).Trim();
                    // [Codex] [Change time: 260324] [Keep persisted folder path visible in UI even when directory is temporarily unavailable]
                    // if (!string.IsNullOrWhiteSpace(txt) && Directory.Exists(txt))
                    if (!string.IsNullOrWhiteSpace(txt))
                        s.LastFolderPath = txt;
                }
            }
            catch
            {
                // default
            }

            s.MatchConfig = LoadMatchConfig();
            s.SystemConfig = LoadSystemConfig();
            s.FilenamePattern = LoadFilenamePattern();
            s.NodeInterval = LoadNodeInterval();

            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                File.WriteAllText(SettingsPath, LastFolderPath ?? string.Empty);
            }
            catch
            {
                // ignore
            }

            SaveMatchConfig(MatchConfig);
            SaveSystemConfig(SystemConfig);
            SaveFilenamePattern(FilenamePattern);
            SaveNodeInterval(NodeInterval);
        }

        private static string LoadFilenamePattern()
        {
            try
            {
                if (File.Exists(FilenamePatternPath))
                    return File.ReadAllText(FilenamePatternPath).Trim();
            }
            catch
            {
            }

            return string.Empty;
        }

        private static void SaveFilenamePattern(string pattern)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                File.WriteAllText(FilenamePatternPath, pattern ?? string.Empty);
            }
            catch
            {
            }
        }

        // [Codex] [Change time: 260323] [Persist manual NodeInterval alongside the existing app settings files]
        private static int LoadNodeInterval()
        {
            try
            {
                if (File.Exists(NodeIntervalPath))
                {
                    var raw = File.ReadAllText(NodeIntervalPath).Trim();
                    if (int.TryParse(raw, out var value) && value > 0)
                        return value;
                }
            }
            catch
            {
            }

            return 21;
        }

        // [Codex] [Change time: 260323] [Persist manual NodeInterval alongside the existing app settings files]
        private static void SaveNodeInterval(int nodeInterval)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                File.WriteAllText(NodeIntervalPath, Math.Max(1, nodeInterval).ToString());
            }
            catch
            {
            }
        }

        private static StitchingConfig LoadMatchConfig()
        {
            if (File.Exists(MatchConfigPath))
                return StitchingConfig.LoadFromYaml(MatchConfigPath);

            if (File.Exists(LegacyMatchConfigPath))
                return LoadLegacyMatchConfig(LegacyMatchConfigPath);

            return new StitchingConfig();
        }

        private static StitchingConfig LoadLegacyMatchConfig(string path)
        {
            var cfg = new StitchingConfig();
            if (!File.Exists(path))
                return cfg;

            try
            {
                var lines = File.ReadAllLines(path);
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                        continue;

                    var key = parts[0].Trim();
                    var val = parts[1].Trim();
                    cfg.UpdateValue(key, val, out _);
                }
            }
            catch
            {
                return cfg;
            }

            return cfg;
        }

        private static void SaveMatchConfig(StitchingConfig cfg)
        {
            if (cfg == null)
                return;

            cfg.SaveToYaml(MatchConfigPath);
        }

        private static SystemConfig LoadSystemConfig()
        {
            if (File.Exists(SystemConfigPath))
                return SystemConfig.LoadFromYaml(SystemConfigPath);

            return new SystemConfig();
        }

        private static void SaveSystemConfig(SystemConfig cfg)
        {
            if (cfg == null)
                return;

            cfg.SaveToYaml(SystemConfigPath);
        }
    }
}
