using UnityEngine;

namespace InterStella.Game.Shared.State
{
    public readonly struct ScrapCarryState
    {
        public ScrapCarryState(int scrapId, int carrierId, bool isCarried, Vector3 worldPosition)
        {
            ScrapId = scrapId;
            CarrierId = carrierId;
            IsCarried = isCarried;
            WorldPosition = worldPosition;
        }

        public int ScrapId { get; }
        public int CarrierId { get; }
        public bool IsCarried { get; }
        public Vector3 WorldPosition { get; }
    }
}
