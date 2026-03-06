using FishNet.Managing;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class FishNetAuthorityGateway : MonoBehaviour, INetworkAuthorityGateway
    {
        [SerializeField]
        private NetworkManager _networkManager;

        [SerializeField]
        private bool _allowServerOnlyAuthoring = true;

        public bool IsHostAuthority
        {
            get
            {
                if (_networkManager == null)
                {
                    return false;
                }

                if (_allowServerOnlyAuthoring)
                {
                    return _networkManager.IsServerStarted;
                }

                return _networkManager.IsHostStarted;
            }
        }

        private void Awake()
        {
            ResolveNetworkManagerIfMissing();
        }

        public bool IsAuthoritativeOwner(int ownerId)
        {
            if (ownerId < 0 || _networkManager == null || !_networkManager.IsClientStarted)
            {
                return false;
            }

            if (_networkManager.ClientManager == null || _networkManager.ClientManager.Connection == null)
            {
                return false;
            }

            int localClientId = _networkManager.ClientManager.Connection.ClientId;
            if (localClientId < 0)
            {
                return false;
            }

            return localClientId == ownerId;
        }

        public bool TryCommitAuthoritativeAction(string actionName, int requesterId)
        {
            ResolveNetworkManagerIfMissing();
            if (!IsHostAuthority)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(actionName) || requesterId < 0)
            {
                return false;
            }

            // Host-authoritative server commits validated remote requests.
            if (_networkManager.IsServerStarted)
            {
                return true;
            }

            // Local-only fallback: allow only if this process is the requester owner.
            return IsAuthoritativeOwner(requesterId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveNetworkManagerIfMissing();
        }
#endif

        private void ResolveNetworkManagerIfMissing()
        {
            if (_networkManager == null)
            {
                _networkManager = FindObjectOfType<NetworkManager>();
            }
        }
    }
}
