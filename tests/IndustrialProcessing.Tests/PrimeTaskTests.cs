using System.Threading;
using IndustrialProcessing.Tasks;
using Xunit;

namespace IndustrialProcessing.Tests
{
    public class PrimeTaskTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(10, 4)]
        [InlineData(20, 8)]
        public void Run_BrojiProsteBrojeve(int upTo, int expected)
        {
            int count = PrimeTask.Run("numbers:" + upTo + ",threads:2", CancellationToken.None);
            Assert.Equal(expected, count);
        }

        [Fact]
        public void ParsePayload_ClampThreads()
        {
            var (_, t1) = PrimeTask.ParsePayload("numbers:10,threads:0");
            var (_, t2) = PrimeTask.ParsePayload("numbers:10,threads:100");
            Assert.Equal(PrimeTask.MinThreads, t1);
            Assert.Equal(PrimeTask.MaxThreads, t2);
        }
    }
}
