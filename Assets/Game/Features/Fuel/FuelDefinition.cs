using UnityEngine;

namespace InterStella.Game.Features.Fuel
{
    [CreateAssetMenu(
        fileName = "FuelDefinition",
        menuName = "interStella/Fuel/Fuel Definition")]
    public sealed class FuelDefinition : ScriptableObject
    {
        [Header("Capacity")]
        [SerializeField]
        private float _maxFuel = 100f;

        [SerializeField]
        private float _passiveRecoveryRate = 1f;

        [Header("Consumption Rates")]
        [SerializeField]
        private float _thrustConsumptionRate = 5f;

        [SerializeField]
        private float _boostConsumptionRate = 14f;

        [SerializeField]
        private float _recoveryConsumptionRate = 4f;

        [SerializeField]
        private float _toolAssistConsumptionRate = 6f;

        public float MaxFuel => Mathf.Max(0f, _maxFuel);
        public float PassiveRecoveryRate => Mathf.Max(0f, _passiveRecoveryRate);

        public float GetConsumptionRate(FuelUseReason reason)
        {
            switch (reason)
            {
                case FuelUseReason.Thrust:
                    return Mathf.Max(0f, _thrustConsumptionRate);
                case FuelUseReason.Boost:
                    return Mathf.Max(0f, _boostConsumptionRate);
                case FuelUseReason.Recovery:
                    return Mathf.Max(0f, _recoveryConsumptionRate);
                case FuelUseReason.ToolAssist:
                    return Mathf.Max(0f, _toolAssistConsumptionRate);
                default:
                    return 0f;
            }
        }
    }
}
