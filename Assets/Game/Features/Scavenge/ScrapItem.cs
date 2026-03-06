using InterStella.Game.Shared.Interaction;
using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Features.Scavenge
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class ScrapItem : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private ScrapItemDefinition _definition;

        private Rigidbody _rigidbody;
        private Collider _collider;
        private Renderer[] _renderers;
        private ScrapState _state = ScrapState.World;
        private PlayerCarrySocket _carrier;
        private int _scrapId;

        public ScrapState State => _state;
        public int ScrapId => _scrapId;
        public ScrapItemDefinition Definition => _definition;
        public PlayerCarrySocket Carrier => _carrier;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _scrapId = gameObject.GetInstanceID();
        }

        public bool TryInteract(in InteractionContext context)
        {
            if (_state == ScrapState.Delivered || context.Interactor == null)
            {
                return false;
            }

            if (!context.Interactor.TryGetComponent(out PlayerCarrySocket carrySocket))
            {
                return false;
            }

            if (_state == ScrapState.World)
            {
                return carrySocket.TryPickup(this);
            }

            if (_state == ScrapState.Carried && _carrier == carrySocket)
            {
                Vector3 forward = context.Origin == null ? context.Interactor.transform.forward : context.Origin.forward;
                return carrySocket.TryDrop(forward);
            }

            return false;
        }

        public void AttachToCarrier(PlayerCarrySocket carrySocket)
        {
            SetCarriedStateAuthoritative(carrySocket);
        }

        public void SetCarriedStateAuthoritative(PlayerCarrySocket carrySocket)
        {
            if (carrySocket == null)
            {
                return;
            }

            _carrier = carrySocket;
            _state = ScrapState.Carried;

            Transform attachPoint = carrySocket.AttachPoint;
            transform.SetParent(attachPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            ClearVelocityIfDynamic();
            _rigidbody.isKinematic = true;
            _collider.enabled = false;
            SetVisualsEnabled(true);
        }

        public void DropToWorld(Vector3 worldPosition, Vector3 impulse)
        {
            SetWorldStateAuthoritative(worldPosition, simulatePhysics: true);
            _rigidbody.AddForce(impulse, ForceMode.VelocityChange);
        }

        public void SetWorldStateAuthoritative(Vector3 worldPosition, bool simulatePhysics)
        {
            transform.SetParent(null, true);
            transform.position = worldPosition;

            _carrier = null;
            _state = ScrapState.World;

            if (simulatePhysics && _rigidbody.isKinematic)
            {
                _rigidbody.isKinematic = false;
            }

            ClearVelocityIfDynamic();
            _rigidbody.isKinematic = !simulatePhysics;
            _collider.enabled = true;
            SetVisualsEnabled(true);
        }

        public void MarkDelivered()
        {
            _carrier = null;
            _state = ScrapState.Delivered;
            ClearVelocityIfDynamic();
            _rigidbody.isKinematic = true;
            _collider.enabled = false;
            SetVisualsEnabled(false);
        }

        public ScrapCarryState BuildState()
        {
            int carrierId = _carrier == null ? -1 : _carrier.gameObject.GetInstanceID();
            return new ScrapCarryState(
                _scrapId,
                carrierId,
                _state == ScrapState.Carried,
                transform.position);
        }

        private void SetVisualsEnabled(bool isEnabled)
        {
            if (_renderers == null)
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer != null)
                {
                    renderer.enabled = isEnabled;
                }
            }
        }

        private void ClearVelocityIfDynamic()
        {
            if (_rigidbody == null || _rigidbody.isKinematic)
            {
                return;
            }

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
