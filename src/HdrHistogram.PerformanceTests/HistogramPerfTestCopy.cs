using System;
using System.Diagnostics;
using System.Threading;
using HdrHistogram.Utilities;
using NUnit.Framework;

namespace HdrHistogram.PerformanceTests
{
    //This is a copy of the Java perf tests so that we can make side by side comparisons.
    [TestFixture]
    public class HistogramPerfTestCopy
    {
        private const long HighestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units
        private const int NumberOfSignificantValueDigits = 3;
        private const long TestValueLevel = 12340;
        private const long WarmupLoopLength = 50000;
        private const long RawTimingLoopCount = 500000000L;
        //private const long rawDoubleTimingLoopCount = 300000000L;
        //private const long singleWriterIntervalTimingLoopCount = 100000000L;
        //private const long singleWriterDoubleIntervalTimingLoopCount = 100000000L;
        //private const long intervalTimingLoopCount = 40000000L;
        //private const long synchronizedTimingLoopCount = 180000000L;
        //private const long atomicTimingLoopCount = 80000000L;
        //private const long concurrentTimingLoopCount = 50000000L;


        [Test]
        public void TestRawRecordingSpeed()
        {
            HistogramBase histogram = new LongHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            //System.out.println("\n\nTiming Histogram:");
            TestRawRecordingSpeedAtExpectedInterval("Histogram: ", histogram, 1000000000, RawTimingLoopCount);
        }

        [Test]
        public void TestLeadingZerosSpeed()
        {
            Measure("LeadingZeroCount", l => LeadingZerosSpeedLoop(l), WarmupLoopLength, true);
            Reset();
            Measure("LeadingZeroCount", l => LeadingZerosSpeedLoop(l), RawTimingLoopCount, false);
        }

        private static void RecordLoopWithExpectedInterval(HistogramBase histogram, long loopCount, long expectedInterval)
        {
            for (long i = 0; i < loopCount; i++)
                histogram.RecordValueWithExpectedInterval(TestValueLevel + (i & 0x8000), expectedInterval);
        }

        private static long LeadingZerosSpeedLoop(long loopCount)
        {
            long sum = 0;
            for (long i = 0; i < loopCount; i++)
            {
                // long val = testValueLevel + (i & 0x8000);
                long val = TestValueLevel;
                sum += Bitwise.NumberOfLeadingZeros(val);
                sum += Bitwise.NumberOfLeadingZeros(val);
                sum += Bitwise.NumberOfLeadingZeros(val);
                sum += Bitwise.NumberOfLeadingZeros(val);
                sum += Bitwise.NumberOfLeadingZeros(val);
                sum += Bitwise.NumberOfLeadingZeros(val);
                sum += Bitwise.NumberOfLeadingZeros(val);
                sum += Bitwise.NumberOfLeadingZeros(val);
            }
            return sum;
        }

        private static void TestRawRecordingSpeedAtExpectedInterval(string label,
            HistogramBase histogram,
            long expectedInterval,
            long timingLoopCount)
        {
            Measure(label, loopLength => RecordLoopWithExpectedInterval(histogram, loopLength, expectedInterval), WarmupLoopLength, true);

            histogram.Reset();
            Reset();

            Measure(label, loopLength => RecordLoopWithExpectedInterval(histogram, loopLength, expectedInterval), timingLoopCount, false);
            histogram.Reset();
        }

        private static void Reset()
        {
            // Wait a bit to make sure compiler had a cache to do it's stuff            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(1000);
        }

        private static void Measure(string label, Action<long> action, long loopLength, bool isWarmup)
        {
            var startTime = Stopwatch.GetTimestamp();
            action(loopLength);
            var endTime = Stopwatch.GetTimestamp();
            var elapsed = TimeSpan.FromTicks(endTime - startTime);

            var foreground = Console.ForegroundColor;
            if (isWarmup)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("Warmup ");
            }

            Console.WriteLine($"{label} {loopLength} ops completed in {elapsed}, rate = {loopLength / elapsed.TotalSeconds} ops/sec.");
            Console.ForegroundColor = foreground;
        }
    }
}
