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
        private const string AUTO_INTERACT_MAX_ATTEMPTS_ARGUMENT = "interstella-auto-interact-max-attempts";
        private const string AUTO_INTERACT_INITIAL_DELAY_ARGUMENT = "interstella-auto-interact-initial-delay";
        private const string AUTO_INTERACT_INTERVAL_ARGUMENT = "interstella-auto-interact-interval";
        private const string AUTO_INTERACT_SESSION_KEY = "interstella.client.autoInteractRequested";
        private const string AUTO_INTERACT_COUNT_SESSION_KEY = "interstella.client.autoInteractSuccessTarget";
        private const string AUTO_INTERACT_MAX_ATTEMPTS_SESSION_KEY = "interstella.client.autoInteractMaxAttempts";
        private const string AUTO_INTERACT_INITIAL_DELAY_SESSION_KEY = "interstella.client.autoInteractInitialDelaySec";
        private const string AUTO_INTERACT_INTERVAL_SESSION_KEY = "interstella.client.autoInteractIntervalSec";
        private const int MAX_AUTO_INTERACT_ATTEMPTS = 24;
        private const int MAX_AUTO_INTERACT_ATTEMPTS_LIMIT = 240;
        private const int DEFAULT_AUTO_INTERACT_SUCCESS_TARGET = 1;
        private const int MAX_AUTO_INTERACT_SUCCESS_TARGET = 8;
        private const float DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS = 1.25f;
        private const float DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS = 0.5f;
        private const float MIN_AUTO_INTERACT_INTERVAL_SECONDS = 0.1f;
        private const float MAX_AUTO_INTERACT_INTERVAL_SECONDS = 5f;
        private static bool _autoInteractRequested;
        private static int _autoInteractAttempts;
        private static int _autoInteractSuccessCount;
        private static int _autoInteractSuccessTarget = DEFAULT_AUTO_INTERACT_SUCCESS_TARGET;
        private static int _autoInteractMaxAttempts = MAX_AUTO_INTERACT_ATTEMPTS;
        private static float _autoInteractInitialDelaySeconds = DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS;
        private static float _autoInteractAttemptIntervalSeconds = DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS;
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
            _autoInteractMaxAttempts = Mathf.Clamp(
                SessionState.GetInt(AUTO_INTERACT_MAX_ATTEMPTS_SESSION_KEY, MAX_AUTO_INTERACT_ATTEMPTS),
                DEFAULT_AUTO_INTERACT_SUCCESS_TARGET,
                MAX_AUTO_INTERACT_ATTEMPTS_LIMIT);
            _autoInteractInitialDelaySeconds = Mathf.Clamp(
                SessionState.GetFloat(AUTO_INTERACT_INITIAL_DELAY_SESSION_KEY, DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS),
                0f,
                120f);
            _autoInteractAttemptIntervalSeconds = Mathf.Clamp(
                SessionState.GetFloat(AUTO_INTERACT_INTERVAL_SESSION_KEY, DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS),
                MIN_AUTO_INTERACT_INTERVAL_SECONDS,
                MAX_AUTO_INTERACT_INTERVAL_SECONDS);
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
            _autoInteractMaxAttempts = _autoInteractRequested
                ? ResolveAutoInteractMaxAttemptsFromArgs()
                : MAX_AUTO_INTERACT_ATTEMPTS;
            _autoInteractInitialDelaySeconds = _autoInteractRequested
                ? ResolveAutoInteractInitialDelayFromArgs()
                : DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS;
            _autoInteractAttemptIntervalSeconds = _autoInteractRequested
                ? ResolveAutoInteractIntervalFromArgs()
                : DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS;
            SessionState.SetBool(AUTO_INTERACT_SESSION_KEY, _autoInteractRequested);
            SessionState.SetInt(AUTO_INTERACT_COUNT_SESSION_KEY, _autoInteractSuccessTarget);
            SessionState.SetInt(AUTO_INTERACT_MAX_ATTEMPTS_SESSION_KEY, _autoInteractMaxAttempts);
            SessionState.SetFloat(AUTO_INTERACT_INITIAL_DELAY_SESSION_KEY, _autoInteractInitialDelaySeconds);
            SessionState.SetFloat(AUTO_INTERACT_INTERVAL_SESSION_KEY, _autoInteractAttemptIntervalSeconds);

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
            _autoInteractMaxAttempts = Mathf.Clamp(
                SessionState.GetInt(AUTO_INTERACT_MAX_ATTEMPTS_SESSION_KEY, MAX_AUTO_INTERACT_ATTEMPTS),
                DEFAULT_AUTO_INTERACT_SUCCESS_TARGET,
                MAX_AUTO_INTERACT_ATTEMPTS_LIMIT);
            _autoInteractInitialDelaySeconds = Mathf.Clamp(
                SessionState.GetFloat(AUTO_INTERACT_INITIAL_DELAY_SESSION_KEY, DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS),
                0f,
                120f);
            _autoInteractAttemptIntervalSeconds = Mathf.Clamp(
                SessionState.GetFloat(AUTO_INTERACT_INTERVAL_SESSION_KEY, DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS),
                MIN_AUTO_INTERACT_INTERVAL_SECONDS,
                MAX_AUTO_INTERACT_INTERVAL_SECONDS);

            if (stateChange == PlayModeStateChange.ExitingPlayMode || stateChange == PlayModeStateChange.EnteredEditMode)
            {
                StopAutoInteractLoop();
                if (stateChange == PlayModeStateChange.EnteredEditMode)
                {
                    _autoInteractRequested = false;
                    _autoInteractSuccessTarget = DEFAULT_AUTO_INTERACT_SUCCESS_TARGET;
                    _autoInteractMaxAttempts = MAX_AUTO_INTERACT_ATTEMPTS;
                    _autoInteractInitialDelaySeconds = DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS;
                    _autoInteractAttemptIntervalSeconds = DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS;
                    SessionState.SetBool(AUTO_INTERACT_SESSION_KEY, false);
                    SessionState.SetInt(AUTO_INTERACT_COUNT_SESSION_KEY, DEFAULT_AUTO_INTERACT_SUCCESS_TARGET);
                    SessionState.SetInt(AUTO_INTERACT_MAX_ATTEMPTS_SESSION_KEY, MAX_AUTO_INTERACT_ATTEMPTS);
                    SessionState.SetFloat(AUTO_INTERACT_INITIAL_DELAY_SESSION_KEY, DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS);
                    SessionState.SetFloat(AUTO_INTERACT_INTERVAL_SESSION_KEY, DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS);
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
            _nextAutoInteractAttemptTime = EditorApplication.timeSinceStartup + _autoInteractInitialDelaySeconds;

            EditorApplication.update += TryAutoInteractFromOwner;
            _autoInteractLoopRegistered = true;
            Debug.Log($"[interStella] ClientAutoPlayBootstrap: auto-interact mode enabled. targetSuccesses={_autoInteractSuccessTarget}, maxAttempts={_autoInteractMaxAttempts}, initialDelay={_autoInteractInitialDelaySeconds:F2}, interval={_autoInteractAttemptIntervalSeconds:F2}");
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

            _nextAutoInteractAttemptTime = EditorApplication.timeSinceStartup + _autoInteractAttemptIntervalSeconds;
            _autoInteractAttempts++;

            PlayerInteraction ownerInteraction = FindOwnerInteraction();
            if (ownerInteraction == null)
            {
                if (_autoInteractAttempts >= _autoInteractMaxAttempts)
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

            Debug.Log($"[interStella] ClientAutoPlayBootstrap: auto-interact attempt {_autoInteractAttempts}/{_autoInteractMaxAttempts}, accepted={requestAccepted}, successes={_autoInteractSuccessCount}/{_autoInteractSuccessTarget}, owner={ownerInteraction.name}");
            if (_autoInteractSuccessCount >= _autoInteractSuccessTarget)
            {
                StopAutoInteractLoopAndClearState();
                return;
            }

            if (_autoInteractAttempts >= _autoInteractMaxAttempts)
            {
                Debug.LogWarning($"[interStella] ClientAutoPlayBootstrap: auto-interact exhausted attempts. successes={_autoInteractSuccessCount}/{_autoInteractSuccessTarget}");
                StopAutoInteractLoopAndClearState();
            }
        }

        private static void StopAutoInteractLoopAndClearState()
        {
            _autoInteractRequested = false;
            _autoInteractSuccessTarget = DEFAULT_AUTO_INTERACT_SUCCESS_TARGET;
            _autoInteractMaxAttempts = MAX_AUTO_INTERACT_ATTEMPTS;
            _autoInteractInitialDelaySeconds = DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS;
            _autoInteractAttemptIntervalSeconds = DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS;
            SessionState.SetBool(AUTO_INTERACT_SESSION_KEY, false);
            SessionState.SetInt(AUTO_INTERACT_COUNT_SESSION_KEY, DEFAULT_AUTO_INTERACT_SUCCESS_TARGET);
            SessionState.SetInt(AUTO_INTERACT_MAX_ATTEMPTS_SESSION_KEY, MAX_AUTO_INTERACT_ATTEMPTS);
            SessionState.SetFloat(AUTO_INTERACT_INITIAL_DELAY_SESSION_KEY, DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS);
            SessionState.SetFloat(AUTO_INTERACT_INTERVAL_SESSION_KEY, DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS);
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

        private static int ResolveAutoInteractMaxAttemptsFromArgs()
        {
            string rawAttempts = ReadCommandLineValue(AUTO_INTERACT_MAX_ATTEMPTS_ARGUMENT);
            if (!string.IsNullOrWhiteSpace(rawAttempts) && int.TryParse(rawAttempts, out int parsedAttempts))
            {
                return Mathf.Clamp(parsedAttempts, DEFAULT_AUTO_INTERACT_SUCCESS_TARGET, MAX_AUTO_INTERACT_ATTEMPTS_LIMIT);
            }

            return MAX_AUTO_INTERACT_ATTEMPTS;
        }

        private static float ResolveAutoInteractInitialDelayFromArgs()
        {
            string rawDelay = ReadCommandLineValue(AUTO_INTERACT_INITIAL_DELAY_ARGUMENT);
            if (!string.IsNullOrWhiteSpace(rawDelay) && float.TryParse(rawDelay, out float parsedDelay))
            {
                return Mathf.Clamp(parsedDelay, 0f, 120f);
            }

            return DEFAULT_AUTO_INTERACT_INITIAL_DELAY_SECONDS;
        }

        private static float ResolveAutoInteractIntervalFromArgs()
        {
            string rawInterval = ReadCommandLineValue(AUTO_INTERACT_INTERVAL_ARGUMENT);
            if (!string.IsNullOrWhiteSpace(rawInterval) && float.TryParse(rawInterval, out float parsedInterval))
            {
                return Mathf.Clamp(parsedInterval, MIN_AUTO_INTERACT_INTERVAL_SECONDS, MAX_AUTO_INTERACT_INTERVAL_SECONDS);
            }

            return DEFAULT_AUTO_INTERACT_ATTEMPT_INTERVAL_SECONDS;
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
