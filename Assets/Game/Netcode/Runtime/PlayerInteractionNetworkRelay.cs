using FishNet.Connection;
using FishNet.Object;
using InterStella.Game.Features.Player;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(PlayerInteraction))]
    public sealed class PlayerInteractionNetworkRelay : NetworkBehaviour
    {
        [SerializeField]
        private PlayerInteraction _playerInteraction;

        [SerializeField, Min(0f)]
        private float _serverRequestCooldown = 0.05f;

        [SerializeField]
        private bool _logRejectedRequests;

        [SerializeField]
        private bool _logAcceptedRequests = true;

        private float _nextAllowedServerRequestTime;

        public bool IsNetworkActive => IsSpawned && (IsServerStarted || IsClientStarted);
        public bool ShouldRequestServerInteraction => IsNetworkActive && IsClientStarted && !IsServerStarted && IsOwner;
        public bool CanExecuteAuthoritativeLocally => !IsNetworkActive || IsServerStarted;

        private void Awake()
        {
            ResolveDependenciesIfMissing();
        }

        public bool TryRequestServerInteraction()
        {
            if (!ShouldRequestServerInteraction)
            {
                return false;
            }

            RequestInteractionServerRpc();
            return true;
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestInteractionServerRpc(NetworkConnection caller = null)
        {
            if (!ValidateCaller(caller))
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextAllowedServerRequestTime)
            {
                return;
            }

            _nextAllowedServerRequestTime = now + _serverRequestCooldown;
            if (_playerInteraction == null)
            {
                ResolveDependenciesIfMissing();
            }

            if (_playerInteraction == null)
            {
                return;
            }

            bool interactionCommitted = _playerInteraction.TryInteractAuthoritative();
            if (_logAcceptedRequests)
            {
                int callerId = caller == null ? -1 : caller.ClientId;
                int ownerId = Owner.IsValid ? Owner.ClientId : -1;
                Debug.Log($"[PlayerInteractionNetworkRelay] Accepted interaction request. caller={callerId}, owner={ownerId}, committed={interactionCommitted}, object={name}");
            }
        }

        private bool ValidateCaller(NetworkConnection caller)
        {
            if (caller == null || !Owner.IsValid || caller.ClientId != Owner.ClientId)
            {
                if (_logRejectedRequests)
                {
                    int callerId = caller == null ? -1 : caller.ClientId;
                    int ownerId = Owner.IsValid ? Owner.ClientId : -1;
                    Debug.LogWarning($"[PlayerInteractionNetworkRelay] Rejected interaction request. caller={callerId}, owner={ownerId}, object={name}");
                }

                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private new void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private void ResolveDependenciesIfMissing()
        {
            if (_playerInteraction == null)
            {
                _playerInteraction = GetComponent<PlayerInteraction>();
            }
        }
    }
}
