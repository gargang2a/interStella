namespace InterStella.Game.Shared.State
{
    public enum TetherTensionLevel
    {
        Slack = 0,
        NearLimit = 1,
        Tension = 2,
        HardLimit = 3,
        Broken = 4
    }

    public readonly struct TetherState
    {
        public TetherState(
            int endpointAId,
            int endpointBId,
            bool isEnabled,
            bool isBroken,
            float maxLength,
            float currentDistance,
            TetherTensionLevel tensionLevel)
        {
            EndpointAId = endpointAId;
            EndpointBId = endpointBId;
            IsEnabled = isEnabled;
            IsBroken = isBroken;
            MaxLength = maxLength;
            CurrentDistance = currentDistance;
            TensionLevel = tensionLevel;
        }

        public int EndpointAId { get; }
        public int EndpointBId { get; }
        public bool IsEnabled { get; }
        public bool IsBroken { get; }
        public float MaxLength { get; }
        public float CurrentDistance { get; }
        public TetherTensionLevel TensionLevel { get; }
    }
}
