using NUnit.Framework;

namespace HdrHistogram.UnitTests.Persistence
{
    [TestFixture]
    public sealed class ConcurrentHistogramLogReaderWriterTests : HistogramLogReaderWriterTestBase
    {
        protected override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new ConcurrentHistogram(1, highestTrackableValue, numberOfSignificantValueDigits);
        }

        [Test, TestCaseSource(typeof(TestCaseGenerator), nameof(TestCaseGenerator.PowersOfTwo), new object[] { 63 })]
        public void CanRoundTripSingleHistogramsWithFullRangesOfCountsAndValues(long count)
        {
            RoundTripSingleHistogramsWithFullRangesOfCountsAndValues(count);
        }
    }
}