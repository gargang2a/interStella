using InterStella.Game.Features.Fuel;
using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Features.Player
{
    public sealed class PlayerFuel : MonoBehaviour
    {
        [SerializeField]
        private FuelDefinition _definition;

        [SerializeField]
        private bool _startFull = true;

        [SerializeField]
        private float _startingFuel = 100f;

        private float _currentFuel;

        public float CurrentFuel => _currentFuel;
        public float MaxFuel => _definition == null ? 0f : _definition.MaxFuel;
        public bool IsDepleted => _currentFuel <= 0.0001f;

        private void Awake()
        {
            if (_definition == null)
            {
                _currentFuel = 0f;
                return;
            }

            ResetToStartupFuel();
        }

        public void ResetToStartupFuel()
        {
            if (_definition == null)
            {
                _currentFuel = 0f;
                return;
            }

            _currentFuel = _startFull
                ? _definition.MaxFuel
                : Mathf.Clamp(_startingFuel, 0f, _definition.MaxFuel);
        }

        public bool TryConsume(FuelUseReason reason, float deltaTime)
        {
            if (_definition == null)
            {
                return false;
            }

            float rate = _definition.GetConsumptionRate(reason);
            float amount = rate * Mathf.Max(0f, deltaTime);
            if (amount <= 0f)
            {
                return true;
            }

            if (_currentFuel < amount)
            {
                _currentFuel = 0f;
                return false;
            }

            _currentFuel -= amount;
            return true;
        }

        public void RecoverWhenIdle(float deltaTime, bool canRecover)
        {
            if (_definition == null || !canRecover)
            {
                return;
            }

            float recover = _definition.PassiveRecoveryRate * Mathf.Max(0f, deltaTime);
            if (recover <= 0f)
            {
                return;
            }

            _currentFuel = Mathf.Clamp(_currentFuel + recover, 0f, _definition.MaxFuel);
        }

        public FuelState BuildState()
        {
            if (_definition == null)
            {
                return new FuelState(0f, 0f, 0f, 0f);
            }

            return new FuelState(
                _currentFuel,
                _definition.MaxFuel,
                _definition.GetConsumptionRate(FuelUseReason.Thrust),
                _definition.PassiveRecoveryRate);
        }

        public float GetMaximumDeltaRatePerSecond()
        {
            if (_definition == null)
            {
                return 0f;
            }

            float maxConsumptionRate = Mathf.Max(
                _definition.GetConsumptionRate(FuelUseReason.Thrust),
                _definition.GetConsumptionRate(FuelUseReason.Boost),
                _definition.GetConsumptionRate(FuelUseReason.Recovery),
                _definition.GetConsumptionRate(FuelUseReason.ToolAssist));

            return Mathf.Max(maxConsumptionRate, _definition.PassiveRecoveryRate);
        }

        public void SetCurrentFuelAuthoritative(float currentFuel)
        {
            float maxFuel = MaxFuel;
            if (maxFuel <= 0f)
            {
                _currentFuel = 0f;
                return;
            }

            _currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);
        }
    }
}
