using InterStella.Game.Shared.Interaction;
using InterStella.Game.Netcode.Runtime;
using UnityEngine;

namespace InterStella.Game.Features.Player
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField]
        private Transform _interactionOrigin;

        [SerializeField]
        private float _interactionRange = 2f;

        [SerializeField]
        private float _sphereRadius = 0.2f;

        [SerializeField]
        private LayerMask _interactionMask = ~0;

        [SerializeField]
        private KeyCode _interactionKey = KeyCode.E;

        [SerializeField]
        private PlayerNetworkBridge _networkBridge;

        [SerializeField]
        private PlayerInteractionNetworkRelay _interactionNetworkRelay;

        private void Awake()
        {
            if (_interactionOrigin == null)
            {
                _interactionOrigin = transform;
            }

            if (_networkBridge == null)
            {
                _networkBridge = GetComponent<PlayerNetworkBridge>();
            }

            if (_interactionNetworkRelay == null)
            {
                _interactionNetworkRelay = GetComponent<PlayerInteractionNetworkRelay>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(_interactionKey))
            {
                if (!CanIssueInteraction())
                {
                    return;
                }

                TryInteract();
            }
        }

        public bool TryInteract()
        {
            if (!CanIssueInteraction())
            {
                return false;
            }

            if (_interactionNetworkRelay != null && _interactionNetworkRelay.IsNetworkActive)
            {
                if (_interactionNetworkRelay.ShouldRequestServerInteraction)
                {
                    return _interactionNetworkRelay.TryRequestServerInteraction();
                }

                if (!_interactionNetworkRelay.CanExecuteAuthoritativeLocally)
                {
                    return false;
                }
            }

            return TryInteractAuthoritative();
        }

        public bool TryInteractAuthoritative()
        {
            Ray ray = new Ray(_interactionOrigin.position, _interactionOrigin.forward);
            if (!Physics.SphereCast(ray, _sphereRadius, out RaycastHit hit, _interactionRange, _interactionMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            if (!hit.collider.TryGetComponent(out MonoBehaviour behaviour))
            {
                return false;
            }

            if (behaviour is IInteractable interactable)
            {
                return interactable.TryInteract(new InteractionContext(gameObject, _interactionOrigin));
            }

            return false;
        }

        private bool CanIssueInteraction()
        {
            if (_networkBridge == null)
            {
                return true;
            }

            return _networkBridge.IsAuthoritativeOwner;
        }
    }
}
