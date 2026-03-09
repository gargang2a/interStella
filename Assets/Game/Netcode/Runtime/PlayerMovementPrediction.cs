using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using InterStella.Game.Features.Player;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerMovementPrediction : TickNetworkBehaviour
    {
        private const byte BOOST_FLAG = 1 << 0;
        private const byte BRAKE_FLAG = 1 << 1;

        public struct ReplicateData : IReplicateData
        {
            public Vector3 Translation;
            public Vector2 LookDelta;
            public float RollInput;
            public byte Flags;

            private uint _tick;

            public ReplicateData(Vector3 translation, Vector2 lookDelta, float rollInput, byte flags)
            {
                Translation = translation;
                LookDelta = lookDelta;
                RollInput = rollInput;
                Flags = flags;
                _tick = 0u;
            }

            public bool IsBoosting => (Flags & BOOST_FLAG) != 0;
            public bool IsBraking => (Flags & BRAKE_FLAG) != 0;

            public PlayerInputSample ToInputSample()
            {
                return new PlayerInputSample(Translation, LookDelta, RollInput, IsBoosting, IsBraking);
            }

            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }

        public struct ReconcileData : IReconcileData
        {
            public PredictionRigidbody Body;
            public float CurrentFuel;

            private uint _tick;

            public ReconcileData(PredictionRigidbody body, float currentFuel)
            {
                Body = body;
                CurrentFuel = currentFuel;
                _tick = 0u;
            }

            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }

        [SerializeField]
        private PlayerMotor _playerMotor;

        [SerializeField]
        private PlayerInputReader _inputReader;

        [SerializeField]
        private PlayerFuel _playerFuel;

        private readonly PredictionRigidbody _predictionBody = new();

        public bool UsesServerAuthoritativeFuel => isActiveAndEnabled && IsSpawned;

        private void Awake()
        {
            ResolveDependenciesIfMissing();
            InitializePredictionBody();
        }

        public override void OnStartNetwork()
        {
            ResolveDependenciesIfMissing();
            InitializePredictionBody();
            SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);

            if (_playerMotor != null)
            {
                _playerMotor.SetExternalSimulationEnabled(true);
            }
        }

        public override void OnStopNetwork()
        {
            if (_playerMotor != null)
            {
                _playerMotor.SetExternalSimulationEnabled(false);
            }
        }

        protected override void TimeManager_OnTick()
        {
            PerformReplicate(BuildMoveData());
        }

        protected override void TimeManager_OnPostTick()
        {
            CreateReconcile();
        }

        public override void CreateReconcile()
        {
            if (_playerFuel == null)
            {
                return;
            }

            InitializePredictionBody();
            if (_predictionBody.Rigidbody == null)
            {
                return;
            }

            PerformReconcile(new ReconcileData(_predictionBody, _playerFuel.CurrentFuel));
        }

        [Replicate]
        private void PerformReplicate(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (_playerMotor == null)
            {
                return;
            }

            InitializePredictionBody();
            if (_predictionBody.Rigidbody == null)
            {
                return;
            }

            _playerMotor.Simulate(data.ToInputSample(), (float)TimeManager.TickDelta, _predictionBody);
        }

        [Reconcile]
        private void PerformReconcile(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            InitializePredictionBody();
            if (_predictionBody.Rigidbody == null)
            {
                return;
            }

            _predictionBody.Reconcile(data.Body);

            if (_playerFuel != null)
            {
                _playerFuel.SetCurrentFuelAuthoritative(data.CurrentFuel);
            }

            _playerMotor?.RefreshCurrentState();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private ReplicateData BuildMoveData()
        {
            if (!IsOwner || _inputReader == null)
            {
                return default;
            }

            PlayerInputSample sample = _inputReader.CurrentSample;
            byte flags = 0;
            if (sample.IsBoosting)
            {
                flags |= BOOST_FLAG;
            }

            if (sample.IsBraking)
            {
                flags |= BRAKE_FLAG;
            }

            return new ReplicateData(sample.Translation, sample.LookDelta, sample.RollInput, flags);
        }

        private void ResolveDependenciesIfMissing()
        {
            if (_playerMotor == null)
            {
                _playerMotor = GetComponent<PlayerMotor>();
            }

            if (_inputReader == null)
            {
                _inputReader = GetComponent<PlayerInputReader>();
            }

            if (_playerFuel == null)
            {
                _playerFuel = GetComponent<PlayerFuel>();
            }
        }

        private void InitializePredictionBody()
        {
            if (_playerMotor == null || _playerMotor.Body == null)
            {
                return;
            }

            if (_predictionBody.Rigidbody == _playerMotor.Body)
            {
                return;
            }

            _predictionBody.Initialize(_playerMotor.Body);
        }
    }
}
