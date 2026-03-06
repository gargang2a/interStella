using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using InterStella.Game.Features.Player;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class PlayerFuelNetworkState : NetworkBehaviour
    {
        [SerializeField]
        private PlayerFuel _playerFuel;

        [SerializeField]
        private PlayerNetworkBridge _playerNetworkBridge;

        [SerializeField, Min(0.02f)]
        private float _sendInterval = 0.1f;

        [SerializeField, Min(0f)]
        private float _minimumDeltaToSync = 0.02f;

        [SerializeField, Min(0f)]
        private float _maxAcceptedDeltaPerSecond;

        [SerializeField, Min(0f)]
        private float _serverDeltaTolerance = 0.05f;

        private readonly SyncVar<float> _currentFuelSync = new();
        private float _lastSubmittedFuel = float.MinValue;
        private float _nextSendTime;
        private float _lastAcceptedFuel = float.MinValue;
        private float _lastAcceptedFuelTime = -1f;

        private void Awake()
        {
            ResolveDependenciesIfMissing();

            _currentFuelSync.UpdateSendRate(0f);
            _currentFuelSync.OnChange += HandleCurrentFuelChanged;
            if (_playerFuel != null)
            {
                _currentFuelSync.Value = _playerFuel.CurrentFuel;
            }
        }

        private void OnDestroy()
        {
            _currentFuelSync.OnChange -= HandleCurrentFuelChanged;
        }

        public override void OnStartServer()
        {
            PublishFromLocalFuel();
        }

        private void FixedUpdate()
        {
            if (_playerFuel == null || (!IsServerStarted && !IsClientStarted))
            {
                return;
            }

            if (!IsOwner)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextSendTime)
            {
                return;
            }

            float localFuel = _playerFuel.CurrentFuel;
            if (Mathf.Abs(localFuel - _lastSubmittedFuel) < _minimumDeltaToSync)
            {
                return;
            }

            _lastSubmittedFuel = localFuel;
            _nextSendTime = now + _sendInterval;

            if (IsServerStarted)
            {
                float clampedFuel = ClampFuel(localFuel);
                _currentFuelSync.Value = clampedFuel;
                UpdateLastAcceptedFuel(clampedFuel);
                return;
            }

            SubmitFuelToServer(localFuel);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitFuelToServer(float currentFuel, NetworkConnection caller = null)
        {
            int expectedOwnerId = ResolveExpectedOwnerId();
            if (expectedOwnerId < 0 || caller == null || caller.ClientId != expectedOwnerId)
            {
                return;
            }

            float clampedFuel = ClampFuel(currentFuel);
            if (!CanAcceptSubmittedFuel(clampedFuel))
            {
                return;
            }

            _currentFuelSync.Value = clampedFuel;
            UpdateLastAcceptedFuel(clampedFuel);
        }

        private void HandleCurrentFuelChanged(float previous, float next, bool asServer)
        {
            if (_playerFuel == null)
            {
                return;
            }

            if (asServer && IsHostStarted)
            {
                return;
            }

            _playerFuel.SetCurrentFuelAuthoritative(next);
        }

        private void PublishFromLocalFuel()
        {
            if (_playerFuel == null)
            {
                return;
            }

            float localFuel = ClampFuel(_playerFuel.CurrentFuel);
            _currentFuelSync.Value = localFuel;
            _lastSubmittedFuel = localFuel;
            UpdateLastAcceptedFuel(localFuel);
        }

        private int ResolveExpectedOwnerId()
        {
            if (Owner.IsValid)
            {
                return Owner.ClientId;
            }

            if (_playerNetworkBridge != null)
            {
                return _playerNetworkBridge.OwnerId;
            }

            return -1;
        }

        private bool CanAcceptSubmittedFuel(float submittedFuel)
        {
            if (_lastAcceptedFuel < 0f || _lastAcceptedFuelTime < 0f)
            {
                return true;
            }

            float now = Time.unscaledTime;
            float deltaTime = Mathf.Max(0.02f, now - _lastAcceptedFuelTime);
            float maxDeltaPerSecond = ResolveMaxAcceptedDeltaPerSecond();
            if (maxDeltaPerSecond <= 0f)
            {
                return true;
            }

            float maxAllowedDelta = (maxDeltaPerSecond * deltaTime) + Mathf.Max(0f, _serverDeltaTolerance);
            return Mathf.Abs(submittedFuel - _lastAcceptedFuel) <= maxAllowedDelta;
        }

        private float ResolveMaxAcceptedDeltaPerSecond()
        {
            if (_maxAcceptedDeltaPerSecond > 0f)
            {
                return _maxAcceptedDeltaPerSecond;
            }

            if (_playerFuel == null)
            {
                return 0f;
            }

            return _playerFuel.GetMaximumDeltaRatePerSecond() * 2f;
        }

        private void UpdateLastAcceptedFuel(float acceptedFuel)
        {
            _lastAcceptedFuel = acceptedFuel;
            _lastAcceptedFuelTime = Time.unscaledTime;
        }

        private float ClampFuel(float currentFuel)
        {
            if (_playerFuel == null || _playerFuel.MaxFuel <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp(currentFuel, 0f, _playerFuel.MaxFuel);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private void ResolveDependenciesIfMissing()
        {
            if (_playerFuel == null)
            {
                _playerFuel = GetComponent<PlayerFuel>();
            }

            if (_playerNetworkBridge == null)
            {
                _playerNetworkBridge = GetComponent<PlayerNetworkBridge>();
            }
        }
    }
}
