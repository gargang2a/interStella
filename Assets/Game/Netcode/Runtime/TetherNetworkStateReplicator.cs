using FishNet.Object;
using FishNet.Object.Synchronizing;
using InterStella.Game.Features.Tether;
using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class TetherNetworkStateReplicator : NetworkBehaviour
    {
        [SerializeField]
        private TetherLink _tetherLink;

        [SerializeField, Min(0.02f)]
        private float _syncInterval = 0.05f;

        [SerializeField, Min(0f)]
        private float _distanceDeltaThreshold = 0.02f;

        [SerializeField]
        private bool _emitRegressionMarkers = true;

        private readonly SyncVar<float> _distanceSync = new();
        private readonly SyncVar<byte> _tensionLevelSync = new();
        private readonly SyncVar<bool> _isBrokenSync = new();

        private float _nextSyncTime;
        private float _lastDistance;
        private byte _lastTensionLevel;
        private bool _lastBroken;
        private uint _serverBreakSequence;
        private float _lastAppliedDistance = float.MinValue;
        private byte _lastAppliedTensionLevel = byte.MaxValue;
        private bool _lastAppliedBroken;
        private uint _lastReceivedBreakSequence;
        private bool _hasReceivedBreakSequence;
        private bool _hasLoggedClientApplyMarker;
        private bool _hasLoggedTransientBreakMarker;

        private void Awake()
        {
            ResolveDependenciesIfMissing();

            _distanceSync.UpdateSendRate(0f);
            _tensionLevelSync.UpdateSendRate(0f);
            _isBrokenSync.UpdateSendRate(0f);

            _distanceSync.OnChange += HandleDistanceChanged;
            _tensionLevelSync.OnChange += HandleTensionLevelChanged;
            _isBrokenSync.OnChange += HandleBrokenChanged;

            PublishServerState(force: true);
        }

        private void OnDestroy()
        {
            _distanceSync.OnChange -= HandleDistanceChanged;
            _tensionLevelSync.OnChange -= HandleTensionLevelChanged;
            _isBrokenSync.OnChange -= HandleBrokenChanged;
        }

        public override void OnStartServer()
        {
            PublishServerState(force: true);
        }

        private void FixedUpdate()
        {
            if (!IsServerStarted || _tetherLink == null)
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

        [ObserversRpc]
        private void RpcTetherBroken(float currentDistance, uint breakSequence)
        {
            if (IsServerStarted || _tetherLink == null)
            {
                return;
            }

            if (_hasReceivedBreakSequence && !NetworkSequenceComparer.IsNewer(breakSequence, _lastReceivedBreakSequence))
            {
                return;
            }

            _lastReceivedBreakSequence = breakSequence;
            _hasReceivedBreakSequence = true;
            _tetherLink.MarkBroken(currentDistance);
            CacheAppliedState(currentDistance, (byte)TetherTensionLevel.Broken, true);
            if (_emitRegressionMarkers && !_hasLoggedTransientBreakMarker)
            {
                _hasLoggedTransientBreakMarker = true;
                Debug.Log($"[TetherNetworkStateReplicator] Transient tether break received. distance={currentDistance:F3}, sequence={breakSequence}, object={name}");
            }
        }

        private void HandleDistanceChanged(float previous, float next, bool asServer)
        {
            if (asServer && IsHostStarted)
            {
                return;
            }

            ApplyClientState();
        }

        private void HandleTensionLevelChanged(byte previous, byte next, bool asServer)
        {
            if (asServer && IsHostStarted)
            {
                return;
            }

            ApplyClientState();
        }

        private void HandleBrokenChanged(bool previous, bool next, bool asServer)
        {
            if (asServer && IsHostStarted)
            {
                return;
            }

            ApplyClientState();
        }

        private void ApplyClientState()
        {
            if (_tetherLink == null)
            {
                return;
            }

            float distance = _distanceSync.Value;
            byte levelByte = (byte)Mathf.Clamp(_tensionLevelSync.Value, 0, (int)TetherTensionLevel.Broken);
            bool isBroken = _isBrokenSync.Value || levelByte == (byte)TetherTensionLevel.Broken;

            if (HasAlreadyApplied(distance, levelByte, isBroken))
            {
                return;
            }

            TetherTensionLevel level = (TetherTensionLevel)levelByte;
            if (isBroken)
            {
                _tetherLink.MarkBroken(distance);
                ApplyConstraintFlags(false);
                CacheAppliedState(distance, levelByte, true);
                TryLogClientApplyMarker(distance, level, true);
                return;
            }

            _tetherLink.SetRuntimeState(distance, level);
            ApplyConstraintFlags(level == TetherTensionLevel.Tension || level == TetherTensionLevel.HardLimit);
            CacheAppliedState(distance, levelByte, false);
            TryLogClientApplyMarker(distance, level, false);
        }

        private void PublishServerState(bool force)
        {
            if (_tetherLink == null)
            {
                return;
            }

            TetherState state = _tetherLink.BuildState();
            float distance = state.CurrentDistance;
            byte level = (byte)Mathf.Clamp((int)state.TensionLevel, 0, (int)TetherTensionLevel.Broken);
            bool broken = state.IsBroken || state.TensionLevel == TetherTensionLevel.Broken;

            bool changed = force
                || Mathf.Abs(distance - _lastDistance) > _distanceDeltaThreshold
                || level != _lastTensionLevel
                || broken != _lastBroken;
            if (!changed)
            {
                return;
            }

            if (!_lastBroken && broken)
            {
                _serverBreakSequence++;
                RpcTetherBroken(distance, _serverBreakSequence);
            }

            _distanceSync.Value = distance;
            _tensionLevelSync.Value = level;
            _isBrokenSync.Value = broken;

            _lastDistance = distance;
            _lastTensionLevel = level;
            _lastBroken = broken;
            if (_emitRegressionMarkers && !force)
            {
                Debug.Log($"[TetherNetworkStateReplicator] Durable tether sync published. distance={distance:F3}, level={(TetherTensionLevel)level}, broken={broken}, object={name}");
            }
        }

        private bool HasAlreadyApplied(float distance, byte tensionLevel, bool broken)
        {
            if (_lastAppliedTensionLevel != tensionLevel || _lastAppliedBroken != broken)
            {
                return false;
            }

            return Mathf.Abs(_lastAppliedDistance - distance) <= 0.0005f;
        }

        private void CacheAppliedState(float distance, byte tensionLevel, bool broken)
        {
            _lastAppliedDistance = distance;
            _lastAppliedTensionLevel = tensionLevel;
            _lastAppliedBroken = broken;
        }

        private void TryLogClientApplyMarker(float distance, TetherTensionLevel level, bool isBroken)
        {
            if (!_emitRegressionMarkers || _hasLoggedClientApplyMarker || IsServerStarted)
            {
                return;
            }

            _hasLoggedClientApplyMarker = true;
            Debug.Log($"[TetherNetworkStateReplicator] Durable tether sync applied. distance={distance:F3}, level={level}, broken={isBroken}, object={name}");
        }

        public void LogRegressionSnapshot()
        {
            if (!_emitRegressionMarkers || !IsServerStarted || _tetherLink == null)
            {
                return;
            }

            TetherState state = _tetherLink.BuildState();
            Debug.Log($"[TetherNetworkStateReplicator] Durable tether snapshot. distance={state.CurrentDistance:F3}, level={state.TensionLevel}, broken={state.IsBroken}, object={name}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private void ResolveDependenciesIfMissing()
        {
            if (_tetherLink == null)
            {
                _tetherLink = GetComponent<TetherLink>();
            }
        }

        private void ApplyConstraintFlags(bool isConstrained)
        {
            if (_tetherLink == null || !_tetherLink.TryGetEndpoints(out TetherEndpoint endpointA, out TetherEndpoint endpointB))
            {
                return;
            }

            if (endpointA.PlayerMotor != null)
            {
                endpointA.PlayerMotor.SetTetherConstrained(isConstrained);
            }

            if (endpointB.PlayerMotor != null)
            {
                endpointB.PlayerMotor.SetTetherConstrained(isConstrained);
            }
        }
    }
}
