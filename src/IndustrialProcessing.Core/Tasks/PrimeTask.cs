using System;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialProcessing.Tasks
{
    internal static class PrimeTask
    {
        public const int MinThreads = 1;
        public const int MaxThreads = 8;

        public static int Run(string payload, CancellationToken token)
        {
            var (limit, threads) = ParsePayload(payload);
            if (limit < 2) return 0;

            int total = 0;

            var opts = new ParallelOptions();
            opts.MaxDegreeOfParallelism = threads;
            opts.CancellationToken = token;

            Parallel.For(2, limit + 1, opts, n =>
            {
                if (IsPrime(n))
                    Interlocked.Increment(ref total);
            });

            return total;
        }

        public static (int limit, int threads) ParsePayload(string payload)
        {
            var map = PayloadDecoder.ToMap(payload);
            int n = PayloadDecoder.ReadInteger(map, "numbers");
            int t = PayloadDecoder.ReadInteger(map, "threads");

            // ogranicavamo broj niti na [1, 8] kako spec trazi
            if (t < MinThreads) t = MinThreads;
            if (t > MaxThreads) t = MaxThreads;

            return (n, t);
        }

        private static bool IsPrime(int n)
        {
            if (n < 2) return false;
            if (n == 2 || n == 3) return true;
            if (n % 2 == 0) return false;

            int i = 3;
            while ((long)i * i <= n)
            {
                if (n % i == 0) return false;
                i += 2;
            }
            return true;
        }
    }
}
