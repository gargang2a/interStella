#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace InterStella.EditorTools
{
    public static class InterStellaBuildSmoke
    {
        private const string DEFAULT_OUTPUT_ARGUMENT = "interstella-build-output";
        private const string DEFAULT_DEVELOPMENT_ARGUMENT = "interstella-build-development";
        private const string DEFAULT_OUTPUT_PATH = "Builds/SteamSmokeWindows64/interStella-Smoke.exe";
        private const string STEAM_APP_ID_FILE = "steam_appid.txt";
        private static readonly string[] BUILD_SCENES =
        {
            "Assets/Game/Scenes/VerticalSlice/VerticalSlice_MVP.unity"
        };

        [MenuItem("Tools/InterStella/Build/Build Steam Smoke Windows64")]
        public static void BuildSteamSmokeWindows64FromMenu()
        {
            BuildSteamSmokeWindows64(DEFAULT_OUTPUT_PATH, developmentBuild: true);
        }

        /// <summary>
        /// Command-line entry point for Unity batchmode builds.
        /// Optional args:
        /// -interstella-build-output <path>
        /// -interstella-build-development 0|1
        /// </summary>
        public static void BuildSteamSmokeWindows64FromCommandLine()
        {
            string outputPath = ReadCommandLineValue(DEFAULT_OUTPUT_ARGUMENT);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = DEFAULT_OUTPUT_PATH;
            }

            bool developmentBuild = ReadCommandLineFlag(DEFAULT_DEVELOPMENT_ARGUMENT, defaultValue: true);
            BuildSteamSmokeWindows64(outputPath, developmentBuild);
        }

        private static void BuildSteamSmokeWindows64(string outputPath, bool developmentBuild)
        {
            string normalizedOutputPath = NormalizeOutputPath(outputPath);
            string outputDirectory = Path.GetDirectoryName(normalizedOutputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOperationException("Build output directory could not be resolved.");
            }

            Directory.CreateDirectory(outputDirectory);

            BuildOptions buildOptions = BuildOptions.CompressWithLz4;
            if (developmentBuild)
            {
                buildOptions |= BuildOptions.Development;
                buildOptions |= BuildOptions.AllowDebugging;
            }

            BuildPlayerOptions playerOptions = new BuildPlayerOptions
            {
                scenes = BUILD_SCENES,
                locationPathName = normalizedOutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Steam smoke build failed. result={summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}, output={normalizedOutputPath}");
            }

            CopySteamAppIdFile(outputDirectory);
            Debug.Log(
                $"[InterStella][BuildSmoke] Windows64 smoke build succeeded. output={normalizedOutputPath}, size={summary.totalSize}, warnings={summary.totalWarnings}, duration={summary.totalTime}.");
        }

        private static void CopySteamAppIdFile(string outputDirectory)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Project root could not be resolved for steam_appid.txt copy.");
            }

            string sourcePath = Path.Combine(projectRoot, STEAM_APP_ID_FILE);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("steam_appid.txt was not found.", sourcePath);
            }

            string targetPath = Path.Combine(outputDirectory, STEAM_APP_ID_FILE);
            File.Copy(sourcePath, targetPath, overwrite: true);
        }

        private static string NormalizeOutputPath(string outputPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            if (Path.IsPathRooted(outputPath))
            {
                return outputPath;
            }

            return Path.GetFullPath(Path.Combine(projectRoot, outputPath));
        }

        private static string ReadCommandLineValue(string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return string.Empty;
            }

            string token = "-" + argumentName;
            string equalsToken = token + "=";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith(equalsToken, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(equalsToken.Length).Trim();
                }

                if (string.Equals(arg, token, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    string nextValue = args[i + 1];
                    if (!nextValue.StartsWith("-", StringComparison.Ordinal))
                    {
                        return nextValue.Trim();
                    }
                }
            }

            return string.Empty;
        }

        private static bool ReadCommandLineFlag(string argumentName, bool defaultValue)
        {
            string rawValue = ReadCommandLineValue(argumentName);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            switch (rawValue.Trim().ToLowerInvariant())
            {
                case "0":
                case "false":
                case "off":
                case "no":
                    return false;
                case "1":
                case "true":
                case "on":
                case "yes":
                    return true;
                default:
                    return defaultValue;
            }
        }
    }
}
#endif
