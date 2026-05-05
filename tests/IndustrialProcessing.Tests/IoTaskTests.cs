using System;
using System.Threading;
using IndustrialProcessing.Tasks;
using Xunit;

namespace IndustrialProcessing.Tests
{
    public class IoTaskTests
    {
        [Fact]
        public void Run_VracaBrojIzmedju0i100()
        {
            int v = IoTask.Run("delay:10", CancellationToken.None);
            Assert.InRange(v, 0, 100);
        }

        [Fact]
        public void ReadDelay_BacaNaNegativan()
        {
            Assert.Throws<FormatException>(() => IoTask.ReadDelay("delay:-5"));
        }

        [Fact]
        public void Run_PrekidaSeNaTimeout()
        {
            using var cts = new CancellationTokenSource(150);
            Assert.ThrowsAny<OperationCanceledException>(() => IoTask.Run("delay:5000", cts.Token));
        }
    }
}
