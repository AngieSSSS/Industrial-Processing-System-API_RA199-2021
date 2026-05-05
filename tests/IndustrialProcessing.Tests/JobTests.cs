using System;
using System.Threading.Tasks;
using IndustrialProcessing;
using Xunit;

namespace IndustrialProcessing.Tests
{
    public class JobTests
    {
        [Fact]
        public void Konstruktor_KreiraNoviId()
        {
            var a = new Job(JobType.IO, "delay:10", 1);
            var b = new Job(JobType.IO, "delay:10", 1);
            Assert.NotEqual(a.Id, b.Id);
        }

        [Fact]
        public void Konstruktor_SaZadatimIdZadrzavaGa()
        {
            var id = Guid.NewGuid();
            var j = new Job(id, JobType.Prime, "numbers:10,threads:2", 3);
            Assert.Equal(id, j.Id);
            Assert.Equal(JobType.Prime, j.Type);
            Assert.Equal(3, j.Priority);
        }

        [Fact]
        public async Task JobHandle_Complete_ZavrsavaTask()
        {
            var h = new JobHandle(Guid.NewGuid());
            h.Complete(42);
            int v = await h.Result;
            Assert.Equal(42, v);
        }

        [Fact]
        public async Task JobHandle_Abort_BacaJobAbortException()
        {
            var id = Guid.NewGuid();
            var h = new JobHandle(id);
            h.Abort(new InvalidOperationException("boom"));

            var ex = await Assert.ThrowsAsync<JobAbortException>(async () => await h.Result);
            Assert.Equal(id, ex.JobId);
        }
    }
}
