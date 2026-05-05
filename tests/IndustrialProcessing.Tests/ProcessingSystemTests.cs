using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndustrialProcessing;
using IndustrialProcessing.Configuration;
using Xunit;

namespace IndustrialProcessing.Tests
{
    public class ProcessingSystemTests
    {
        private static SystemConfig MakeConfig(int workers = 2, int max = 10)
        {
            return new SystemConfig { WorkerCount = workers, MaxQueueSize = max };
        }

        private static string MakeRoot()
        {
            var dir = Path.Combine(Path.GetTempPath(), "ips_" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            return dir;
        }

        [Fact]
        public async Task Submit_IoPosaoSeZavrsi()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(), root);
                var h = sys.Submit(new Job(JobType.IO, "delay:50", 1));
                int r = await h.Result;
                Assert.InRange(r, 0, 100);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public async Task Submit_PrimePosaoSeZavrsi()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(), root);
                var h = sys.Submit(new Job(JobType.Prime, "numbers:20,threads:2", 1));
                int r = await h.Result;
                Assert.Equal(8, r);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void Submit_IstiIdVracaIstiHandle()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(0, 10), root);
                var id = Guid.NewGuid();
                var h1 = sys.Submit(new Job(id, JobType.IO, "delay:10", 1));
                var h2 = sys.Submit(new Job(id, JobType.IO, "delay:10", 1));
                Assert.Same(h1, h2);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void Submit_BacaKadJeRedPun()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(0, 2), root);
                sys.Submit(new Job(JobType.IO, "delay:10", 1));
                sys.Submit(new Job(JobType.IO, "delay:10", 1));

                Assert.Throws<InvalidOperationException>(() =>
                    sys.Submit(new Job(JobType.IO, "delay:10", 1)));
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void GetTopJobs_VracaPoPrioritetu()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(0, 10), root);
                sys.Submit(new Job(JobType.IO, "delay:10", 5));
                sys.Submit(new Job(JobType.IO, "delay:10", 1));
                sys.Submit(new Job(JobType.IO, "delay:10", 3));

                var top = sys.GetTopJobs(3).ToList();
                Assert.Equal(1, top[0].Priority);
                Assert.Equal(3, top[1].Priority);
                Assert.Equal(5, top[2].Priority);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void GetJob_VracaPoId()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(0), root);
                var job = new Job(JobType.IO, "delay:10", 1);
                sys.Submit(job);
                var found = sys.GetJob(job.Id);
                Assert.Equal(job.Id, found.Id);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void GetJob_BacaKadNePostoji()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(0), root);
                Assert.Throws<KeyNotFoundException>(() => sys.GetJob(Guid.NewGuid()));
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public async Task PosaoKojiTrajePredugoSeAbortuje()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(), root);
                sys.TaskTimeout = TimeSpan.FromMilliseconds(200);
                sys.MaxRetries = 3;

                var h = sys.Submit(new Job(JobType.IO, "delay:5000", 1));
                await Assert.ThrowsAsync<JobAbortException>(async () => await h.Result);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public async Task EventJobCompletedSeEmituje()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(), root);
                var seen = new TaskCompletionSource<int>();
                sys.JobCompleted += (s, e) => seen.TrySetResult(e.Result);

                sys.Submit(new Job(JobType.IO, "delay:20", 1));
                int v = await seen.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.InRange(v, 0, 100);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void GenerateReport_PiseFajl()
        {
            var root = MakeRoot();
            try
            {
                using var sys = new ProcessingSystem(MakeConfig(0), root);
                string path = sys.GenerateReport();
                Assert.True(File.Exists(path));
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }
    }
}
