using FishNet.Object;
using FishNet.Object.Synchronizing;
using InterStella.Game.Features.Repair;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class RepairObjectiveNetworkState : NetworkBehaviour
    {
        [SerializeField]
        private RepairStationObjective _repairObjective;

        [SerializeField, Min(0.02f)]
        private float _syncInterval = 0.1f;

        [SerializeField]
        private bool _logTransientDeliveryEvents;

        private readonly SyncVar<int> _deliveredCountSync = new();
        private float _nextSyncTime;
        private int _lastPublishedDelivered = int.MinValue;
        private int _lastNotifiedDelivered = int.MinValue;

        private void Awake()
        {
            ResolveDependenciesIfMissing();

            _deliveredCountSync.UpdateSendRate(0f);
            _deliveredCountSync.OnChange += HandleDeliveredCountChanged;
            if (_repairObjective != null)
            {
                _deliveredCountSync.Value = _repairObjective.DeliveredCount;
            }
        }

        private void OnDestroy()
        {
            _deliveredCountSync.OnChange -= HandleDeliveredCountChanged;
        }

        public override void OnStartServer()
        {
            PublishServerState(force: true);
        }

        private void Update()
        {
            if (!IsServerStarted || _repairObjective == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextSyncTime)
            {
                return;
            }

            _nextSyncTime = now + _syncInterval;
            PublishServerState(force: false);
        }

        private void HandleDeliveredCountChanged(int previous, int next, bool asServer)
        {
            if (_repairObjective == null)
            {
                return;
            }

            if (asServer && IsHostStarted)
            {
                return;
            }

            _repairObjective.SetDeliveredCountAuthoritative(next);
        }

        private void PublishServerState(bool force)
        {
            if (_repairObjective == null)
            {
                return;
            }

            int required = Mathf.Max(0, _repairObjective.RequiredScrapCount);
            int delivered = Mathf.Clamp(_repairObjective.DeliveredCount, 0, required);
            if (_lastPublishedDelivered != int.MinValue && delivered > _lastPublishedDelivered)
            {
                RpcDeliveryAccepted(delivered, required);
            }

            if (!force && delivered == _lastPublishedDelivered)
            {
                return;
            }

            _deliveredCountSync.Value = delivered;
            _lastPublishedDelivered = delivered;
        }

        [ObserversRpc]
        private void RpcDeliveryAccepted(int deliveredCount, int requiredCount)
        {
            if (IsServerStarted)
            {
                return;
            }

            if (deliveredCount <= _lastNotifiedDelivered)
            {
                return;
            }

            _lastNotifiedDelivered = deliveredCount;
            if (_logTransientDeliveryEvents)
            {
                Debug.Log($"[RepairObjectiveNetworkState] Delivery event received. delivered={deliveredCount}/{requiredCount}, station={name}");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private void ResolveDependenciesIfMissing()
        {
            if (_repairObjective == null)
            {
                _repairObjective = GetComponent<RepairStationObjective>();
            }
        }
    }
}
