using System;
using System.Threading;

namespace IndustrialProcessing.Tasks
{
    internal static class IoTask
    {
        private const int Slice = 100;

        public static int Run(string payload, CancellationToken token)
        {
            int delay = ReadDelay(payload);

            // sleep u manjim parcicima da bi mogao da se prekine na timeout
            int passed = 0;
            while (passed < delay)
            {
                int step = Math.Min(Slice, delay - passed);
                Thread.Sleep(step);
                token.ThrowIfCancellationRequested();
                passed += step;
            }

            // simulacija citanja stanja - vrijednost izmedju 0 i 100
            return new Random().Next(0, 101);
        }

        public static int ReadDelay(string payload)
        {
            var map = PayloadDecoder.ToMap(payload);
            int delay = PayloadDecoder.ReadInteger(map, "delay");
            if (delay < 0)
                throw new FormatException("delay mora biti >= 0");
            return delay;
        }
    }
}
