namespace InterStella.Game.Shared.State
{
    public readonly struct FuelState
    {
        public FuelState(float current, float max, float consumptionRate, float recoveryRate)
        {
            Current = current;
            Max = max;
            ConsumptionRate = consumptionRate;
            RecoveryRate = recoveryRate;
        }

        public float Current { get; }
        public float Max { get; }
        public float ConsumptionRate { get; }
        public float RecoveryRate { get; }
        public bool IsDepleted => Current <= 0.0001f;
        public float Normalized => Max <= 0f ? 0f : Current / Max;
    }
}
