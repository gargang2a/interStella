namespace InterStella.Game.Shared.State
{
    public readonly struct RepairObjectiveState
    {
        public RepairObjectiveState(int requiredCount, int deliveredCount, bool isLocked)
        {
            RequiredCount = requiredCount;
            DeliveredCount = deliveredCount;
            IsLocked = isLocked;
        }

        public int RequiredCount { get; }
        public int DeliveredCount { get; }
        public bool IsLocked { get; }
        public bool IsCompleted => DeliveredCount >= RequiredCount;
        public float Normalized => RequiredCount <= 0 ? 1f : (float)DeliveredCount / RequiredCount;
    }
}
