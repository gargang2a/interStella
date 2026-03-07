using System;
using FishNet.Managing;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class FishNetSessionService : MonoBehaviour, ISessionService
    {
        private enum ConnectionProvider
        {
            Direct = 0,
            SteamRelay = 1
        }

        private enum StartupMode
        {
            Host = 0,
            ClientOnly = 1,
            ServerOnly = 2
        }

        [SerializeField]
        private NetworkManager _networkManager;

        [SerializeField]
        private StartupMode _startupMode = StartupMode.Host;

        [SerializeField]
        private string _clientAddress = "127.0.0.1";

        [SerializeField]
        private ushort _port = 7770;

        [Header("Connection Provider")]
        [SerializeField]
        private ConnectionProvider _connectionProvider = ConnectionProvider.Direct;

        [SerializeField]
        private string _steamLobbyId = string.Empty;

        [SerializeField]
        private string _steamHostId = string.Empty;

        [SerializeField]
        private bool _allowSteamFallbackToDirect = false;

        [Header("Runtime Override")]
        [SerializeField]
        private bool _allowRuntimeOverride = true;

        [SerializeField]
        private string _modeArgument = "interstella-mode";

        [SerializeField]
        private string _addressArgument = "interstella-address";

        [SerializeField]
        private string _portArgument = "interstella-port";

        [SerializeField]
        private string _providerArgument = "interstella-provider";

        [SerializeField]
        private string _lobbyIdArgument = "interstella-lobby-id";

        [SerializeField]
        private string _steamHostIdArgument = "interstella-steam-host-id";

        public bool IsSessionActive
        {
            get
            {
                if (_networkManager == null)
                {
                    return false;
                }

                switch (_startupMode)
                {
                    case StartupMode.ClientOnly:
                        return _networkManager.IsClientStarted;
                    case StartupMode.ServerOnly:
                        return _networkManager.IsServerStarted;
                    default:
                        return _networkManager.IsHostStarted;
                }
            }
        }

        public bool IsHost => _startupMode == StartupMode.Host || _startupMode == StartupMode.ServerOnly;

        private void Awake()
        {
            ResolveNetworkManagerIfMissing();
            ApplyRuntimeOverrides();
        }

        public bool StartSession()
        {
            ResolveNetworkManagerIfMissing();
            if (_networkManager == null)
            {
                return false;
            }

            ApplyRuntimeOverrides();
            if (_connectionProvider == ConnectionProvider.SteamRelay)
            {
                Debug.LogWarning($"[FishNetSessionService] Steam relay bootstrap requested. lobbyId={_steamLobbyId}, hostId={_steamHostId}. Steam transport wiring is not implemented yet.");
                if (!_allowSteamFallbackToDirect)
                {
                    Debug.LogWarning("[FishNetSessionService] Session start blocked because SteamRelay is selected and direct fallback is disabled.");
                    return false;
                }

                Debug.LogWarning("[FishNetSessionService] Falling back to direct endpoint because _allowSteamFallbackToDirect is enabled.");
            }

            Debug.Log($"[FishNetSessionService] Starting session provider={_connectionProvider}, mode={_startupMode}, address={_clientAddress}, port={_port}.");

            switch (_startupMode)
            {
                case StartupMode.ClientOnly:
                    return _networkManager.ClientManager.StartConnection(_clientAddress, _port);
                case StartupMode.ServerOnly:
                    return _networkManager.ServerManager.StartConnection(_port);
                default:
                    bool serverStarted = _networkManager.ServerManager.StartConnection(_port);
                    bool clientStarted = _networkManager.ClientManager.StartConnection(_clientAddress, _port);
                    return serverStarted && clientStarted;
            }
        }

        public void StopSession()
        {
            if (_networkManager == null)
            {
                return;
            }

            if (_networkManager.IsClientStarted)
            {
                _networkManager.ClientManager.StopConnection();
            }

            if (_networkManager.IsServerStarted)
            {
                _networkManager.ServerManager.StopConnection(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveNetworkManagerIfMissing();
        }
#endif

        private void ApplyRuntimeOverrides()
        {
            if (!_allowRuntimeOverride)
            {
                return;
            }

            string modeValue = ReadRuntimeOverride(_modeArgument, "INTERSTELLA_MODE");
            if (!string.IsNullOrWhiteSpace(modeValue) && TryParseStartupMode(modeValue, out StartupMode runtimeMode))
            {
                _startupMode = runtimeMode;
            }

            string addressValue = ReadRuntimeOverride(_addressArgument, "INTERSTELLA_ADDRESS");
            if (!string.IsNullOrWhiteSpace(addressValue))
            {
                _clientAddress = addressValue.Trim();
            }

            string portValue = ReadRuntimeOverride(_portArgument, "INTERSTELLA_PORT");
            if (!string.IsNullOrWhiteSpace(portValue) && ushort.TryParse(portValue, out ushort runtimePort))
            {
                _port = runtimePort;
            }

            string providerValue = ReadRuntimeOverride(_providerArgument, "INTERSTELLA_PROVIDER");
            if (!string.IsNullOrWhiteSpace(providerValue) && TryParseConnectionProvider(providerValue, out ConnectionProvider provider))
            {
                _connectionProvider = provider;
            }

            string lobbyIdValue = ReadRuntimeOverride(_lobbyIdArgument, "INTERSTELLA_STEAM_LOBBY_ID");
            if (!string.IsNullOrWhiteSpace(lobbyIdValue))
            {
                _steamLobbyId = lobbyIdValue.Trim();
            }

            string steamHostIdValue = ReadRuntimeOverride(_steamHostIdArgument, "INTERSTELLA_STEAM_HOST_ID");
            if (!string.IsNullOrWhiteSpace(steamHostIdValue))
            {
                _steamHostId = steamHostIdValue.Trim();
            }
        }

        private static bool TryParseStartupMode(string rawValue, out StartupMode mode)
        {
            string normalized = rawValue.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "host":
                case "h":
                    mode = StartupMode.Host;
                    return true;
                case "client":
                case "clientonly":
                case "client-only":
                case "c":
                    mode = StartupMode.ClientOnly;
                    return true;
                case "server":
                case "serveronly":
                case "server-only":
                case "s":
                    mode = StartupMode.ServerOnly;
                    return true;
                default:
                    mode = StartupMode.Host;
                    return false;
            }
        }

        private static bool TryParseConnectionProvider(string rawValue, out ConnectionProvider provider)
        {
            string normalized = rawValue.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "direct":
                case "ip":
                case "socket":
                    provider = ConnectionProvider.Direct;
                    return true;
                case "steam":
                case "steamrelay":
                case "steam-relay":
                case "relay":
                    provider = ConnectionProvider.SteamRelay;
                    return true;
                default:
                    provider = ConnectionProvider.Direct;
                    return false;
            }
        }

        private static string ReadRuntimeOverride(string argumentName, string environmentVariableName)
        {
            string fromEnvironment = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return string.Empty;
            }

            string argumentToken = $"-{argumentName}";
            string equalsToken = $"{argumentToken}=";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith(equalsToken, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(equalsToken.Length);
                }

                if (string.Equals(arg, argumentToken, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }

        private void ResolveNetworkManagerIfMissing()
        {
            if (_networkManager == null)
            {
                _networkManager = FindObjectOfType<NetworkManager>();
            }
        }
    }
}
