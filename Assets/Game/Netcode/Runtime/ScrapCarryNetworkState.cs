using FishNet.Object;
using FishNet.Object.Synchronizing;
using InterStella.Game.Features.Player;
using InterStella.Game.Features.Scavenge;
using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(ScrapItem))]
    public sealed class ScrapCarryNetworkState : NetworkBehaviour
    {
        [SerializeField]
        private ScrapItem _scrapItem;

        [SerializeField, Min(0.02f)]
        private float _syncInterval = 0.05f;

        [SerializeField, Min(0f)]
        private float _worldPositionDeltaThreshold = 0.02f;

        private readonly SyncVar<byte> _stateSync = new();
        private readonly SyncVar<int> _carrierOwnerIdSync = new();
        private readonly SyncVar<Vector3> _worldPositionSync = new();

        private float _nextSyncTime;
        private byte _lastPublishedState = byte.MaxValue;
        private int _lastPublishedCarrierOwnerId = int.MinValue;
        private Vector3 _lastPublishedWorldPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        private byte _lastAppliedState = byte.MaxValue;
        private int _lastAppliedCarrierOwnerId = int.MinValue;
        private Vector3 _lastAppliedWorldPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        private void Awake()
        {
            ResolveDependenciesIfMissing();

            _stateSync.UpdateSendRate(0f);
            _carrierOwnerIdSync.UpdateSendRate(0f);
            _worldPositionSync.UpdateSendRate(0f);

            _stateSync.OnChange += HandleStateChanged;
            _carrierOwnerIdSync.OnChange += HandleCarrierOwnerChanged;
            _worldPositionSync.OnChange += HandleWorldPositionChanged;

            PublishServerState(force: true);
        }

        private void OnDestroy()
        {
            _stateSync.OnChange -= HandleStateChanged;
            _carrierOwnerIdSync.OnChange -= HandleCarrierOwnerChanged;
            _worldPositionSync.OnChange -= HandleWorldPositionChanged;
        }

        public override void OnStartServer()
        {
            PublishServerState(force: true);
        }

        private void FixedUpdate()
        {
            if (!IsServerStarted || _scrapItem == null)
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

        private void HandleStateChanged(byte previous, byte next, bool asServer)
        {
            if (asServer && IsHostStarted)
            {
                return;
            }

            ApplyClientState();
        }

        private void HandleCarrierOwnerChanged(int previous, int next, bool asServer)
        {
            if (asServer && IsHostStarted)
            {
                return;
            }

            ApplyClientState();
        }

        private void HandleWorldPositionChanged(Vector3 previous, Vector3 next, bool asServer)
        {
            if (asServer && IsHostStarted)
            {
                return;
            }

            ApplyClientState();
        }

        private void ApplyClientState()
        {
            if (_scrapItem == null)
            {
                return;
            }

            byte maxState = (byte)ScrapState.Delivered;
            byte stateByte = (byte)Mathf.Clamp(_stateSync.Value, 0, maxState);
            int carrierOwnerId = _carrierOwnerIdSync.Value;
            Vector3 worldPosition = _worldPositionSync.Value;

            if (HasAlreadyApplied(stateByte, carrierOwnerId, worldPosition))
            {
                return;
            }

            ScrapState state = (ScrapState)stateByte;
            switch (state)
            {
                case ScrapState.World:
                    _scrapItem.SetWorldStateAuthoritative(worldPosition, simulatePhysics: false);
                    break;
                case ScrapState.Carried:
                    PlayerCarrySocket carrySocket = FindCarrySocketByOwnerId(carrierOwnerId);
                    if (carrySocket != null)
                    {
                        _scrapItem.SetCarriedStateAuthoritative(carrySocket);
                    }
                    else
                    {
                        _scrapItem.SetWorldStateAuthoritative(worldPosition, simulatePhysics: false);
                    }
                    break;
                case ScrapState.Delivered:
                    _scrapItem.MarkDelivered();
                    break;
                default:
                    _scrapItem.SetWorldStateAuthoritative(worldPosition, simulatePhysics: false);
                    break;
            }

            CacheAppliedState(stateByte, carrierOwnerId, worldPosition);
        }

        private void PublishServerState(bool force)
        {
            if (_scrapItem == null)
            {
                return;
            }

            ScrapState state = _scrapItem.State;
            byte stateByte = (byte)Mathf.Clamp((int)state, 0, (int)ScrapState.Delivered);
            int carrierOwnerId = state == ScrapState.Carried ? ResolveCarrierOwnerId(_scrapItem.Carrier) : -1;
            Vector3 worldPosition = _scrapItem.transform.position;

            bool positionChanged = state == ScrapState.World && (worldPosition - _lastPublishedWorldPosition).sqrMagnitude > (_worldPositionDeltaThreshold * _worldPositionDeltaThreshold);
            bool changed = force
                || stateByte != _lastPublishedState
                || carrierOwnerId != _lastPublishedCarrierOwnerId
                || positionChanged;
            if (!changed)
            {
                return;
            }

            _stateSync.Value = stateByte;
            _carrierOwnerIdSync.Value = carrierOwnerId;
            _worldPositionSync.Value = worldPosition;

            _lastPublishedState = stateByte;
            _lastPublishedCarrierOwnerId = carrierOwnerId;
            _lastPublishedWorldPosition = worldPosition;
        }

        private static int ResolveCarrierOwnerId(PlayerCarrySocket carrier)
        {
            if (carrier == null || !carrier.TryGetComponent(out PlayerNetworkBridge playerNetworkBridge))
            {
                return -1;
            }

            return playerNetworkBridge.OwnerId;
        }

        private static PlayerCarrySocket FindCarrySocketByOwnerId(int ownerId)
        {
            if (ownerId < 0)
            {
                return null;
            }

            PlayerCarrySocket[] sockets = FindObjectsOfType<PlayerCarrySocket>();
            for (int i = 0; i < sockets.Length; i++)
            {
                PlayerCarrySocket socket = sockets[i];
                if (socket == null || !socket.TryGetComponent(out PlayerNetworkBridge playerNetworkBridge))
                {
                    continue;
                }

                if (playerNetworkBridge.OwnerId == ownerId)
                {
                    return socket;
                }
            }

            return null;
        }

        private bool HasAlreadyApplied(byte state, int carrierOwnerId, Vector3 worldPosition)
        {
            if (_lastAppliedState != state || _lastAppliedCarrierOwnerId != carrierOwnerId)
            {
                return false;
            }

            return (worldPosition - _lastAppliedWorldPosition).sqrMagnitude <= 0.0004f;
        }

        private void CacheAppliedState(byte state, int carrierOwnerId, Vector3 worldPosition)
        {
            _lastAppliedState = state;
            _lastAppliedCarrierOwnerId = carrierOwnerId;
            _lastAppliedWorldPosition = worldPosition;
        }

#if UNITY_EDITOR
        private new void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private void ResolveDependenciesIfMissing()
        {
            if (_scrapItem == null)
            {
                _scrapItem = GetComponent<ScrapItem>();
            }
        }
    }
}
