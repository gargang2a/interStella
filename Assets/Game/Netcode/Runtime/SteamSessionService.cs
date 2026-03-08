using System;
using System.IO;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class SteamSessionService : MonoBehaviour, ISessionService
    {
        private enum LifecycleState
        {
            Idle = 0,
            InvitePending = 1,
            LobbyReady = 2,
            SessionActive = 3,
            Failed = 4
        }

        [SerializeField]
        private MonoBehaviour _networkSessionBehaviour;

        [SerializeField]
        private MonoBehaviour _steamLobbyServiceBehaviour;

        [SerializeField]
        private SteamworksBootstrap _steamworksBootstrap;

        [SerializeField]
        private bool _autoCreateLobbyForHost = true;

        [SerializeField]
        private bool _autoAcceptInviteForClient = true;

        [SerializeField]
        private bool _allowDirectFallbackIfRelayUnavailable = true;

        [SerializeField]
        private string _localSteamUserId = "editor-local-user";

        [Header("Optional Host Invite")]
        [SerializeField]
        private string _autoInviteFriendSteamId = string.Empty;

        [Header("Invite Bootstrap")]
        [SerializeField]
        private string _queuedInviteLobbyId = string.Empty;

        [SerializeField]
        private string _queuedInviteHostSteamId = string.Empty;

        [Header("Active Lobby")]
        [SerializeField]
        private string _activeLobbyId = string.Empty;

        [SerializeField]
        private string _activeHostSteamId = string.Empty;

        [Header("Runtime Override")]
        [SerializeField]
        private bool _allowRuntimeOverride = true;

        [SerializeField]
        private string _inviteLobbyIdArgument = "interstella-invite-lobby-id";

        [SerializeField]
        private string _inviteHostIdArgument = "interstella-invite-host-id";

        [SerializeField]
        private string _inviteFriendIdArgument = "interstella-invite-friend-id";

        [SerializeField]
        private string _connectLobbyArgument = "connect_lobby";

        [SerializeField]
        private string _selfSteamIdArgument = "interstella-steam-self-id";

        [SerializeField]
        private string _providerArgument = "interstella-provider";

        [SerializeField]
        private string _strictRelayArgument = "interstella-steam-strict-relay";

        [Header("Build Smoke Lobby Share")]
        [SerializeField]
        private bool _writeSharedLobbyInfoFile = true;

        [SerializeField]
        private string _sharedLobbyInfoFileName = "current-steam-lobby.txt";

        [SerializeField]
        private string _sharedLobbyInfoFileArgument = "interstella-shared-lobby-file";

        [SerializeField]
        private LifecycleState _lifecycleState = LifecycleState.Idle;

        private ISessionService _networkSession;
        private ISteamLobbyService _steamLobbyService;

        public bool IsSessionActive => _networkSession != null && _networkSession.IsSessionActive;
        public bool IsHost => _networkSession != null && _networkSession.IsHost;
        public bool UsesSteamProvider => IsSteamProviderConfigured();
        public string ActiveLobbyId => _activeLobbyId;
        public string ActiveHostSteamId => _activeHostSteamId;
        public string StateName => _lifecycleState.ToString();
        public string AutoInviteFriendSteamId => Normalize(_autoInviteFriendSteamId);

        private void Awake()
        {
            ResolveNetworkSession();
            ResolveSteamLobbyServiceIfMissing();
            ResolveSteamworksBootstrapIfMissing();
            ApplyRuntimeOverrides();
            RefreshLifecycleFromState();
        }

        public bool StartSession()
        {
            ResolveNetworkSession();
            ResolveSteamLobbyServiceIfMissing();
            ApplyRuntimeOverrides();

            if (_networkSession == null)
            {
                _lifecycleState = LifecycleState.Failed;
                Debug.LogWarning("[SteamSessionService] StartSession failed: network session dependency is missing.");
                return false;
            }

            if (!IsSteamProviderConfigured())
            {
                return StartUnderlyingDirectSession();
            }

            PrepareSharedLobbyInfoFileForHostSession();

            if (!PrepareLobbyBootstrap())
            {
                _lifecycleState = LifecycleState.Failed;
                return false;
            }

            ConfigureUnderlyingTransport();
            bool started = _networkSession.StartSession();
            _lifecycleState = started ? LifecycleState.SessionActive : LifecycleState.Failed;
            if (started)
            {
                WriteSharedLobbyInfoFile("session_active");
                Debug.Log($"[SteamSessionService] Session started. lobbyId={_activeLobbyId}, hostSteamId={_activeHostSteamId}, networkHost={_networkSession.IsHost}.");
                TrySendAutoInviteIfConfigured();
            }
            else
            {
                Debug.LogWarning("[SteamSessionService] Underlying network session start failed.");
            }

            return started;
        }

        public void StopSession()
        {
            _networkSession?.StopSession();
            ClearSharedLobbyInfoFile();
            LeaveActiveLobby();
            RefreshLifecycleFromState();
            Debug.Log("[SteamSessionService] Session stopped and active lobby was cleared.");
        }

        public void QueueInvite(string lobbyId, string hostSteamId)
        {
            _queuedInviteLobbyId = Normalize(lobbyId);
            _queuedInviteHostSteamId = Normalize(hostSteamId);
            RefreshLifecycleFromState();
            Debug.Log($"[SteamSessionService] Invite queued. lobbyId={_queuedInviteLobbyId}, hostSteamId={_queuedInviteHostSteamId}.");
        }

        public bool TryInviteUserToActiveLobby(string targetSteamId, out string details)
        {
            details = string.Empty;
            ResolveNetworkSession();
            ResolveSteamLobbyServiceIfMissing();

            if (!IsSteamProviderConfigured())
            {
                details = "Steam invite is unavailable because the active provider is not Steam.";
                return false;
            }

            if (!IsHost)
            {
                details = "Steam invite is only supported from the host session in the current MVP flow.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_activeLobbyId))
            {
                details = "Steam invite is unavailable because there is no active lobby.";
                return false;
            }

            if (_steamLobbyService == null)
            {
                details = "Steam invite is unavailable because ISteamLobbyService is missing.";
                return false;
            }

            return _steamLobbyService.TryInviteUser(_activeLobbyId, targetSteamId, out details);
        }

        public bool TryJoinLobby(string lobbyId, string hostSteamId)
        {
            string normalizedLobbyId = Normalize(lobbyId);
            if (string.IsNullOrWhiteSpace(normalizedLobbyId))
            {
                Debug.LogWarning("[SteamSessionService] TryJoinLobby failed: lobbyId is empty.");
                return false;
            }

            if (IsSteamProviderConfigured())
            {
                ResolveSteamLobbyServiceIfMissing();
                if (_steamLobbyService == null)
                {
                    Debug.LogWarning("[SteamSessionService] TryJoinLobby failed: Steam provider is active but ISteamLobbyService is missing.");
                    return false;
                }

                if (!_steamLobbyService.TryJoinLobby(normalizedLobbyId, out string resolvedHostSteamId, out string details))
                {
                    Debug.LogWarning("[SteamSessionService] TryJoinLobby failed: " + details);
                    return false;
                }

                _activeLobbyId = normalizedLobbyId;
                _activeHostSteamId = Normalize(string.IsNullOrWhiteSpace(resolvedHostSteamId) ? hostSteamId : resolvedHostSteamId);
                _lifecycleState = LifecycleState.LobbyReady;
                Debug.Log($"[SteamSessionService] Lobby joined via Steam. lobbyId={_activeLobbyId}, hostSteamId={_activeHostSteamId}. {details}");
                return true;
            }

            _activeLobbyId = normalizedLobbyId;
            _activeHostSteamId = Normalize(hostSteamId);
            _lifecycleState = LifecycleState.LobbyReady;
            Debug.Log($"[SteamSessionService] Lobby joined. lobbyId={_activeLobbyId}, hostSteamId={_activeHostSteamId}.");
            return true;
        }

        public bool TryCreateHostLobby()
        {
            if (!IsHost)
            {
                Debug.LogWarning("[SteamSessionService] TryCreateHostLobby failed: current runtime is not host.");
                return false;
            }

            if (IsSteamProviderConfigured())
            {
                ResolveSteamLobbyServiceIfMissing();
                if (_steamLobbyService == null)
                {
                    Debug.LogWarning("[SteamSessionService] TryCreateHostLobby failed: Steam provider is active but ISteamLobbyService is missing.");
                    return false;
                }

                if (!_steamLobbyService.TryCreateLobby(out string lobbyId, out string hostSteamId, out string details))
                {
                    Debug.LogWarning("[SteamSessionService] TryCreateHostLobby failed: " + details);
                    return false;
                }

                _activeLobbyId = Normalize(lobbyId);
                _activeHostSteamId = Normalize(hostSteamId);
                _lifecycleState = LifecycleState.LobbyReady;
                WriteSharedLobbyInfoFile("lobby_ready");
                Debug.Log($"[SteamSessionService] Host Steam lobby created. lobbyId={_activeLobbyId}, hostSteamId={_activeHostSteamId}. {details}");
                return true;
            }

            if (string.IsNullOrWhiteSpace(_activeLobbyId))
            {
                _activeLobbyId = GenerateSyntheticLobbyId();
            }

            if (string.IsNullOrWhiteSpace(_activeHostSteamId))
            {
                _activeHostSteamId = ResolvePreferredLocalHostId();
                if (string.IsNullOrWhiteSpace(_activeHostSteamId))
                {
                    _activeHostSteamId = "editor-host";
                }
            }

            _lifecycleState = LifecycleState.LobbyReady;
            Debug.Log($"[SteamSessionService] Host lobby created. lobbyId={_activeLobbyId}, hostSteamId={_activeHostSteamId}.");
            return true;
        }

        public void LeaveActiveLobby()
        {
            string lobbyIdToLeave = _activeLobbyId;
            if (IsSteamProviderConfigured())
            {
                ResolveSteamLobbyServiceIfMissing();
                _steamLobbyService?.LeaveLobby(lobbyIdToLeave);
            }

            _activeLobbyId = string.Empty;
            _activeHostSteamId = string.Empty;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveNetworkSession();
            ResolveSteamLobbyServiceIfMissing();
            ResolveSteamworksBootstrapIfMissing();
            RefreshLifecycleFromState();
        }
#endif

        private bool PrepareLobbyBootstrap()
        {
            if (IsHost)
            {
                return EnsureHostLobby();
            }

            return EnsureClientLobby();
        }

        private bool EnsureHostLobby()
        {
            if (!string.IsNullOrWhiteSpace(_activeLobbyId))
            {
                if (string.IsNullOrWhiteSpace(_activeHostSteamId))
                {
                    _activeHostSteamId = ResolvePreferredLocalHostId();
                }

                _lifecycleState = LifecycleState.LobbyReady;
                return true;
            }

            if (!_autoCreateLobbyForHost)
            {
                Debug.LogWarning("[SteamSessionService] Host session start blocked: no active lobby and auto-create is disabled.");
                return false;
            }

            return TryCreateHostLobby();
        }

        private bool EnsureClientLobby()
        {
            CaptureQueuedInviteFromSteam();

            if (!string.IsNullOrWhiteSpace(_activeLobbyId))
            {
                _lifecycleState = LifecycleState.LobbyReady;
                return true;
            }

            if (_autoAcceptInviteForClient && !string.IsNullOrWhiteSpace(_queuedInviteLobbyId))
            {
                bool accepted = TryJoinLobby(_queuedInviteLobbyId, _queuedInviteHostSteamId);
                if (accepted)
                {
                    _queuedInviteLobbyId = string.Empty;
                    _queuedInviteHostSteamId = string.Empty;
                }

                return accepted;
            }

            Debug.LogWarning("[SteamSessionService] Client session start blocked: no active lobby and no queued invite.");
            return false;
        }

        private void ConfigureUnderlyingTransport()
        {
            if (_networkSessionBehaviour is FishNetSessionService fishNetSession)
            {
                if (!IsSteamProviderConfigured())
                {
                    fishNetSession.UseDirectBootstrap();
                    Debug.Log("[SteamSessionService] Applied direct bootstrap to FishNet.");
                    return;
                }

                fishNetSession.UseSteamBootstrap(_activeLobbyId, _activeHostSteamId, _allowDirectFallbackIfRelayUnavailable);
                Debug.Log($"[SteamSessionService] Applied Steam bootstrap to FishNet. provider={fishNetSession.ActiveConnectionProvider}, lobbyId={fishNetSession.ActiveSteamLobbyId}, binder={fishNetSession.HasSteamRelayTransportBinder}.");
                return;
            }

            Debug.LogWarning("[SteamSessionService] Underlying session is not FishNetSessionService. Steam bootstrap was not applied.");
        }

        private bool StartUnderlyingDirectSession()
        {
            LeaveActiveLobby();
            ConfigureUnderlyingTransport();

            bool started = _networkSession.StartSession();
            _lifecycleState = started ? LifecycleState.SessionActive : LifecycleState.Failed;
            if (started)
            {
                Debug.Log($"[SteamSessionService] Session started in direct mode. networkHost={_networkSession.IsHost}.");
            }
            else
            {
                Debug.LogWarning("[SteamSessionService] Underlying direct network session start failed.");
            }

            return started;
        }

        private void ResolveNetworkSession()
        {
            if (_networkSessionBehaviour == null)
            {
                _networkSessionBehaviour = GetComponent<FishNetSessionService>();
            }

            _networkSession = _networkSessionBehaviour as ISessionService;
        }

        private void ResolveSteamLobbyServiceIfMissing()
        {
            if (_steamLobbyServiceBehaviour == null)
            {
                _steamLobbyServiceBehaviour = GetComponent<SteamworksLobbyService>();
            }

            if (_steamLobbyServiceBehaviour == null)
            {
                Component interfaceComponent = GetComponent(typeof(ISteamLobbyService));
                if (interfaceComponent is MonoBehaviour lobbyServiceBehaviour)
                {
                    _steamLobbyServiceBehaviour = lobbyServiceBehaviour;
                }
            }

            _steamLobbyService = _steamLobbyServiceBehaviour as ISteamLobbyService;
        }

        private void ResolveSteamworksBootstrapIfMissing()
        {
            if (_steamworksBootstrap == null)
            {
                _steamworksBootstrap = GetComponent<SteamworksBootstrap>();
            }
        }

        private void ApplyRuntimeOverrides()
        {
            if (!_allowRuntimeOverride)
            {
                return;
            }

            string selfSteamId = ReadRuntimeOverride(_selfSteamIdArgument, "INTERSTELLA_STEAM_SELF_ID");
            if (!string.IsNullOrWhiteSpace(selfSteamId))
            {
                _localSteamUserId = selfSteamId.Trim();
            }

            string inviteFriendSteamId = ReadRuntimeOverride(_inviteFriendIdArgument, "INTERSTELLA_STEAM_INVITE_FRIEND_ID");
            if (!string.IsNullOrWhiteSpace(inviteFriendSteamId))
            {
                _autoInviteFriendSteamId = inviteFriendSteamId.Trim();
            }

            string inviteLobbyId = ReadRuntimeOverride(_inviteLobbyIdArgument, "INTERSTELLA_INVITE_LOBBY_ID");
            if (string.IsNullOrWhiteSpace(inviteLobbyId))
            {
                inviteLobbyId = ReadCommandLineOverride(_connectLobbyArgument, '+');
            }

            if (string.IsNullOrWhiteSpace(inviteLobbyId))
            {
                CaptureQueuedInviteFromSteam();
                inviteLobbyId = _queuedInviteLobbyId;
            }

            if (!string.IsNullOrWhiteSpace(inviteLobbyId))
            {
                string inviteHostId = ReadRuntimeOverride(_inviteHostIdArgument, "INTERSTELLA_INVITE_HOST_ID");
                if (string.IsNullOrWhiteSpace(inviteHostId))
                {
                    inviteHostId = _queuedInviteHostSteamId;
                }

                QueueInvite(inviteLobbyId, inviteHostId);
            }

            if (ReadRuntimeFlag(_strictRelayArgument, "INTERSTELLA_STEAM_STRICT_RELAY"))
            {
                _allowDirectFallbackIfRelayUnavailable = false;
            }
        }

        private void CaptureQueuedInviteFromSteam()
        {
            if (!IsSteamProviderConfigured())
            {
                return;
            }

            ResolveSteamLobbyServiceIfMissing();
            if (_steamLobbyService != null && _steamLobbyService.TryConsumePendingInvite(out string pendingInviteLobbyId))
            {
                QueueInvite(pendingInviteLobbyId, string.Empty);
            }
        }

        private void TrySendAutoInviteIfConfigured()
        {
            if (!IsHost || !IsSteamProviderConfigured())
            {
                return;
            }

            string inviteTarget = Normalize(_autoInviteFriendSteamId);
            if (string.IsNullOrWhiteSpace(inviteTarget))
            {
                return;
            }

            bool invited = TryInviteUserToActiveLobby(inviteTarget, out string details);
            if (invited)
            {
                Debug.Log("[SteamSessionService] Auto invite succeeded. " + details);
            }
            else
            {
                Debug.LogWarning("[SteamSessionService] Auto invite failed. " + details);
            }
        }

        private void PrepareSharedLobbyInfoFileForHostSession()
        {
            if (!CanAccessSharedLobbyInfoFileForHostSession())
            {
                return;
            }

            string filePath = ResolveSharedLobbyInfoFilePath();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SteamSessionService] Failed to clear previous shared lobby info file. " + exception.Message);
            }
        }

        private void WriteSharedLobbyInfoFile(string phase)
        {
            if (!CanAccessSharedLobbyInfoFileForHostSession() || string.IsNullOrWhiteSpace(_activeLobbyId))
            {
                return;
            }

            string filePath = ResolveSharedLobbyInfoFilePath();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            try
            {
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string[] lines =
                {
                    "lobby_id=" + _activeLobbyId,
                    "host_steam_id=" + _activeHostSteamId,
                    "phase=" + phase,
                    "updated_utc=" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
                };

                File.WriteAllLines(filePath, lines);
                Debug.Log($"[SteamSessionService] Shared lobby info file updated. path={filePath}, lobbyId={_activeLobbyId}, phase={phase}.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SteamSessionService] Failed to write shared lobby info file. " + exception.Message);
            }
        }

        private void ClearSharedLobbyInfoFile()
        {
            if (!CanAccessSharedLobbyInfoFileForHostSession())
            {
                return;
            }

            string filePath = ResolveSharedLobbyInfoFilePath();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SteamSessionService] Failed to clear shared lobby info file. " + exception.Message);
            }
        }

        private bool CanAccessSharedLobbyInfoFileForHostSession()
        {
            return _writeSharedLobbyInfoFile
                && !Application.isEditor
                && IsHost
                && IsSteamProviderConfigured();
        }

        private string ResolveSharedLobbyInfoFilePath()
        {
            string overridePath = ReadRuntimeOverride(_sharedLobbyInfoFileArgument, "INTERSTELLA_SHARED_LOBBY_FILE_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return overridePath.Trim();
            }

            string baseDirectoryPath = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(baseDirectoryPath) || string.IsNullOrWhiteSpace(_sharedLobbyInfoFileName))
            {
                return string.Empty;
            }

            return Path.Combine(baseDirectoryPath, _sharedLobbyInfoFileName);
        }

        private string ResolvePreferredLocalHostId()
        {
            if (IsSteamProviderConfigured() && _steamworksBootstrap != null && _steamworksBootstrap.TryInitialize())
            {
                if (_steamworksBootstrap.TryGetLocalSteamIdString(out string bootstrapSteamId))
                {
                    return bootstrapSteamId;
                }
            }

            return Normalize(_localSteamUserId);
        }

        private bool IsSteamProviderConfigured()
        {
            string runtimeProvider = ReadRuntimeOverride(_providerArgument, "INTERSTELLA_PROVIDER");
            if (!string.IsNullOrWhiteSpace(runtimeProvider))
            {
                switch (runtimeProvider.Trim().ToLowerInvariant())
                {
                    case "steam":
                    case "steamrelay":
                    case "steam-relay":
                    case "relay":
                        return true;
                    case "direct":
                    case "ip":
                    case "socket":
                        return false;
                }
            }

            if (_networkSessionBehaviour is FishNetSessionService fishNetSession)
            {
                return fishNetSession.ConfiguredForSteamRelay;
            }

            return false;
        }

        private void RefreshLifecycleFromState()
        {
            if (_networkSession != null && _networkSession.IsSessionActive)
            {
                _lifecycleState = LifecycleState.SessionActive;
                return;
            }

            if (!string.IsNullOrWhiteSpace(_activeLobbyId))
            {
                _lifecycleState = LifecycleState.LobbyReady;
                return;
            }

            if (!string.IsNullOrWhiteSpace(_queuedInviteLobbyId))
            {
                _lifecycleState = LifecycleState.InvitePending;
                return;
            }

            if (_lifecycleState != LifecycleState.Failed)
            {
                _lifecycleState = LifecycleState.Idle;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string GenerateSyntheticLobbyId()
        {
            return "local-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
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

            return ReadCommandLineOverride(argumentName, '-');
        }

        private static string ReadCommandLineOverride(string argumentName, char prefix)
        {
            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return string.Empty;
            }

            string argumentToken = prefix + argumentName;
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

        private static bool ReadRuntimeFlag(string argumentName, string environmentVariableName)
        {
            string fromEnvironment = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                string normalized = fromEnvironment.Trim().ToLowerInvariant();
                return normalized == "1" || normalized == "true" || normalized == "yes" || normalized == "on";
            }

            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return false;
            }

            string argumentToken = "-" + argumentName;
            string equalsToken = argumentToken + "=";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, argumentToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    {
                        string value = args[i + 1].Trim();
                        return value != "0" && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
                    }

                    return true;
                }

                if (arg.StartsWith(equalsToken, StringComparison.OrdinalIgnoreCase))
                {
                    string value = arg.Substring(equalsToken.Length).Trim();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return true;
                    }

                    return value != "0" && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
    }
}
