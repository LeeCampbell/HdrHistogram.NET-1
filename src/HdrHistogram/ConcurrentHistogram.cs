using System;
using System.Diagnostics;
using HdrHistogram.Utilities;

namespace HdrHistogram
{
    

public class ConcurrentHistogram : HistogramBase
    {
        private readonly WriterReaderPhaser wrp = new WriterReaderPhaser();
        volatile AtomicLongArrayWithNormalizingOffset activeCounts;
        volatile AtomicLongArrayWithNormalizingOffset inactiveCounts;


        public ConcurrentHistogram(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits) : base(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits)
        {
        }

        public ConcurrentHistogram(long instanceId, long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits) : base(instanceId, lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits)
        {
        }

        public override long TotalCount { get; protected set; }
        protected override int WordSizeInBytes { get; }
        protected override long MaxAllowableCount { get; }
        public override HistogramBase Copy()
        {
            throw new System.NotImplementedException();
        }

        protected override long GetCountAtIndex(int index)
        {
            try
            {
                wrp.ReaderLock();
                
                Debug.Assert(base.CountsArrayLength == activeCounts.Length);
                Debug.Assert(base.CountsArrayLength == inactiveCounts.Length);

                var idx = NormalizeIndex(index, activeCounts.NormalizingIndexOffset, activeCounts.Length);
                long activeCount = activeCounts[idx];
                idx = NormalizeIndex(index, inactiveCounts.NormalizingIndexOffset, inactiveCounts.Length);
                long inactiveCount = inactiveCounts[idx];
                return activeCount + inactiveCount;
            }
            finally
            {
                wrp.ReaderUnlock();
            }
        }

        protected override void SetCountAtIndex(int index, long value)
        {
            throw new System.NotImplementedException();
        }

        protected override void IncrementCountAtIndex(int index)
        {
            long criticalValue = wrp.WriterCriticalSectionEnter();
            try
            {
                activeCounts.incrementAndGet(
                        normalizeIndex(index, activeCounts.getNormalizingIndexOffset(), activeCounts.length()));
            }
            finally
            {
                wrp.WriterCriticalSectionExit(criticalValue);
            }
        }

        protected override void AddToCountAtIndex(int index, long addend)
        {
            throw new System.NotImplementedException();
        }

        protected override void ClearCounts()
        {
            throw new System.NotImplementedException();
        }

        protected override void CopyCountsInto(long[] target)
        {
            throw new System.NotImplementedException();
        }

        //TODO: Blindly ported -LC
        private static int NormalizeIndex(int index, int normalizingIndexOffset, int arrayLength)
        {
            if (normalizingIndexOffset == 0)
            {
                // Fastpath out of normalization. Keeps integer value histograms fast while allowing
                // others (like DoubleHistogram) to use normalization at a cost...
                return index;
            }
            if ((index > arrayLength) || (index < 0))
            {
                throw new IndexOutOfRangeException();
            }
            int normalizedIndex = index - normalizingIndexOffset;
            // The following is the same as an unsigned remainder operation, as long as no double wrapping happens
            // (which shouldn't happen, as normalization is never supposed to wrap, since it would have overflowed
            // or underflowed before it did). This (the + and - tests) seems to be faster than a % op with a
            // correcting if < 0...:
            if (normalizedIndex < 0)
            {
                normalizedIndex += arrayLength;
            }
            else if (normalizedIndex >= arrayLength)
            {
                normalizedIndex -= arrayLength;
            }
            return normalizedIndex;
        }
    }
}