namespace HdrHistogram.Benchmarking.LeadingZeroCount
{

    public static class IntrinsicOperations
    {
        public static int GetLeadingZeroCount(long value)
        {
            
#if NET5_0_OR_GREATER
            ulong testValue = (ulong)value;
            return System.Numerics.BitOperations.LeadingZeroCount(testValue);
#else
            return HdrHistogram.Utilities.Bitwise.NumberOfLeadingZeros(value);
#endif
        }
    }

}