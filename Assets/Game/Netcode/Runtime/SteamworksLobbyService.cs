using System;
using System.Threading;
using Steamworks;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    [DefaultExecutionOrder(-900)]
    public sealed class SteamworksLobbyService : MonoBehaviour, ISteamLobbyService
    {
        private enum LobbyVisibility
        {
            Private = 0,
            FriendsOnly = 1,
            Public = 2,
            Invisible = 3
        }

        [SerializeField]
        private SteamworksBootstrap _steamworksBootstrap;

        [SerializeField]
        private LobbyVisibility _defaultLobbyVisibility = LobbyVisibility.FriendsOnly;

        [SerializeField]
        private int _maxLobbyMembers = 2;

        [SerializeField]
        private int _operationTimeoutMilliseconds = 5000;

        [SerializeField]
        private bool _setLobbyJoinable = true;

        [SerializeField]
        private bool _acceptOverlayJoinRequests = true;

        [SerializeField]
        private bool _logLobbyEvents = true;

        [Header("Lobby Metadata Keys")]
        [SerializeField]
        private string _hostSteamIdKey = "host_steam_id";

        [SerializeField]
        private string _providerKey = "provider";

        [SerializeField]
        private string _buildVersionKey = "build_version";

        [SerializeField]
        private string _createdUtcKey = "created_utc";

        [Header("Runtime")]
        [SerializeField]
        private string _pendingInviteLobbyId = string.Empty;

        [SerializeField]
        private string _lastOperationDetails = string.Empty;

        private Callback<GameLobbyJoinRequested_t> _joinRequestedCallback;
        private CallResult<LobbyCreated_t> _createLobbyCallResult;
        private CallResult<LobbyEnter_t> _joinLobbyCallResult;

        private bool _createLobbyCompleted;
        private bool _createLobbyIoFailure;
        private LobbyCreated_t _createLobbyResult;

        private bool _joinLobbyCompleted;
        private bool _joinLobbyIoFailure;
        private LobbyEnter_t _joinLobbyResult;

        public bool TryCreateLobby(out string lobbyId, out string hostSteamId, out string details)
        {
            lobbyId = string.Empty;
            hostSteamId = string.Empty;
            details = string.Empty;

            ResolveSteamworksBootstrapIfMissing();
            RegisterCallbacksIfNeeded();
            if (!EnsureSteamInitialized(out details))
            {
                _lastOperationDetails = details;
                return false;
            }

            if (!_steamworksBootstrap.TryGetLocalSteamIdString(out hostSteamId))
            {
                details = "Steam local user id is unavailable after Steam initialization.";
                _lastOperationDetails = details;
                return false;
            }

            ResetCreateLobbyOperation();
            SteamAPICall_t createLobbyCall = SteamMatchmaking.CreateLobby(ToSteamLobbyType(_defaultLobbyVisibility), Math.Max(2, _maxLobbyMembers));
            _createLobbyCallResult.Set(createLobbyCall, OnLobbyCreated);

            if (!WaitForCreateLobby(out details))
            {
                _lastOperationDetails = details;
                return false;
            }

            if (_createLobbyIoFailure)
            {
                details = "Steam lobby creation failed with an IO failure.";
                _lastOperationDetails = details;
                return false;
            }

            if (_createLobbyResult.m_eResult != EResult.k_EResultOK)
            {
                details = $"Steam lobby creation failed with result={_createLobbyResult.m_eResult}.";
                _lastOperationDetails = details;
                return false;
            }

            CSteamID lobbySteamId = new CSteamID(_createLobbyResult.m_ulSteamIDLobby);
            if (!lobbySteamId.IsValid())
            {
                details = "Steam lobby creation returned an invalid lobby id.";
                _lastOperationDetails = details;
                return false;
            }

            lobbyId = lobbySteamId.m_SteamID.ToString();
            string metadataDetails = ApplyHostLobbyMetadata(lobbySteamId, hostSteamId);
            details = string.IsNullOrWhiteSpace(metadataDetails)
                ? $"Steam lobby created. lobbyId={lobbyId}, hostSteamId={hostSteamId}."
                : $"Steam lobby created. lobbyId={lobbyId}, hostSteamId={hostSteamId}. {metadataDetails}";
            _lastOperationDetails = details;
            Log(details);
            return true;
        }

        public bool TryJoinLobby(string lobbyId, out string hostSteamId, out string details)
        {
            hostSteamId = string.Empty;
            details = string.Empty;

            if (!TryParseLobbyId(lobbyId, out CSteamID lobbySteamId))
            {
                details = $"Steam lobby join failed: lobbyId='{lobbyId}' is not a valid Steam lobby id.";
                _lastOperationDetails = details;
                return false;
            }

            ResolveSteamworksBootstrapIfMissing();
            RegisterCallbacksIfNeeded();
            if (!EnsureSteamInitialized(out details))
            {
                _lastOperationDetails = details;
                return false;
            }

            ResetJoinLobbyOperation();
            SteamAPICall_t joinLobbyCall = SteamMatchmaking.JoinLobby(lobbySteamId);
            _joinLobbyCallResult.Set(joinLobbyCall, OnLobbyEntered);

            if (!WaitForJoinLobby(out details))
            {
                _lastOperationDetails = details;
                return false;
            }

            if (_joinLobbyIoFailure)
            {
                details = "Steam lobby join failed with an IO failure.";
                _lastOperationDetails = details;
                return false;
            }

            if (_joinLobbyResult.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                details = $"Steam lobby join failed with response={(EChatRoomEnterResponse)_joinLobbyResult.m_EChatRoomEnterResponse}.";
                _lastOperationDetails = details;
                return false;
            }

            hostSteamId = ResolveLobbyHostSteamId(lobbySteamId);
            if (string.IsNullOrWhiteSpace(hostSteamId))
            {
                details = $"Steam lobby join succeeded for lobbyId={lobbyId}, but host SteamID metadata was missing.";
                _lastOperationDetails = details;
                return false;
            }

            details = $"Steam lobby joined. lobbyId={lobbyId}, hostSteamId={hostSteamId}.";
            _lastOperationDetails = details;
            Log(details);
            return true;
        }

        public bool TryConsumePendingInvite(out string lobbyId)
        {
            lobbyId = Normalize(_pendingInviteLobbyId);
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                return false;
            }

            _pendingInviteLobbyId = string.Empty;
            return true;
        }

        public void LeaveLobby(string lobbyId)
        {
            if (!TryParseLobbyId(lobbyId, out CSteamID lobbySteamId))
            {
                return;
            }

            ResolveSteamworksBootstrapIfMissing();
            if (_steamworksBootstrap == null || !_steamworksBootstrap.IsInitialized)
            {
                return;
            }

            SteamMatchmaking.LeaveLobby(lobbySteamId);
            Log($"Steam lobby left. lobbyId={lobbyId}.");
        }

        private void Awake()
        {
            ResolveSteamworksBootstrapIfMissing();
            RegisterCallbacksIfNeeded();
        }

        private void OnEnable()
        {
            ResolveSteamworksBootstrapIfMissing();
            RegisterCallbacksIfNeeded();
        }

        private void OnDisable()
        {
            DisposeCallbacks();
        }

        private void OnDestroy()
        {
            DisposeCallbacks();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveSteamworksBootstrapIfMissing();
            _maxLobbyMembers = Math.Max(2, _maxLobbyMembers);
            _operationTimeoutMilliseconds = Math.Max(100, _operationTimeoutMilliseconds);
        }
#endif

        private void ResolveSteamworksBootstrapIfMissing()
        {
            if (_steamworksBootstrap == null)
            {
                _steamworksBootstrap = GetComponent<SteamworksBootstrap>();
            }
        }

        private void RegisterCallbacksIfNeeded()
        {
            _createLobbyCallResult ??= CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _joinLobbyCallResult ??= CallResult<LobbyEnter_t>.Create(OnLobbyEntered);

            if (_acceptOverlayJoinRequests && _joinRequestedCallback == null)
            {
                _joinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            }
        }

        private void DisposeCallbacks()
        {
            _joinRequestedCallback?.Dispose();
            _joinRequestedCallback = null;

            _createLobbyCallResult?.Dispose();
            _createLobbyCallResult = null;

            _joinLobbyCallResult?.Dispose();
            _joinLobbyCallResult = null;
        }

        private bool EnsureSteamInitialized(out string details)
        {
            if (_steamworksBootstrap == null)
            {
                details = "SteamworksBootstrap is missing.";
                return false;
            }

            if (!_steamworksBootstrap.TryInitialize())
            {
                details = $"SteamworksBootstrap failed: {_steamworksBootstrap.LastInitError}";
                return false;
            }

            details = string.Empty;
            return true;
        }

        private void ResetCreateLobbyOperation()
        {
            _createLobbyCompleted = false;
            _createLobbyIoFailure = false;
            _createLobbyResult = default;
        }

        private void ResetJoinLobbyOperation()
        {
            _joinLobbyCompleted = false;
            _joinLobbyIoFailure = false;
            _joinLobbyResult = default;
        }

        private bool WaitForCreateLobby(out string details)
        {
            return WaitForOperation("CreateLobby", () => _createLobbyCompleted, out details);
        }

        private bool WaitForJoinLobby(out string details)
        {
            return WaitForOperation("JoinLobby", () => _joinLobbyCompleted, out details);
        }

        private bool WaitForOperation(string operationName, Func<bool> isCompleted, out string details)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(100, _operationTimeoutMilliseconds));
            while (!isCompleted())
            {
                SteamAPI.RunCallbacks();
                if (DateTime.UtcNow >= deadline)
                {
                    details = $"Steam {operationName} timed out after {_operationTimeoutMilliseconds} ms.";
                    return false;
                }

                Thread.Sleep(15);
            }

            details = string.Empty;
            return true;
        }

        private string ApplyHostLobbyMetadata(CSteamID lobbySteamId, string hostSteamId)
        {
            string warnings = string.Empty;
            TrySetLobbyData(lobbySteamId, _hostSteamIdKey, hostSteamId, ref warnings);
            TrySetLobbyData(lobbySteamId, _providerKey, "steam", ref warnings);

            if (!string.IsNullOrWhiteSpace(Application.version))
            {
                TrySetLobbyData(lobbySteamId, _buildVersionKey, Application.version, ref warnings);
            }

            TrySetLobbyData(lobbySteamId, _createdUtcKey, DateTime.UtcNow.ToString("o"), ref warnings);
            SteamMatchmaking.SetLobbyGameServer(lobbySteamId, 0u, 0, new CSteamID(_steamworksBootstrap.LocalSteamId));
            SteamMatchmaking.SetLobbyMemberLimit(lobbySteamId, Math.Max(2, _maxLobbyMembers));
            if (_setLobbyJoinable)
            {
                SteamMatchmaking.SetLobbyJoinable(lobbySteamId, true);
            }

            return warnings;
        }

        private void TrySetLobbyData(CSteamID lobbySteamId, string key, string value, ref string warnings)
        {
            string normalizedKey = Normalize(key);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                return;
            }

            if (!SteamMatchmaking.SetLobbyData(lobbySteamId, normalizedKey, value ?? string.Empty))
            {
                string failure = $"Failed to write lobby metadata key='{normalizedKey}'.";
                warnings = string.IsNullOrWhiteSpace(warnings) ? failure : warnings + " " + failure;
            }
        }

        private string ResolveLobbyHostSteamId(CSteamID lobbySteamId)
        {
            string fromMetadata = Normalize(SteamMatchmaking.GetLobbyData(lobbySteamId, _hostSteamIdKey));
            if (TryParseSteamId(fromMetadata, out _))
            {
                return fromMetadata;
            }

            if (SteamMatchmaking.GetLobbyGameServer(lobbySteamId, out _, out _, out CSteamID gameServerSteamId) && gameServerSteamId.IsValid())
            {
                return gameServerSteamId.m_SteamID.ToString();
            }

            CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(lobbySteamId);
            if (lobbyOwner.IsValid())
            {
                return lobbyOwner.m_SteamID.ToString();
            }

            return string.Empty;
        }

        private void OnLobbyCreated(LobbyCreated_t callbackData, bool ioFailure)
        {
            _createLobbyIoFailure = ioFailure;
            _createLobbyResult = callbackData;
            _createLobbyCompleted = true;
        }

        private void OnLobbyEntered(LobbyEnter_t callbackData, bool ioFailure)
        {
            _joinLobbyIoFailure = ioFailure;
            _joinLobbyResult = callbackData;
            _joinLobbyCompleted = true;
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callbackData)
        {
            _pendingInviteLobbyId = callbackData.m_steamIDLobby.m_SteamID.ToString();
            Log($"Steam overlay join requested. lobbyId={_pendingInviteLobbyId}.");
        }

        private static ELobbyType ToSteamLobbyType(LobbyVisibility visibility)
        {
            switch (visibility)
            {
                case LobbyVisibility.Private:
                    return ELobbyType.k_ELobbyTypePrivate;
                case LobbyVisibility.Public:
                    return ELobbyType.k_ELobbyTypePublic;
                case LobbyVisibility.Invisible:
                    return ELobbyType.k_ELobbyTypeInvisible;
                default:
                    return ELobbyType.k_ELobbyTypeFriendsOnly;
            }
        }

        private static bool TryParseLobbyId(string rawLobbyId, out CSteamID lobbySteamId)
        {
            if (TryParseSteamId(rawLobbyId, out ulong steamId))
            {
                lobbySteamId = new CSteamID(steamId);
                return lobbySteamId.IsValid();
            }

            lobbySteamId = CSteamID.Nil;
            return false;
        }

        private static bool TryParseSteamId(string rawSteamId, out ulong steamId)
        {
            if (ulong.TryParse(Normalize(rawSteamId), out steamId))
            {
                return steamId > 0ul;
            }

            steamId = 0ul;
            return false;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private void Log(string message)
        {
            if (_logLobbyEvents)
            {
                Debug.Log($"[SteamworksLobbyService] {message}");
            }
        }
    }
}
