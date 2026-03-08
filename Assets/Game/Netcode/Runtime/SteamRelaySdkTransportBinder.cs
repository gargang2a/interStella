using System;
using System.Collections.Generic;
using System.Reflection;
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    [DefaultExecutionOrder(-40000)]
    public sealed class SteamRelaySdkTransportBinder : MonoBehaviour, ISteamRelayTransportBinder
    {
        private const BindingFlags INSTANCE_FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const string PROVIDER_ARGUMENT = "interstella-provider";
        private const string PROVIDER_ENVIRONMENT_VARIABLE = "INTERSTELLA_PROVIDER";

        [SerializeField]
        private bool _allowLoopbackFallbackUntilSdkReady = true;

        [SerializeField]
        private bool _applyStartupSelectionOnAwake = true;

        [SerializeField]
        private SteamRelayLoopbackTransportBinder _loopbackFallback;

        [SerializeField]
        private SteamworksBootstrap _steamworksBootstrap;

        [SerializeField]
        private FishNetSessionService _fishNetSessionService;

        [SerializeField]
        private TransportManager _transportManager;

        [SerializeField]
        private Tugboat _tugboatTransport;

        [SerializeField]
        private Multipass _multipassTransport;

        [SerializeField]
        private FishySteamworks.FishySteamworks _fishySteamworksTransport;

        [SerializeField]
        private bool _logBootstrapEvents = true;

        private bool _startupSelectionApplied;

        public bool TryApplyBootstrap(
            NetworkManager networkManager,
            bool isHostMode,
            string lobbyId,
            string hostId,
            ref string clientAddress,
            ref ushort port,
            out string details)
        {
            if (networkManager == null)
            {
                details = "NetworkManager is missing.";
                return false;
            }

            ResolveDependencies(networkManager);
            if (!EnsureTransportComponents(networkManager, out string transportSetupDetails))
            {
                details = transportSetupDetails;
                return false;
            }

            ApplyStartupTransportSelection();

            bool hostIdIsEndpoint = SteamRelayLoopbackTransportBinder.TryParseEndpoint(hostId, out _, out _);
            bool hostIdIsSteamId = TryParseSteamId(hostId, out _);

            if (_transportManager == null)
            {
                details = "TransportManager is missing.";
                return false;
            }

            if (_steamworksBootstrap == null || !_steamworksBootstrap.TryInitialize())
            {
                string bootstrapFailure = _steamworksBootstrap == null
                    ? "SteamworksBootstrap is missing."
                    : $"SteamworksBootstrap failed: {_steamworksBootstrap.LastInitError}";

                if (!isHostMode && _allowLoopbackFallbackUntilSdkReady && hostIdIsEndpoint)
                {
                    return TryApplyLoopbackFallback(
                        networkManager,
                        isHostMode,
                        lobbyId,
                        hostId,
                        ref clientAddress,
                        ref port,
                        bootstrapFailure,
                        out details);
                }

                details = bootstrapFailure;
                return false;
            }

            if (hostIdIsEndpoint)
            {
                if (!isHostMode && _allowLoopbackFallbackUntilSdkReady)
                {
                    return TryApplyLoopbackFallback(
                        networkManager,
                        isHostMode,
                        lobbyId,
                        hostId,
                        ref clientAddress,
                        ref port,
                        "HostId is an endpoint override, not a SteamID.",
                        out details);
                }

                details = $"Host mode Steam relay requires a numeric host SteamID, but hostId='{hostId}' resolved as an endpoint.";
                return false;
            }

            if (_transportManager.Transport != _multipassTransport)
            {
                details = "Steam relay startup transport was not prepared before NetworkManager initialization. Re-enter play mode with Steam relay selected before startup.";
                return false;
            }

            ConfigureMultipassTransports();
            SetFishyPeerToPeer(true);
            _multipassTransport.SetPort(port);
            _multipassTransport.SetClientTransport(_fishySteamworksTransport);

            if (isHostMode)
            {
                clientAddress = _steamworksBootstrap.LocalSteamIdString;
                details = $"Steam relay binder configured host mode with FishySteamworks. lobbyId={lobbyId}, steamId={clientAddress}, virtualPort={port}.";
                Log(details);
                return true;
            }

            if (!hostIdIsSteamId)
            {
                details = $"Client Steam relay bootstrap requires a numeric host SteamID, but hostId='{hostId}' was not valid.";
                return false;
            }

            clientAddress = hostId.Trim();
            _fishySteamworksTransport.SetClientAddress(clientAddress);
            details = $"Steam relay binder configured client mode with FishySteamworks. lobbyId={lobbyId}, hostSteamId={clientAddress}, virtualPort={port}.";
            Log(details);
            return true;
        }

        private void Awake()
        {
            ResolveDependencies(null);
            if (_applyStartupSelectionOnAwake)
            {
                ApplyStartupTransportSelection();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependencies(null);
        }
#endif

        private void ApplyStartupTransportSelection()
        {
            if (_startupSelectionApplied)
            {
                return;
            }

            if (!EnsureTransportComponents(null, out string details))
            {
                Log(details);
                return;
            }

            if (ShouldUseSteamRelayStartupTransport())
            {
                ConfigureMultipassTransports();
                SetFishyPeerToPeer(true);
                _multipassTransport.GlobalServerActions = true;
                _multipassTransport.SetClientTransport(_tugboatTransport);
                SetTransportManagerTransport(_multipassTransport);
                Log("Startup transport prepared for Steam relay using Multipass(Tugboat,FishySteamworks).");
            }
            else
            {
                SetTransportManagerTransport(_tugboatTransport);
                Log("Startup transport prepared for direct mode using Tugboat.");
            }

            _startupSelectionApplied = true;
        }

        private bool EnsureTransportComponents(NetworkManager networkManager, out string details)
        {
            GameObject target = networkManager != null ? networkManager.gameObject : gameObject;
            _transportManager ??= target.GetComponent<TransportManager>();
            _tugboatTransport ??= target.GetComponent<Tugboat>();
            _multipassTransport ??= target.GetComponent<Multipass>();
            _fishySteamworksTransport ??= target.GetComponent<FishySteamworks.FishySteamworks>();

            if (_transportManager == null)
            {
                _transportManager = target.AddComponent<TransportManager>();
            }

            if (_tugboatTransport == null)
            {
                _tugboatTransport = target.AddComponent<Tugboat>();
            }

            if (_multipassTransport == null)
            {
                _multipassTransport = target.AddComponent<Multipass>();
            }

            if (_fishySteamworksTransport == null)
            {
                _fishySteamworksTransport = target.AddComponent<FishySteamworks.FishySteamworks>();
            }

            if (_transportManager == null || _tugboatTransport == null || _multipassTransport == null || _fishySteamworksTransport == null)
            {
                details = "Steam relay binder could not create required transport components.";
                return false;
            }

            details = string.Empty;
            return true;
        }

        private bool TryApplyLoopbackFallback(
            NetworkManager networkManager,
            bool isHostMode,
            string lobbyId,
            string hostId,
            ref string clientAddress,
            ref ushort port,
            string reason,
            out string details)
        {
            ResolveDependencies(networkManager);
            if (_loopbackFallback == null)
            {
                details = $"Loopback fallback requested but SteamRelayLoopbackTransportBinder is missing. reason={reason}";
                return false;
            }

            if (isHostMode)
            {
                details = $"Loopback fallback is not supported for host startup once Steam relay transport selection has begun. reason={reason}";
                return false;
            }

            if (_transportManager != null && _transportManager.Transport == _multipassTransport)
            {
                ConfigureMultipassTransports();
                _multipassTransport.SetPort(port);
                _multipassTransport.SetClientTransport(_tugboatTransport);
            }
            else if (_transportManager != null && _tugboatTransport != null)
            {
                SetTransportManagerTransport(_tugboatTransport);
            }

            bool applied = _loopbackFallback.TryApplyBootstrap(
                networkManager,
                isHostMode,
                lobbyId,
                hostId,
                ref clientAddress,
                ref port,
                out string loopbackDetails);

            details = applied
                ? $"Steam relay SDK binder delegated to loopback fallback. reason={reason} {loopbackDetails}"
                : $"Loopback fallback failed. reason={reason} {loopbackDetails}";

            if (applied)
            {
                Log(details);
            }

            return applied;
        }

        private void ConfigureMultipassTransports()
        {
            if (_multipassTransport == null || _tugboatTransport == null || _fishySteamworksTransport == null)
            {
                return;
            }

            FieldInfo transportsField = typeof(Multipass).GetField("_transports", INSTANCE_FLAGS);
            if (transportsField == null)
            {
                return;
            }

            List<Transport> transports = transportsField.GetValue(_multipassTransport) as List<Transport>;
            if (transports == null)
            {
                transports = new List<Transport>();
                transportsField.SetValue(_multipassTransport, transports);
            }

            transports.Clear();
            transports.Add(_tugboatTransport);
            transports.Add(_fishySteamworksTransport);
        }

        private bool ShouldUseSteamRelayStartupTransport()
        {
            string runtimeProvider = ReadRuntimeOverride(PROVIDER_ARGUMENT, PROVIDER_ENVIRONMENT_VARIABLE);
            if (TryParseProvider(runtimeProvider, out bool useSteamRelay))
            {
                return useSteamRelay;
            }

            return _fishNetSessionService != null && _fishNetSessionService.ConfiguredForSteamRelay;
        }

        private void SetTransportManagerTransport(Transport transport)
        {
            if (_transportManager == null || transport == null)
            {
                return;
            }

            _transportManager.Transport = transport;
        }

        private void SetFishyPeerToPeer(bool value)
        {
            if (_fishySteamworksTransport == null)
            {
                return;
            }

            FieldInfo peerToPeerField = typeof(FishySteamworks.FishySteamworks).GetField("_peerToPeer", INSTANCE_FLAGS);
            peerToPeerField?.SetValue(_fishySteamworksTransport, value);
        }

        private void ResolveDependencies(NetworkManager networkManager)
        {
            GameObject target = networkManager != null ? networkManager.gameObject : gameObject;
            _loopbackFallback ??= target.GetComponent<SteamRelayLoopbackTransportBinder>();
            _steamworksBootstrap ??= target.GetComponent<SteamworksBootstrap>();
            _fishNetSessionService ??= target.GetComponent<FishNetSessionService>();
            _transportManager ??= target.GetComponent<TransportManager>();
            _tugboatTransport ??= target.GetComponent<Tugboat>();
            _multipassTransport ??= target.GetComponent<Multipass>();
            _fishySteamworksTransport ??= target.GetComponent<FishySteamworks.FishySteamworks>();
        }

        private static bool TryParseProvider(string rawValue, out bool useSteamRelay)
        {
            switch ((rawValue ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "steam":
                case "steamrelay":
                case "steam-relay":
                case "relay":
                    useSteamRelay = true;
                    return true;
                case "direct":
                case "ip":
                case "socket":
                    useSteamRelay = false;
                    return true;
                default:
                    useSteamRelay = false;
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

            string argumentToken = "-" + argumentName;
            string equalsToken = argumentToken + "=";
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

        private static bool TryParseSteamId(string rawValue, out ulong steamId)
        {
            if (ulong.TryParse(rawValue?.Trim(), out steamId))
            {
                return steamId > 0ul;
            }

            steamId = 0ul;
            return false;
        }

        private void Log(string message)
        {
            if (_logBootstrapEvents)
            {
                Debug.Log($"[SteamRelaySdkTransportBinder] {message}");
            }
        }
    }
}
