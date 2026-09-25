using System;
using System.IO;
using PirateGame.Persistence;
using UnityEngine;

namespace PirateGame.Composition
{
    public enum LaunchMode { Auto, Continue, NewCampaign }

    // Hand-off from the title menu to the world scene, plus command-line overrides
    // used by automated runs: -save-dir <path>, -seed <int>.
    public static class LaunchOptions
    {
        public const string TitleScene = "Bootstrap";
        public const string WorldScene = "OceanWorld";
        public static LaunchMode Mode = LaunchMode.Auto;
        public static string SaveDirectoryOverride;
        public static int? SeedOverride;

        public static string SaveDirectory =>
            SaveDirectoryOverride ?? Argument("-save-dir") ?? Path.Combine(Application.persistentDataPath, "Saves");

        public static bool HasSaveFiles =>
            File.Exists(Path.Combine(SaveDirectory, JsonSaveStore.MainName)) ||
            File.Exists(Path.Combine(SaveDirectory, JsonSaveStore.BackupName));

        public static int? Seed
        {
            get
            {
                if (SeedOverride.HasValue) return SeedOverride;
                var text = Argument("-seed");
                return text != null && int.TryParse(text, out var value) ? value : (int?)null;
            }
        }

        public static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        public static bool Flag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
    }
}
