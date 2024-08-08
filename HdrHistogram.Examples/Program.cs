using System;

namespace HdrHistogram.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            //SimpleHistogramExample.Run();

            //using (var example = new RecorderExample())
            //{
            //    example.Run();
            //}

            var profiler = new Recording32BitBenchmark();
            long dummyValue = 0L;
            for (int i = 0; i < 100; i++)
            {
                dummyValue += profiler.LongHistogramRecording();
            }
            Console.WriteLine(dummyValue);//just to avoid the whole things being a no-op.

        }
    }
}
