#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
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
        private const string BUILD_INFO_FILE = "build-info.txt";
        private const string CURRENT_LOBBY_FILE = "current-steam-lobby.txt";
        private const string HOST_LAUNCHER_FILE = "RunHost.bat";
        private const string CLIENT_LAUNCHER_FILE = "RunClient.bat";
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
            CreateLauncherBatchFiles(outputDirectory, Path.GetFileName(normalizedOutputPath));
            WriteBuildInfoFile(outputDirectory, normalizedOutputPath, summary, developmentBuild);
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

        private static void CreateLauncherBatchFiles(string outputDirectory, string executableFileName)
        {
            string hostLauncherPath = Path.Combine(outputDirectory, HOST_LAUNCHER_FILE);
            string clientLauncherPath = Path.Combine(outputDirectory, CLIENT_LAUNCHER_FILE);

            File.WriteAllText(hostLauncherPath, BuildHostLauncherContents(executableFileName), new UTF8Encoding(false));
            File.WriteAllText(clientLauncherPath, BuildClientLauncherContents(executableFileName), new UTF8Encoding(false));
        }

        private static void WriteBuildInfoFile(string outputDirectory, string normalizedOutputPath, BuildSummary summary, bool developmentBuild)
        {
            string buildInfoPath = Path.Combine(outputDirectory, BUILD_INFO_FILE);
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            ResolveGitBranchAndCommit(projectRoot, out string branchName, out string commitHash);

            string[] lines =
            {
                "build_local=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                "build_utc=" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
                "unity_version=" + Application.unityVersion,
                "development_build=" + developmentBuild,
                "output_path=" + normalizedOutputPath,
                "output_size_bytes=" + summary.totalSize,
                "build_duration=" + summary.totalTime,
                "branch=" + branchName,
                "commit=" + commitHash,
                "scene_0=" + BUILD_SCENES[0]
            };

            File.WriteAllLines(buildInfoPath, lines, new UTF8Encoding(false));
        }

        private static string BuildHostLauncherContents(string executableFileName)
        {
            string newLine = Environment.NewLine;
            return string.Join(newLine, new[]
            {
                "@echo off",
                "setlocal",
                "set \"EXE=%~dp0" + executableFileName + "\"",
                "set \"LOG=%~dp0steam-build-host.log\"",
                "set \"LOBBY_FILE=%~dp0" + CURRENT_LOBBY_FILE + "\"",
                "if not exist \"%EXE%\" (",
                "  echo Build executable not found: \"%EXE%\"",
                "  pause",
                "  exit /b 1",
                ")",
                "start \"interStella Host\" \"%EXE%\" -interstella-provider steam -interstella-steam-strict-relay 1 -interstella-mode host -interstella-address 127.0.0.1 -interstella-port 7770 -logFile \"%LOG%\"",
                "echo Host launched.",
                "echo Log: \"%LOG%\"",
                "echo Shared lobby file: \"%LOBBY_FILE%\"",
                "echo After the lobby is created, the latest lobbyId will be written to the shared lobby file.",
                "pause"
            }) + newLine;
        }

        private static string BuildClientLauncherContents(string executableFileName)
        {
            string newLine = Environment.NewLine;
            return string.Join(newLine, new[]
            {
                "@echo off",
                "setlocal",
                "set \"EXE=%~dp0" + executableFileName + "\"",
                "set \"LOG=%~dp0steam-build-client.log\"",
                "set \"LOBBY_FILE=%~dp0" + CURRENT_LOBBY_FILE + "\"",
                "set \"LOBBY_ID=%~1\"",
                "if \"%LOBBY_ID%\"==\"\" (",
                "  if exist \"%LOBBY_FILE%\" (",
                "    for /f \"usebackq tokens=1,* delims==\" %%A in (\"%LOBBY_FILE%\") do (",
                "      if /I \"%%A\"==\"lobby_id\" set \"LOBBY_ID=%%B\"",
                "    )",
                "  )",
                ")",
                "if \"%LOBBY_ID%\"==\"\" (",
                "  echo Shared lobby file was not found or does not contain lobby_id.",
                "  set /p LOBBY_ID=Enter lobbyId: ",
                ")",
                "if \"%LOBBY_ID%\"==\"\" (",
                "  echo lobbyId is required.",
                "  pause",
                "  exit /b 1",
                ")",
                "if not exist \"%EXE%\" (",
                "  echo Build executable not found: \"%EXE%\"",
                "  pause",
                "  exit /b 1",
                ")",
                "start \"interStella Client\" \"%EXE%\" -interstella-provider steam -interstella-steam-strict-relay 1 -interstella-mode client -interstella-address 127.0.0.1 -interstella-port 7770 +connect_lobby %LOBBY_ID% -logFile \"%LOG%\"",
                "echo Client launched with lobbyId %LOBBY_ID%.",
                "echo Shared lobby file: \"%LOBBY_FILE%\"",
                "echo Log: \"%LOG%\"",
                "pause"
            }) + newLine;
        }

        private static void ResolveGitBranchAndCommit(string projectRoot, out string branchName, out string commitHash)
        {
            branchName = "unknown";
            commitHash = "unknown";

            string gitDirectory = ResolveGitDirectory(projectRoot);
            if (string.IsNullOrWhiteSpace(gitDirectory))
            {
                return;
            }

            string headPath = Path.Combine(gitDirectory, "HEAD");
            if (!File.Exists(headPath))
            {
                return;
            }

            string headContents = File.ReadAllText(headPath).Trim();
            if (string.IsNullOrWhiteSpace(headContents))
            {
                return;
            }

            if (!headContents.StartsWith("ref: ", StringComparison.OrdinalIgnoreCase))
            {
                commitHash = headContents;
                return;
            }

            string referencePath = headContents.Substring(5).Trim();
            branchName = referencePath.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)
                ? referencePath.Substring("refs/heads/".Length)
                : referencePath;

            string referenceFilePath = Path.Combine(gitDirectory, referencePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(referenceFilePath))
            {
                string referenceCommit = File.ReadAllText(referenceFilePath).Trim();
                if (!string.IsNullOrWhiteSpace(referenceCommit))
                {
                    commitHash = referenceCommit;
                }
            }
        }

        private static string ResolveGitDirectory(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return string.Empty;
            }

            string gitPath = Path.Combine(projectRoot, ".git");
            if (Directory.Exists(gitPath))
            {
                return gitPath;
            }

            if (!File.Exists(gitPath))
            {
                return string.Empty;
            }

            string pointerContents = File.ReadAllText(gitPath).Trim();
            const string PREFIX = "gitdir:";
            if (!pointerContents.StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            string relativePath = pointerContents.Substring(PREFIX.Length).Trim();
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            return Path.GetFullPath(Path.Combine(projectRoot, relativePath));
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
