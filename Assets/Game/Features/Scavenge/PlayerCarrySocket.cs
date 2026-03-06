using UnityEngine;

namespace InterStella.Game.Features.Scavenge
{
    public sealed class PlayerCarrySocket : MonoBehaviour
    {
        [SerializeField]
        private Transform _attachPoint;

        [SerializeField]
        private float _dropImpulse = 2f;

        private ScrapItem _carriedItem;

        public bool HasItem => _carriedItem != null;
        public ScrapItem CarriedItem => _carriedItem;
        public Transform AttachPoint => _attachPoint == null ? transform : _attachPoint;

        public bool TryPickup(ScrapItem scrapItem)
        {
            if (scrapItem == null || _carriedItem != null)
            {
                return false;
            }

            _carriedItem = scrapItem;
            scrapItem.AttachToCarrier(this);
            return true;
        }

        public bool TryDrop(Vector3 forwardDirection)
        {
            if (_carriedItem == null)
            {
                return false;
            }

            ScrapItem item = _carriedItem;
            _carriedItem = null;

            Vector3 dropPosition = AttachPoint.position;
            Vector3 impulse = forwardDirection.normalized * _dropImpulse;
            item.DropToWorld(dropPosition, impulse);
            return true;
        }

        public bool TryConsumeForDelivery(out ScrapItem consumedItem)
        {
            consumedItem = _carriedItem;
            if (consumedItem == null)
            {
                return false;
            }

            _carriedItem = null;
            return true;
        }

        public bool TryForceDropWithoutImpulse()
        {
            if (_carriedItem == null)
            {
                return false;
            }

            ScrapItem item = _carriedItem;
            _carriedItem = null;
            item.SetWorldStateAuthoritative(AttachPoint.position, simulatePhysics: true);
            return true;
        }
    }
}
