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
        }
#endif

        private void FixedUpdate()
        {
            if (_inputReader == null || _playerFuel == null)
            {
                return;
            }

            PlayerInputSample inputSample = _inputReader.CurrentSample;
            float deltaTime = Time.fixedDeltaTime;

            bool hasTranslationInput = inputSample.Translation.sqrMagnitude > 0.0001f;
            bool usedFuelThisFrame = false;

            if (hasTranslationInput && !_playerFuel.IsDepleted)
            {
                float boostScale = inputSample.IsBoosting ? _boostMultiplier : 1f;
                FuelUseReason reason = inputSample.IsBoosting ? FuelUseReason.Boost : FuelUseReason.Thrust;

                if (_playerFuel.TryConsume(reason, deltaTime))
                {
                    Vector3 desiredAccel = inputSample.Translation * (_thrustAcceleration * boostScale);
                    _rigidbody.AddRelativeForce(desiredAccel, ForceMode.Acceleration);
                    usedFuelThisFrame = true;
                }
            }

            if (inputSample.IsBraking)
            {
                if (_playerFuel.TryConsume(FuelUseReason.Recovery, deltaTime))
                {
                    _rigidbody.velocity *= Mathf.Clamp01(1f - (_brakeDamping * deltaTime));
                    _rigidbody.angularVelocity *= Mathf.Clamp01(1f - (_brakeDamping * deltaTime));
                    usedFuelThisFrame = true;
                }
            }

            if (!_playerFuel.IsDepleted)
            {
                Vector3 torque = new Vector3(-inputSample.LookDelta.y, inputSample.LookDelta.x, -inputSample.RollInput);
                if (torque.sqrMagnitude > 0.0001f)
                {
                    torque.x *= _lookAcceleration;
                    torque.y *= _lookAcceleration;
                    torque.z *= _rollAcceleration;
                    _rigidbody.AddRelativeTorque(torque, ForceMode.Acceleration);
                }
            }

            if (_isTetherConstrained)
            {
                _rigidbody.velocity *= 0.995f;
            }

            _rigidbody.velocity = Vector3.ClampMagnitude(_rigidbody.velocity, _maxLinearSpeed);
            _rigidbody.angularVelocity = Vector3.ClampMagnitude(_rigidbody.angularVelocity, _maxAngularSpeed);

            _playerFuel.RecoverWhenIdle(deltaTime, !usedFuelThisFrame);

            CurrentState = new PlayerMotorState(
                _rigidbody.velocity,
                _rigidbody.angularVelocity,
                inputSample.IsBoosting,
                inputSample.IsBraking,
                _playerFuel.IsDepleted,
                _isTetherConstrained);
        }

        public void SetTetherConstrained(bool isConstrained)
        {
            _isTetherConstrained = isConstrained;
        }
    }
}
