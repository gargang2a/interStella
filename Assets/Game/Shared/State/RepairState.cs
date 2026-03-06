namespace InterStella.Game.Shared.State
{
    public readonly struct RepairState
    {
        public RepairState(int requiredCount, int deliveredCount)
        {
            RequiredCount = requiredCount;
            DeliveredCount = deliveredCount;
        }

        public int RequiredCount { get; }
        public int DeliveredCount { get; }
        public bool IsCompleted => DeliveredCount >= RequiredCount;
        public float NormalizedProgress => RequiredCount <= 0 ? 1f : (float)DeliveredCount / RequiredCount;
    }
}
