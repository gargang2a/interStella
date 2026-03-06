#if UNITY_EDITOR
using System;
using InterStella.Game.Features.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InterStella.EditorTools
{
    public static class InterStellaClientAutoPlayBootstrap
    {
        private const string VERTICAL_SLICE_SCENE_PATH = "Assets/Game/Scenes/VerticalSlice/VerticalSlice_MVP.unity";
        private const string AUTO_INTERACT_ARGUMENT = "interstella-auto-interact";
        private const string AUTO_INTERACT_COUNT_ARGUMENT = "interstella-auto-interact-count";
        private const string AUTO_INTERACT_SESSION_KEY = "interstella.client.autoInteractRequested";
        private const string AUTO_INTERACT_COUNT_SESSION_KEY = "interstella.client.autoInteractSuccessTarget";
        private const int MAX_AUTO_INTERACT_ATTEMPTS = 24;
        private const int DEFAULT_AUTO_INTERACT_SUCCESS_TARGET = 1;
        private const int MAX_AUTO_INTERACT_SUCCESS_TARGET = 8;
        private const float AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS = 0.5f;

        private static bool _autoInteractRequested;
        private static int _autoInteractAttempts;
        private static int _autoInteractSuccessCount;
        private static int _autoInteractSuccessTarget = DEFAULT_AUTO_INTERACT_SUCCESS_TARGET;
        private static double _nextAutoInteractAttemptTime;
        private static bool _autoInteractLoopRegistered;

        [InitializeOnLoadMethod]
        private static void InitializeHooks()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            _autoInteractRequested = SessionState.GetBool(AUTO_INTERACT_SESSION_KEY, false);
            _autoInteractSuccessTarget = Mathf.Clamp(
                SessionState.GetInt(AUTO_INTERACT_COUNT_SESSION_KEY, DEFAULT_AUTO_INTERACT_SUCCESS_TARGET),
                DEFAULT_AUTO_INTERACT_SUCCESS_TARGET,
                MAX_AUTO_INTERACT_SUCCESS_TARGET);
            if (_autoInteractRequested && EditorApplication.isPlaying && !_autoInteractLoopRegistered)
            {
                BeginAutoInteractLoop();
            }
        }

        /// <summary>
        /// Command line entrypoint for Unity `-executeMethod`.
        /// Opens the vertical slice scene and enters play mode so a client-only editor can auto-connect.
        /// </summary>
        public static void StartClientPlay()
        {
            _autoInteractRequested = HasCommandLineFlag(AUTO_INTERACT_ARGUMENT);
            _autoInteractSuccessTarget = _autoInteractRequested
                ? ResolveAutoInteractSuccessTargetFromArgs()
                : DEFAULT_AUTO_INTERACT_SUCCESS_TARGET;
            SessionState.SetBool(AUTO_INTERACT_SESSION_KEY, _autoInteractRequested);
            SessionState.SetInt(AUTO_INTERACT_COUNT_SESSION_KEY, _autoInteractSuccessTarget);

            EnterPlayModeNow();
        }

        private static void EnterPlayModeNow()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(VERTICAL_SLICE_SCENE_PATH))
            {
                Debug.LogError($"[interStella] ClientAutoPlayBootstrap failed: scene not found at '{VERTICAL_SLICE_SCENE_PATH}'.");
                return;
            }

            EditorSceneManager.OpenScene(VERTICAL_SLICE_SCENE_PATH, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
            Debug.Log("[interStella] ClientAutoPlayBootstrap entered play mode.");
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            _autoInteractRequested = SessionState.GetBool(AUTO_INTERACT_SESSION_KEY, false);
            _autoInteractSuccessTarget = Mathf.Clamp(
                SessionState.GetInt(AUTO_INTERACT_COUNT_SESSION_KEY, DEFAULT_AUTO_INTERACT_SUCCESS_TARGET),
                DEFAULT_AUTO_INTERACT_SUCCESS_TARGET,
                MAX_AUTO_INTERACT_SUCCESS_TARGET);

            if (stateChange == PlayModeStateChange.ExitingPlayMode || stateChange == PlayModeStateChange.EnteredEditMode)
            {
                StopAutoInteractLoop();
                if (stateChange == PlayModeStateChange.EnteredEditMode)
                {
                    _autoInteractRequested = false;
                    _autoInteractSuccessTarget = DEFAULT_AUTO_INTERACT_SUCCESS_TARGET;
                    SessionState.SetBool(AUTO_INTERACT_SESSION_KEY, false);
                    SessionState.SetInt(AUTO_INTERACT_COUNT_SESSION_KEY, DEFAULT_AUTO_INTERACT_SUCCESS_TARGET);
                }

                return;
            }

            if (!_autoInteractRequested)
            {
                return;
            }

            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                BeginAutoInteractLoop();
            }
        }

        private static void BeginAutoInteractLoop()
        {
            StopAutoInteractLoop();

            _autoInteractAttempts = 0;
            _autoInteractSuccessCount = 0;
            _nextAutoInteractAttemptTime = EditorApplication.timeSinceStartup + 1.25d;

            EditorApplication.update += TryAutoInteractFromOwner;
            _autoInteractLoopRegistered = true;
            Debug.Log($"[interStella] ClientAutoPlayBootstrap: auto-interact mode enabled. targetSuccesses={_autoInteractSuccessTarget}");
        }

        private static void StopAutoInteractLoop()
        {
            if (_autoInteractLoopRegistered)
            {
                EditorApplication.update -= TryAutoInteractFromOwner;
                _autoInteractLoopRegistered = false;
            }
        }

        private static void TryAutoInteractFromOwner()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextAutoInteractAttemptTime)
            {
                return;
            }

            _nextAutoInteractAttemptTime = EditorApplication.timeSinceStartup + AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS;
            _autoInteractAttempts++;

            PlayerInteraction ownerInteraction = FindOwnerInteraction();
            if (ownerInteraction == null)
            {
                if (_autoInteractAttempts >= MAX_AUTO_INTERACT_ATTEMPTS)
                {
                    Debug.LogWarning("[interStella] ClientAutoPlayBootstrap: owner interaction component was not found before timeout.");
                    StopAutoInteractLoopAndClearState();
                }

                return;
            }

            bool requestAccepted = ownerInteraction.TryInteract();
            if (requestAccepted)
            {
                _autoInteractSuccessCount++;
            }

            Debug.Log($"[interStella] ClientAutoPlayBootstrap: auto-interact attempt {_autoInteractAttempts}/{MAX_AUTO_INTERACT_ATTEMPTS}, accepted={requestAccepted}, successes={_autoInteractSuccessCount}/{_autoInteractSuccessTarget}, owner={ownerInteraction.name}");
            if (_autoInteractSuccessCount >= _autoInteractSuccessTarget)
            {
                StopAutoInteractLoopAndClearState();
                return;
            }

            if (_autoInteractAttempts >= MAX_AUTO_INTERACT_ATTEMPTS)
            {
                Debug.LogWarning($"[interStella] ClientAutoPlayBootstrap: auto-interact exhausted attempts. successes={_autoInteractSuccessCount}/{_autoInteractSuccessTarget}");
                StopAutoInteractLoopAndClearState();
            }
        }

        private static void StopAutoInteractLoopAndClearState()
        {
            _autoInteractRequested = false;
            _autoInteractSuccessTarget = DEFAULT_AUTO_INTERACT_SUCCESS_TARGET;
            SessionState.SetBool(AUTO_INTERACT_SESSION_KEY, false);
            SessionState.SetInt(AUTO_INTERACT_COUNT_SESSION_KEY, DEFAULT_AUTO_INTERACT_SUCCESS_TARGET);
            StopAutoInteractLoop();
        }

        private static PlayerInteraction FindOwnerInteraction()
        {
            PlayerInteraction[] interactions = UnityEngine.Object.FindObjectsOfType<PlayerInteraction>();
            for (int i = 0; i < interactions.Length; i++)
            {
                PlayerInteraction interaction = interactions[i];
                if (interaction == null || !interaction.TryGetComponent(out PlayerNetworkBridge bridge))
                {
                    continue;
                }

                if (bridge.IsAuthoritativeOwner)
                {
                    return interaction;
                }
            }

            return null;
        }

        private static bool HasCommandLineFlag(string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return false;
            }

            string flag = $"-{argumentName}";
            string equalsFlag = $"{flag}=";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    {
                        string nextValue = args[i + 1].Trim();
                        if (string.IsNullOrEmpty(nextValue))
                        {
                            return true;
                        }

                        return !string.Equals(nextValue, "0", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(nextValue, "false", StringComparison.OrdinalIgnoreCase);
                    }

                    return true;
                }

                if (arg.StartsWith(equalsFlag, StringComparison.OrdinalIgnoreCase))
                {
                    string value = arg.Substring(equalsFlag.Length).Trim();
                    if (string.IsNullOrEmpty(value))
                    {
                        return true;
                    }

                    return !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        private static int ResolveAutoInteractSuccessTargetFromArgs()
        {
            string rawCount = ReadCommandLineValue(AUTO_INTERACT_COUNT_ARGUMENT);
            if (!string.IsNullOrWhiteSpace(rawCount) && int.TryParse(rawCount, out int parsedCount))
            {
                return Mathf.Clamp(parsedCount, DEFAULT_AUTO_INTERACT_SUCCESS_TARGET, MAX_AUTO_INTERACT_SUCCESS_TARGET);
            }

            return DEFAULT_AUTO_INTERACT_SUCCESS_TARGET;
        }

        private static string ReadCommandLineValue(string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return string.Empty;
            }

            string flag = $"-{argumentName}";
            string equalsFlag = $"{flag}=";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith(equalsFlag, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(equalsFlag.Length).Trim();
                }

                if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
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
    }
}
#endif
