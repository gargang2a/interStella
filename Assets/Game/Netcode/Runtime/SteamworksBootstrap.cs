using System;
using Steamworks;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SteamworksBootstrap : MonoBehaviour
    {
        [SerializeField]
        private uint _appId = 480;

        [SerializeField]
        private bool _initializeOnAwake = true;

        [SerializeField]
        private bool _restartAppIfNecessary;

        [SerializeField]
        private bool _runCallbacksInUpdate = true;

        [SerializeField]
        private bool _persistAcrossScenes;

        [SerializeField]
        private bool _logInitializationEvents = true;

        [SerializeField]
        private bool _logCallbackExceptions;

        [SerializeField]
        private bool _isInitialized;

        [SerializeField]
        private ulong _localSteamId;

        [SerializeField]
        private string _lastInitError = string.Empty;

        private bool _ownsShutdown;

        public uint AppId => _appId;
        public bool IsInitialized => _isInitialized;
        public ulong LocalSteamId => _localSteamId;
        public string LocalSteamIdString => _localSteamId > 0ul ? _localSteamId.ToString() : string.Empty;
        public string LastInitError => _lastInitError;

        private void Awake()
        {
            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (_initializeOnAwake)
            {
                TryInitialize();
            }
        }

        private void Update()
        {
            if (!_isInitialized || !_runCallbacksInUpdate)
            {
                return;
            }

            try
            {
                SteamAPI.RunCallbacks();
            }
            catch (Exception exception)
            {
                if (_logCallbackExceptions)
                {
                    Debug.LogWarning($"[SteamworksBootstrap] SteamAPI.RunCallbacks failed: {exception.Message}");
                }
            }
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        public bool TryInitialize()
        {
            if (_isInitialized)
            {
                return true;
            }

            _lastInitError = string.Empty;

            try
            {
                if (!Packsize.Test())
                {
                    return Fail("Steamworks.NET Packsize.Test failed.");
                }

                if (!DllCheck.Test())
                {
                    return Fail("Steamworks.NET DllCheck.Test failed.");
                }

                if (_restartAppIfNecessary && !Application.isEditor && SteamAPI.RestartAppIfNecessary(new AppId_t(_appId)))
                {
                    return Fail($"Steam requested relaunch for appId={_appId}.");
                }

                ESteamAPIInitResult initResult = SteamAPI.InitEx(out string errorMessage);
                if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    string failureReason = string.IsNullOrWhiteSpace(errorMessage)
                        ? $"SteamAPI.InitEx failed with {initResult}."
                        : errorMessage;
                    return Fail(failureReason);
                }

                _isInitialized = true;
                _ownsShutdown = true;
                _localSteamId = SteamUser.GetSteamID().m_SteamID;

                if (_logInitializationEvents)
                {
                    Debug.Log($"[SteamworksBootstrap] Initialized Steamworks. appId={_appId}, steamId={_localSteamId}.");
                }

                return true;
            }
            catch (Exception exception)
            {
                return Fail($"Steam bootstrap exception: {exception.Message}");
            }
        }

        public void Shutdown()
        {
            if (!_isInitialized || !_ownsShutdown)
            {
                return;
            }

            try
            {
                SteamAPI.Shutdown();
            }
            catch (Exception exception)
            {
                if (_logInitializationEvents)
                {
                    Debug.LogWarning($"[SteamworksBootstrap] Steam shutdown raised an exception: {exception.Message}");
                }
            }
            finally
            {
                _isInitialized = false;
                _ownsShutdown = false;
                _localSteamId = 0ul;
            }
        }

        public bool TryGetLocalSteamIdString(out string steamId)
        {
            if (!_isInitialized)
            {
                steamId = string.Empty;
                return false;
            }

            steamId = LocalSteamIdString;
            return !string.IsNullOrWhiteSpace(steamId);
        }

        private bool Fail(string reason)
        {
            _isInitialized = false;
            _ownsShutdown = false;
            _localSteamId = 0ul;
            _lastInitError = reason ?? "Unknown Steam bootstrap failure.";

            if (_logInitializationEvents)
            {
                Debug.LogWarning($"[SteamworksBootstrap] {_lastInitError}");
            }

            return false;
        }
    }
}
