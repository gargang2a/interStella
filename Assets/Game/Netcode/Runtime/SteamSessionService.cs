using System;
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
        private bool _autoCreateLobbyForHost = true;

        [SerializeField]
        private bool _autoAcceptInviteForClient = true;

        [SerializeField]
        private bool _allowDirectFallbackIfRelayUnavailable = true;

        [SerializeField]
        private string _localSteamUserId = "editor-local-user";

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
        private string _selfSteamIdArgument = "interstella-steam-self-id";

        [SerializeField]
        private LifecycleState _lifecycleState = LifecycleState.Idle;

        private ISessionService _networkSession;

        public bool IsSessionActive => _networkSession != null && _networkSession.IsSessionActive;
        public bool IsHost => _networkSession != null && _networkSession.IsHost;
        public string ActiveLobbyId => _activeLobbyId;
        public string ActiveHostSteamId => _activeHostSteamId;
        public string StateName => _lifecycleState.ToString();

        private void Awake()
        {
            ResolveNetworkSession();
            ApplyRuntimeOverrides();
            RefreshLifecycleFromState();
        }

        public bool StartSession()
        {
            ResolveNetworkSession();
            ApplyRuntimeOverrides();

            if (_networkSession == null)
            {
                _lifecycleState = LifecycleState.Failed;
                Debug.LogWarning("[SteamSessionService] StartSession failed: network session dependency is missing.");
                return false;
            }

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
                Debug.Log($"[SteamSessionService] Session started. lobbyId={_activeLobbyId}, hostSteamId={_activeHostSteamId}, networkHost={_networkSession.IsHost}.");
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

        public bool TryJoinLobby(string lobbyId, string hostSteamId)
        {
            string normalizedLobbyId = Normalize(lobbyId);
            if (string.IsNullOrWhiteSpace(normalizedLobbyId))
            {
                Debug.LogWarning("[SteamSessionService] TryJoinLobby failed: lobbyId is empty.");
                return false;
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

            if (string.IsNullOrWhiteSpace(_activeLobbyId))
            {
                _activeLobbyId = GenerateSyntheticLobbyId();
            }

            if (string.IsNullOrWhiteSpace(_activeHostSteamId))
            {
                _activeHostSteamId = Normalize(_localSteamUserId);
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
            _activeLobbyId = string.Empty;
            _activeHostSteamId = string.Empty;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveNetworkSession();
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
                    _activeHostSteamId = Normalize(_localSteamUserId);
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
                fishNetSession.UseSteamBootstrap(_activeLobbyId, _activeHostSteamId, _allowDirectFallbackIfRelayUnavailable);
                Debug.Log($"[SteamSessionService] Applied Steam bootstrap to FishNet. provider={fishNetSession.ActiveConnectionProvider}, lobbyId={fishNetSession.ActiveSteamLobbyId}, binder={fishNetSession.HasSteamRelayTransportBinder}.");
                return;
            }

            Debug.LogWarning("[SteamSessionService] Underlying session is not FishNetSessionService. Steam bootstrap was not applied.");
        }

        private void ResolveNetworkSession()
        {
            if (_networkSessionBehaviour == null)
            {
                _networkSessionBehaviour = GetComponent<FishNetSessionService>();
            }

            _networkSession = _networkSessionBehaviour as ISessionService;
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

            string inviteLobbyId = ReadRuntimeOverride(_inviteLobbyIdArgument, "INTERSTELLA_INVITE_LOBBY_ID");
            if (!string.IsNullOrWhiteSpace(inviteLobbyId))
            {
                string inviteHostId = ReadRuntimeOverride(_inviteHostIdArgument, "INTERSTELLA_INVITE_HOST_ID");
                QueueInvite(inviteLobbyId, inviteHostId);
            }
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
    }
}
