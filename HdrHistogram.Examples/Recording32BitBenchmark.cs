namespace HdrHistogram.Examples
{
    public class Recording32BitBenchmark
    {
        private readonly LongHistogram _longHistogram;
        private readonly long _highestTrackableValue = TimeStamp.Minutes(10);
        public Recording32BitBenchmark()
        {
            const int numberOfSignificantValueDigits = 3;

            _longHistogram = new LongHistogram(_highestTrackableValue, numberOfSignificantValueDigits);
        }

        public long LongHistogramRecording()
        {
            long counter = 0L;

            for (long i = 0; i <_highestTrackableValue; i++)
            {
                _longHistogram.RecordValue(i);
                counter += i;
            }
            return counter;
        }

    //    public long LongConcurrentHistogramRecording()
    //    {
    //        long counter = 0L;
    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            var value = _testValues[i];
    //            _longConcurrentHistogram.RecordValue(value);
    //            counter += value;
    //        }
    //        return counter;
    //    }

    //    public long IntHistogramRecording()
    //    {
    //        long counter = 0L;
    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            var value = _testValues[i];
    //            _intHistogram.RecordValue(value);
    //            counter += value;
    //        }
    //        return counter;
    //    }

    //    public long IntConcurrentHistogramRecording()
    //    {
    //        long counter = 0L;
    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            var value = _testValues[i];
    //            _intConcurrentHistogram.RecordValue(value);
    //            counter += value;
    //        }
    //        return counter;
    //    }

    //    public long ShortHistogramRecording()
    //    {
    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            _shortHistogram.RecordValue(_testValues[i]);
    //        }
    //        return _shortHistogram.TotalCount;
    //    }

    //    public long LongRecorderRecording()
    //    {
    //        long counter = 0L;

    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            var value = _testValues[i];
    //            _longRecorder.RecordValue(value);
    //            counter += value;
    //        }
    //        return counter;
    //    }

    //    public long LongConcurrentRecorderRecording()
    //    {
    //        long counter = 0L;

    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            var value = _testValues[i];
    //            _longConcurrentRecorder.RecordValue(value);
    //            counter += value;
    //        }
    //        return counter;
    //    }

    //    public long IntRecorderRecording()
    //    {
    //        long counter = 0L;
    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            var value = _testValues[i];
    //            _intRecorder.RecordValue(value);
    //            counter += value;
    //        }
    //        return counter;
    //    }

    //    public long IntConcurrentRecorderRecording()
    //    {
    //        long counter = 0L;
    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            var value = _testValues[i];
    //            _intConcurrentRecorder.RecordValue(value);
    //            counter += value;
    //        }
    //        return counter;
    //    }

        
    //    public long ShortRecorderRecording()
    //    {
    //        long counter = 0L;
    //        for (int i = 0; i < _testValues.Length; i++)
    //        {
    //            var value = _testValues[i];
    //            _shortRecorder.RecordValue(value);
    //            counter += value;
    //        }
    //        return counter;
    //    }
    }
}
