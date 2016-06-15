using System.Linq;
using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    public static class HistogramAssert
    {
        public static void AreEqual(HistogramBase expected, HistogramBase actual)
        {
            Assert.AreEqual(expected.GetType(), actual.GetType());
            AreValueEqual(expected, actual);
        }

        public static void AreValueEqual(HistogramBase expected, HistogramBase actual)
        {
            Assert.AreEqual(expected.TotalCount, actual.TotalCount, "Total Counts differ");
            Assert.AreEqual(expected.StartTimeStamp, actual.StartTimeStamp, "StartTimeStamp differ");
            Assert.AreEqual(expected.EndTimeStamp, actual.EndTimeStamp, "EndTimeStamp differ");
            Assert.AreEqual(expected.LowestTrackableValue, actual.LowestTrackableValue, "LowestTrackableValue differ");
            Assert.AreEqual(expected.HighestTrackableValue, actual.HighestTrackableValue, "HighestTrackableValue differ");
            Assert.AreEqual(expected.NumberOfSignificantValueDigits, actual.NumberOfSignificantValueDigits, "NumberOfSignificantValueDigits differ");
            var expectedValues = expected.AllValues().ToArray();
            var actualValues = actual.AllValues().ToArray();
            CollectionAssert.AreEqual(expectedValues, actualValues, HistogramIterationValueComparer.Instance, "Recorded values differ");
        }
    }
}