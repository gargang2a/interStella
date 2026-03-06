using InterStella.Game.Netcode.Runtime;
using FishNet.Object;
using UnityEngine;

namespace InterStella.Game.Features.Player
{
    public sealed class PlayerNetworkBridge : MonoBehaviour
    {
        [SerializeField]
        private int _ownerId;

        [SerializeField]
        private MonoBehaviour _authorityGatewayBehaviour;

        [SerializeField]
        private NetworkObject _networkObject;

        private INetworkAuthorityGateway _authorityGateway;

        public int OwnerId
        {
            get
            {
                if (_networkObject != null && _networkObject.IsSpawned)
                {
                    return _networkObject.OwnerId;
                }

                return _ownerId;
            }
        }

        public bool IsAuthoritativeOwner { get; private set; }

        private void Awake()
        {
            ResolveDependenciesIfMissing();
        }

        private void Update()
        {
            if (_networkObject != null && _networkObject.IsSpawned)
            {
                _ownerId = _networkObject.OwnerId;
                IsAuthoritativeOwner = _networkObject.IsOwner;
                return;
            }

            if (_ownerId < 0)
            {
                IsAuthoritativeOwner = false;
                return;
            }

            if (_authorityGateway == null)
            {
                IsAuthoritativeOwner = true;
                return;
            }

            IsAuthoritativeOwner = _authorityGateway.IsAuthoritativeOwner(_ownerId);
        }

        public bool TryCommitAction(string actionName)
        {
            if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            {
                return false;
            }

            int requesterId = OwnerId;
            if (requesterId < 0)
            {
                return false;
            }

            if (_authorityGateway == null)
            {
                return IsAuthoritativeOwner;
            }

            return _authorityGateway.TryCommitAuthoritativeAction(actionName, requesterId);
        }

        public void SetOwnerId(int ownerId)
        {
            _ownerId = ownerId;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private void ResolveDependenciesIfMissing()
        {
            if (_authorityGateway == null && _authorityGatewayBehaviour != null)
            {
                _authorityGateway = _authorityGatewayBehaviour as INetworkAuthorityGateway;
            }

            if (_networkObject == null)
            {
                _networkObject = GetComponent<NetworkObject>();
            }
        }
    }
}
