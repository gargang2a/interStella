using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class LocalAuthorityGateway : MonoBehaviour, INetworkAuthorityGateway
    {
        [SerializeField]
        private bool _isHostAuthority = true;

        public bool IsHostAuthority => _isHostAuthority;

        public bool IsAuthoritativeOwner(int ownerId)
        {
            return _isHostAuthority && ownerId >= 0;
        }

        public bool TryCommitAuthoritativeAction(string actionName, int requesterId)
        {
            return _isHostAuthority && requesterId >= 0 && !string.IsNullOrWhiteSpace(actionName);
        }
    }
}
