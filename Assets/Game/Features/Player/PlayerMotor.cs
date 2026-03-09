using FishNet.Object.Prediction;
using InterStella.Game.Features.Fuel;
using UnityEngine;

namespace InterStella.Game.Features.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private PlayerInputReader _inputReader;

        [SerializeField]
        private PlayerFuel _playerFuel;

        [SerializeField]
        private PlayerNetworkBridge _networkBridge;

        [Header("Linear")]
        [SerializeField]
        private float _thrustAcceleration = 8f;

        [SerializeField]
        private float _boostMultiplier = 2f;

        [SerializeField]
        private float _brakeDamping = 0.9f;

        [SerializeField]
        private float _maxLinearSpeed = 12f;

        [Header("Angular")]
        [SerializeField]
        private float _lookAcceleration = 4f;

        [SerializeField]
        private float _rollAcceleration = 3f;

        [SerializeField]
        private float _maxAngularSpeed = 4f;

        private Rigidbody _rigidbody;
        private bool _isTetherConstrained;
        private bool _useExternalSimulation;

        public PlayerMotorState CurrentState { get; private set; }
        public Rigidbody Body => _rigidbody;

        private void Awake()
        {
            if (_inputReader == null)
            {
                _inputReader = GetComponent<PlayerInputReader>();
            }

            if (_playerFuel == null)
            {
                _playerFuel = GetComponent<PlayerFuel>();
            }

            if (_networkBridge == null)
            {
                _networkBridge = GetComponent<PlayerNetworkBridge>();
            }

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.drag = 0f;
            _rigidbody.angularDrag = 0.25f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_inputReader == null)
            {
                _inputReader = GetComponent<PlayerInputReader>();
            }

            if (_playerFuel == null)
            {
                _playerFuel = GetComponent<PlayerFuel>();
            }

            if (_networkBridge == null)
            {
                _networkBridge = GetComponent<PlayerNetworkBridge>();
            }
        }
#endif

        private void FixedUpdate()
        {
            if (_inputReader == null || _playerFuel == null)
            {
                return;
            }

            if (_useExternalSimulation)
            {
                RefreshCurrentState();
                return;
            }

            if (_networkBridge != null && !_networkBridge.IsAuthoritativeOwner)
            {
                RefreshCurrentState();
                return;
            }

            Simulate(_inputReader.CurrentSample, Time.fixedDeltaTime);
        }

        public void SetExternalSimulationEnabled(bool useExternalSimulation)
        {
            _useExternalSimulation = useExternalSimulation;
        }

        public void Simulate(PlayerInputSample inputSample, float deltaTime, PredictionRigidbody predictionBody = null)
        {
            if (_playerFuel == null)
            {
                return;
            }

            bool usedFuelThisFrame = false;
            Vector3 translationForce = Vector3.zero;
            bool hasTranslationInput = inputSample.Translation.sqrMagnitude > 0.0001f;

            if (hasTranslationInput && !_playerFuel.IsDepleted)
            {
                float boostScale = inputSample.IsBoosting ? _boostMultiplier : 1f;
                FuelUseReason reason = inputSample.IsBoosting ? FuelUseReason.Boost : FuelUseReason.Thrust;

                if (_playerFuel.TryConsume(reason, deltaTime))
                {
                    translationForce = inputSample.Translation * (_thrustAcceleration * boostScale);
                    usedFuelThisFrame = true;
                }
            }

            bool shouldBrake = false;
            if (inputSample.IsBraking && _playerFuel.TryConsume(FuelUseReason.Recovery, deltaTime))
            {
                shouldBrake = true;
                usedFuelThisFrame = true;
            }

            if (shouldBrake)
            {
                float damping = Mathf.Clamp01(1f - (_brakeDamping * deltaTime));
                SetLinearVelocity(_rigidbody.velocity * damping, predictionBody);
                SetAngularVelocity(_rigidbody.angularVelocity * damping, predictionBody);
            }

            if (_isTetherConstrained)
            {
                SetLinearVelocity(_rigidbody.velocity * 0.995f, predictionBody);
            }

            SetLinearVelocity(Vector3.ClampMagnitude(_rigidbody.velocity, _maxLinearSpeed), predictionBody);
            SetAngularVelocity(Vector3.ClampMagnitude(_rigidbody.angularVelocity, _maxAngularSpeed), predictionBody);

            if (translationForce.sqrMagnitude > 0.0001f)
            {
                AddRelativeForce(translationForce, ForceMode.Acceleration, predictionBody);
            }

            if (!_playerFuel.IsDepleted)
            {
                Vector3 torque = new Vector3(-inputSample.LookDelta.y, inputSample.LookDelta.x, -inputSample.RollInput);
                if (torque.sqrMagnitude > 0.0001f)
                {
                    torque.x *= _lookAcceleration;
                    torque.y *= _lookAcceleration;
                    torque.z *= _rollAcceleration;
                    AddRelativeTorque(torque, ForceMode.Acceleration, predictionBody);
                }
            }

            _playerFuel.RecoverWhenIdle(deltaTime, !usedFuelThisFrame);

            if (predictionBody != null)
            {
                predictionBody.Simulate();
            }

            RefreshCurrentState(inputSample.IsBoosting, inputSample.IsBraking);
        }

        public void RefreshCurrentState(bool isBoosting = false, bool isBraking = false)
        {
            if (_playerFuel == null)
            {
                return;
            }

            CurrentState = new PlayerMotorState(
                _rigidbody.velocity,
                _rigidbody.angularVelocity,
                isBoosting,
                isBraking,
                _playerFuel.IsDepleted,
                _isTetherConstrained);
        }

        public void SetTetherConstrained(bool isConstrained)
        {
            _isTetherConstrained = isConstrained;
            RefreshCurrentState(CurrentState.IsBoosting, CurrentState.IsBraking);
        }

        private void AddRelativeForce(Vector3 force, ForceMode mode, PredictionRigidbody predictionBody)
        {
            if (predictionBody != null)
            {
                predictionBody.AddRelativeForce(force, mode);
                return;
            }

            _rigidbody.AddRelativeForce(force, mode);
        }

        private void AddRelativeTorque(Vector3 torque, ForceMode mode, PredictionRigidbody predictionBody)
        {
            if (predictionBody != null)
            {
                predictionBody.AddRelativeTorque(torque, mode);
                return;
            }

            _rigidbody.AddRelativeTorque(torque, mode);
        }

        private void SetLinearVelocity(Vector3 velocity, PredictionRigidbody predictionBody)
        {
            if (predictionBody != null)
            {
                predictionBody.Velocity(velocity);
                return;
            }

            _rigidbody.velocity = velocity;
        }

        private void SetAngularVelocity(Vector3 angularVelocity, PredictionRigidbody predictionBody)
        {
            if (predictionBody != null)
            {
                predictionBody.AngularVelocity(angularVelocity);
                return;
            }

            _rigidbody.angularVelocity = angularVelocity;
        }
    }
}
