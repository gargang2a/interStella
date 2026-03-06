namespace InterStella.Game.Netcode.Runtime
{
    public static class NetworkSequenceComparer
    {
        private const int USHORT_RANGE = 65536;
        private const int USHORT_HALF_RANGE = 32768;
        private const ulong UINT_FULL_MASK = 0xFFFFFFFFUL;
        private const ulong UINT_HALF_RANGE = 0x80000000UL;

        public static bool IsNewer(ushort candidate, ushort reference)
        {
            int delta = (candidate - reference + USHORT_RANGE) % USHORT_RANGE;
            return delta > 0 && delta < USHORT_HALF_RANGE;
        }

        public static bool IsNewer(uint candidate, uint reference)
        {
            ulong delta = ((ulong)candidate - reference) & UINT_FULL_MASK;
            return delta > 0UL && delta < UINT_HALF_RANGE;
        }
    }
}
