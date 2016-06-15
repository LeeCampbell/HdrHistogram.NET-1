using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public class ConcurrentHistogramTests : HistogramTestBase
    {
        protected override int WordSize => sizeof(long);

        protected override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new ConcurrentHistogram(1, highestTrackableValue, numberOfSignificantValueDigits);
        }

        protected override HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new ConcurrentHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
        }

        [Test]
        public void Can_add_LongHistogram_with_values_in_range()
        {
            var source = new LongHistogram(int.MaxValue - 1, 3);
            source.RecordValueWithCount(1, 100);
            source.RecordValueWithCount(int.MaxValue - 1, 1000);

            var target = Create(source.LowestTrackableValue, source.HighestTrackableValue, source.NumberOfSignificantValueDigits);
            target.Add(source);

            HistogramAssert.AreValueEqual(source, target);
        }

        [Test]
        public void Can_support_multiple_concurrent_recorders()
        {
            var target = Create(1, long.MaxValue - 1, 3);
            const int loopcount = 10 * 1000 * 1000;
            var concurrency = Environment.ProcessorCount;
            var expected = loopcount*concurrency;
            Action foo = () =>
            {
                for (var i = 0; i < loopcount; i++)
                    target.RecordValue(i);
            };

            var actions = Enumerable.Range(1, concurrency)
                .Select(_ => foo)
                .ToArray();
            Parallel.Invoke(actions);
            

            Assert.AreEqual(expected, target.TotalCount);
        }
    }
}