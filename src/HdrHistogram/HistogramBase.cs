﻿/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */

using System;
using System.Diagnostics;
using HdrHistogram.Utilities;

namespace HdrHistogram
{
    /// <summary>
    /// Base class for High Dynamic Range (HDR) Histograms
    /// </summary>
    /// <remarks>
    /// <see cref="HistogramBase"/> supports the recording and analyzing sampled data value counts across a configurable
    /// integer value range with configurable value precision within the range.
    /// Value precision is expressed as the number of significant digits in the value recording, and provides control over 
    /// value quantization behavior across the value range and the subsequent value resolution at any given level.
    /// <para>
    /// For example, a Histogram could be configured to track the counts of observed integer values between 0 and
    /// 36,000,000,000 while maintaining a value precision of 3 significant digits across that range.
    /// Value quantization within the range will thus be no larger than 1/1,000th (or 0.1%) of any value.
    /// This example Histogram could be used to track and analyze the counts of observed response times ranging between
    /// 1 tick (100 nanoseconds) and 1 hour in magnitude, while maintaining a value resolution of 100 nanosecond up to 
    /// 100 microseconds, a resolution of 1 millisecond(or better) up to one second, and a resolution of 1 second 
    /// (or better) up to 1,000 seconds.
    /// At it's maximum tracked value(1 hour), it would still maintain a resolution of 3.6 seconds (or better).
    /// </para>
    /// </remarks>
    public abstract class HistogramBase
    {
        private readonly int _subBucketHalfCountMagnitude;
        private readonly int _unitMagnitude;
        private readonly long _subBucketMask;
        private readonly int _bucketIndexOffset;

        /// <summary>
        /// Get the configured highestTrackableValue
        /// </summary>
        /// <returns>highestTrackableValue</returns>
        public long HighestTrackableValue { get; }

        /// <summary>
        /// Get the configured lowestTrackableValue
        /// </summary>
        /// <returns>lowestTrackableValue</returns>
        public long LowestTrackableValue { get; }

        /// <summary>
        /// Get the configured numberOfSignificantValueDigits
        /// </summary>
        /// <returns>numberOfSignificantValueDigits</returns>
        public int NumberOfSignificantValueDigits { get; }

        /// <summary>
        /// Gets the total number of recorded values.
        /// </summary>
        public abstract long TotalCount { get; }


        internal int BucketCount { get; }
        internal int SubBucketCount { get; }
        internal int SubBucketHalfCount { get; }

        /// <summary>
        /// The length of the internal array that stores the counts.
        /// </summary>
        internal int CountsArrayLength { get; }

        /// <summary>
        /// Returns the word size of this implementation
        /// </summary>
        protected abstract int WordSizeInBytes { get; }
        
        /// <summary>
        /// Construct a histogram given the lowest and highest values to be tracked and a number of significant decimal digits.
        /// </summary>
        /// <param name="lowestTrackableValue">The lowest value that can be tracked (distinguished from 0) by the histogram.
        /// Must be a positive integer that is &gt;= 1.
        /// May be internally rounded down to nearest power of 2.
        /// </param>
        /// <param name="highestTrackableValue">The highest value to be tracked by the histogram.
        /// Must be a positive integer that is &gt;= (2 * lowestTrackableValue).
        /// </param>
        /// <param name="numberOfSignificantValueDigits">
        /// The number of significant decimal digits to which the histogram will maintain value resolution and separation. 
        /// Must be a non-negative integer between 0 and 5.
        /// </param>
        /// <remarks>
        /// Providing a lowestTrackableValue is useful in situations where the units used for the histogram's values are much 
        /// smaller that the minimal accuracy required.
        /// For example when tracking time values stated in ticks (100 nanoseconds), where the minimal accuracy required is a
        /// microsecond, the proper value for lowestTrackableValue would be 10.
        /// </remarks>
        protected HistogramBase(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            if (lowestTrackableValue < 1) throw new ArgumentException("lowestTrackableValue must be >= 1", nameof(lowestTrackableValue));
            if (highestTrackableValue < 2 * lowestTrackableValue) throw new ArgumentException("highestTrackableValue must be >= 2 * lowestTrackableValue", nameof(highestTrackableValue));
            if ((numberOfSignificantValueDigits < 0) || (numberOfSignificantValueDigits > 5)) throw new ArgumentException("numberOfSignificantValueDigits must be between 0 and 5", nameof(numberOfSignificantValueDigits));

            LowestTrackableValue = lowestTrackableValue;
            HighestTrackableValue = highestTrackableValue;
            NumberOfSignificantValueDigits = numberOfSignificantValueDigits;

            _unitMagnitude = (int)Math.Floor(Math.Log(LowestTrackableValue) / Math.Log(2));

            // We need to maintain power-of-two subBucketCount (for clean direct indexing) that is large enough to
            // provide unit resolution to at least largestValueWithSingleUnitResolution. So figure out
            // largestValueWithSingleUnitResolution's nearest power-of-two (rounded up), and use that:
            var largestValueWithSingleUnitResolution = 2 * (long)Math.Pow(10, NumberOfSignificantValueDigits);
            var subBucketCountMagnitude = (int)Math.Ceiling(Math.Log(largestValueWithSingleUnitResolution) / Math.Log(2));

            _subBucketHalfCountMagnitude = ((subBucketCountMagnitude > 1) ? subBucketCountMagnitude : 1) - 1;
            SubBucketCount = (int)Math.Pow(2, (_subBucketHalfCountMagnitude + 1));
            SubBucketHalfCount = SubBucketCount / 2;
            _subBucketMask = (SubBucketCount - 1) << _unitMagnitude;


            _bucketIndexOffset = 64 - _unitMagnitude - (_subBucketHalfCountMagnitude + 1);


            // determine exponent range needed to support the trackable value with no overflow:
            BucketCount = GetBucketsNeededToCoverValue(HighestTrackableValue);

            CountsArrayLength = GetLengthForNumberOfBuckets(BucketCount);
        }

        /// <summary>
        /// Records a value in the histogram
        /// </summary>
        /// <param name="value">The value to be recorded</param>
        /// <exception cref="System.IndexOutOfRangeException">if value is exceeds highestTrackableValue</exception>
        public void RecordValue(long value)
        {
            RecordSingleValue(value);
        }

        /// <summary>
        /// Record a value in the histogram (adding to the value's current count)
        /// </summary>
        /// <param name="value">The value to be recorded</param>
        /// <param name="count">The number of occurrences of this value to record</param>
        /// <exception cref="System.IndexOutOfRangeException">if value is exceeds highestTrackableValue</exception>
        public void RecordValueWithCount(long value, long count)
        {
            // Dissect the value into bucket and sub-bucket parts, and derive index into counts array:
            var bucketIndex = GetBucketIndex(value);
            var subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            var countsIndex = CountsArrayIndex(bucketIndex, subBucketIndex);
            AddToCountAtIndex(countsIndex, count);
        }

        /// <summary>
        /// Record a value in the histogram.
        /// </summary>
        /// <param name="value">The value to record</param>
        /// <param name="expectedIntervalBetweenValueSamples">If <paramref name="expectedIntervalBetweenValueSamples"/> is larger than 0, add auto-generated value records as appropriate if <paramref name="value"/> is larger than <paramref name="expectedIntervalBetweenValueSamples"/></param>
        /// <exception cref="System.IndexOutOfRangeException">if value is exceeds highestTrackableValue</exception>
        /// <remarks>
        /// To compensate for the loss of sampled values when a recorded value is larger than the expected interval between value samples, 
        /// Histogram will auto-generate an additional series of decreasingly-smaller (down to the expectedIntervalBetweenValueSamples) value records.
        /// <para>
        /// Note: This is a at-recording correction method, as opposed to the post-recording correction method provided by <see cref="CopyCorrectedForCoordinatedOmission"/>.
        /// The two methods are mutually exclusive, and only one of the two should be be used on a given data set to correct for the same coordinated omission issue.
        /// </para>
        /// See notes in the description of the Histogram calls for an illustration of why this corrective behavior is important.
        /// </remarks>
        public void RecordValueWithExpectedInterval(long value, long expectedIntervalBetweenValueSamples)
        {
            RecordValueWithCountAndExpectedInterval(value, 1, expectedIntervalBetweenValueSamples);
        }

        /// <summary>
        /// Reset the contents and stats of this histogram
        /// </summary>
        public void Reset()
        {
            ClearCounts();
        }

        /// <summary>
        /// Add the contents of another histogram to this one.
        /// </summary>
        /// <param name="fromHistogram">The other histogram.</param>
        /// <exception cref="System.IndexOutOfRangeException">if values in fromHistogram's are higher than highestTrackableValue.</exception>
        public virtual void Add(HistogramBase fromHistogram)
        {
            if (HighestTrackableValue < fromHistogram.HighestTrackableValue)
            {
                throw new ArgumentOutOfRangeException(nameof(fromHistogram), $"The other histogram covers a wider range ({fromHistogram.HighestTrackableValue} than this one ({HighestTrackableValue}).");
            }
            if ((BucketCount == fromHistogram.BucketCount) &&
                    (SubBucketCount == fromHistogram.SubBucketCount) &&
                    (_unitMagnitude == fromHistogram._unitMagnitude))
            {
                // Counts arrays are of the same length and meaning, so we can just iterate and add directly:
                for (var i = 0; i < fromHistogram.CountsArrayLength; i++)
                {
                    AddToCountAtIndex(i, fromHistogram.GetCountAtIndex(i));
                }
            }
            else
            {
                // Arrays are not a direct match, so we can't just stream through and add them.
                // Instead, go through the array and add each non-zero value found at it's proper value:
                for (var i = 0; i < fromHistogram.CountsArrayLength; i++)
                {
                    var count = fromHistogram.GetCountAtIndex(i);
                    RecordValueWithCount(fromHistogram.ValueFromIndex(i), count);
                }
            }
        }
        
        /// <summary>
        /// Get the size (in value units) of the range of values that are equivalent to the given value within the histogram's resolution. 
        /// Where "equivalent" means that value samples recorded for any two equivalent values are counted in a common total count.
        /// </summary>
        /// <param name="value">The given value</param>
        /// <returns>The lowest value that is equivalent to the given value within the histogram's resolution.</returns>
        public long SizeOfEquivalentValueRange(long value)
        {
            var bucketIndex = GetBucketIndex(value);
            var subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            if (subBucketIndex >= SubBucketCount)
            {
                //TODO: Validate the conditions under which this is hit. Missing unit test (just copy from Java) -LC
                bucketIndex++;
            }
                
            var distanceToNextValue = 1 << (_unitMagnitude + bucketIndex);
            return distanceToNextValue;
        }

        /// <summary>
        /// Get the lowest value that is equivalent to the given value within the histogram's resolution.
        /// Where "equivalent" means that value samples recorded for any two equivalent values are counted in a common total count.
        /// </summary>
        /// <param name="value">The given value</param>
        /// <returns>The lowest value that is equivalent to the given value within the histogram's resolution.</returns>
        public long LowestEquivalentValue(long value)
        {
            var bucketIndex = GetBucketIndex(value);
            var subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            return ValueFromIndex(bucketIndex, subBucketIndex);
        }

        /// <summary>
        /// Get a value that lies in the middle (rounded up) of the range of values equivalent the given value.
        /// Where "equivalent" means that value samples recorded for any two equivalent values are counted in a common total count.
        /// </summary>
        /// <param name="value">The given value</param>
        /// <returns>The value lies in the middle (rounded up) of the range of values equivalent the given value.</returns>
        public long MedianEquivalentValue(long value)
        {
            return LowestEquivalentValue(value)
                + (SizeOfEquivalentValueRange(value) >> 1);
        }

        /// <summary>
        /// Get the next value that is not equivalent to the given value within the histogram's resolution.
        /// Where "equivalent" means that value samples recorded for any two equivalent values are counted in a common total count.
        /// </summary>
        /// <param name="value">The given value</param>
        /// <returns>The next value that is not equivalent to the given value within the histogram's resolution.</returns>
        public long NextNonEquivalentValue(long value)
        {
            return LowestEquivalentValue(value) + SizeOfEquivalentValueRange(value);
        }
        
        /// <summary>
        /// Get the count of recorded values at a specific value
        /// </summary>
        /// <param name="value">The value for which to provide the recorded count</param>
        /// <returns>The total count of values recorded in the histogram at the given value (to within the histogram resolution at the value level).</returns>
        /// <exception cref="IndexOutOfRangeException">On parameters that are outside the tracked value range</exception>
        public long GetCountAtValue(long value)
        {
            var bucketIndex = GetBucketIndex(value);
            var subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            // May throw ArrayIndexOutOfBoundsException:
            return GetCountAt(bucketIndex, subBucketIndex);
        }

        /// <summary>
        /// Determine if this histogram had any of it's value counts overflow.
        /// </summary>
        /// <returns><c>true</c> if this histogram has had a count value overflow, else <c>false</c>.</returns>
        /// <remarks>
        /// Since counts are kept in fixed integer form with potentially limited range (e.g. int and short), a specific value range count could potentially overflow, leading to an inaccurate and misleading histogram representation.
        /// This method accurately determines whether or not an overflow condition has happened in an <see cref="IntHistogram"/> or <see cref="ShortHistogram"/>.
        /// </remarks>
        public bool HasOverflowed()
        {
            // On overflow, the totalCount accumulated counter will (always) not match the total of counts
            var totalCounted = 0L;
            for (var i = 0; i < CountsArrayLength; i++)
            {
                totalCounted += GetCountAtIndex(i);
                //If adding has wrapped back around (for Long implementations)
                if (totalCounted < 0)
                    return true;
            }
            return (totalCounted != TotalCount);
        }

        /// <summary>
        /// Provide a (conservatively high) estimate of the Histogram's total footprint in bytes
        /// </summary>
        /// <returns>a (conservatively high) estimate of the Histogram's total footprint in bytes</returns>
        public virtual int GetEstimatedFootprintInBytes()
        {
            return (512 + (WordSizeInBytes * CountsArrayLength));
        }

        internal long GetCountAt(int bucketIndex, int subBucketIndex)
        {
            return GetCountAtIndex(CountsArrayIndex(bucketIndex, subBucketIndex));
        }

        internal long ValueFromIndex(int bucketIndex, int subBucketIndex)
        {
            return ((long)subBucketIndex) << (bucketIndex + _unitMagnitude);
        }

        /// <summary>
        /// Gets the number of recorded values at a given index.
        /// </summary>
        /// <param name="index">The index to get the count for</param>
        /// <returns>The number of recorded values at the given index.</returns>
        protected abstract long GetCountAtIndex(int index);

        /// <summary>
        /// Increments the count at the given index. Will also increment the <see cref="TotalCount"/>.
        /// </summary>
        /// <param name="index">The index to increment the count at.</param>
        protected abstract void IncrementCountAtIndex(int index);

        /// <summary>
        /// Adds the specified amount to the count of the provided index. Also increments the <see cref="TotalCount"/> by the same amount.
        /// </summary>
        /// <param name="index">The index to increment.</param>
        /// <param name="addend">The amount to increment by.</param>
        protected abstract void AddToCountAtIndex(int index, long addend);

        /// <summary>
        /// Clears the counts of this implementation.
        /// </summary>
        protected abstract void ClearCounts();

        private void RecordSingleValue(long value)
        {
            // Dissect the value into bucket and sub-bucket parts, and derive index into counts array:
            var bucketIndex = GetBucketIndex(value);
            var subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            var countsIndex = CountsArrayIndex(bucketIndex, subBucketIndex);
            IncrementCountAtIndex(countsIndex);
        }

        private void RecordValueWithCountAndExpectedInterval(long value, long count, long expectedIntervalBetweenValueSamples)
        {
            RecordValueWithCount(value, count);
            if (expectedIntervalBetweenValueSamples <= 0)
                return;
            for (var missingValue = value - expectedIntervalBetweenValueSamples;
                 missingValue >= expectedIntervalBetweenValueSamples;
                 missingValue -= expectedIntervalBetweenValueSamples)
            {
                RecordValueWithCount(missingValue, count);
            }
        }

        private int GetBucketsNeededToCoverValue(long value)
        {
            long trackableValue = (SubBucketCount - 1) << _unitMagnitude;
            var bucketsNeeded = 1;
            while (trackableValue < value)
            {
                trackableValue <<= 1;
                bucketsNeeded++;
            }
            return bucketsNeeded;
        }

        private int GetLengthForNumberOfBuckets(int numberOfBuckets)
        {
            var lengthNeeded = (numberOfBuckets + 1) * (SubBucketCount / 2);
            return lengthNeeded;
        }

        private int CountsArrayIndex(int bucketIndex, int subBucketIndex)
        {
            Debug.Assert(subBucketIndex < SubBucketCount);
            Debug.Assert(bucketIndex == 0 || (subBucketIndex >= SubBucketHalfCount));
            // Calculate the index for the first entry in the bucket:
            // (The following is the equivalent of ((bucketIndex + 1) * subBucketHalfCount) ):
            var bucketBaseIndex = (bucketIndex + 1) << _subBucketHalfCountMagnitude;
            // Calculate the offset in the bucket:
            var offsetInBucket = subBucketIndex - SubBucketHalfCount;
            // The following is the equivalent of ((subBucketIndex  - subBucketHalfCount) + bucketBaseIndex;
            return bucketBaseIndex + offsetInBucket;
        }

        //Optimization. This simple method should be in-lined by the JIT compiler, allowing hot path `GetBucketIndex(long, long, int)` to become static. -LC
        private int GetBucketIndex(long value)
        {
            return GetBucketIndex(value, _subBucketMask, _bucketIndexOffset);
        }
        private static int GetBucketIndex(long value, long subBucketMask, int bucketIndexOffset)
        {
            var leadingZeros = Bitwise.NumberOfLeadingZeros(value | subBucketMask); // smallest power of 2 containing value
            return bucketIndexOffset - leadingZeros;
        }

        private int GetSubBucketIndex(long value, int bucketIndex)
        {
            return (int)(value >> (bucketIndex + _unitMagnitude));
        }

        private long ValueFromIndex(int index)
        {
            var bucketIndex = (index >> _subBucketHalfCountMagnitude) - 1;
            var subBucketIndex = (index & (SubBucketHalfCount - 1)) + SubBucketHalfCount;
            if (bucketIndex < 0)
            {
                subBucketIndex -= SubBucketHalfCount;
                bucketIndex = 0;
            }
            return ValueFromIndex(bucketIndex, subBucketIndex);
        }
    }
}
