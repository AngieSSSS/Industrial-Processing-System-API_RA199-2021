using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndustrialProcessing.Configuration;
using IndustrialProcessing.Reporting;
using IndustrialProcessing.Tasks;

namespace IndustrialProcessing
{
    public class ProcessingSystem : IDisposable
    {
        public TimeSpan TaskTimeout { get; set; } = TimeSpan.FromSeconds(2);
        public int MaxRetries { get; set; } = 3;

        // bucket po prioritetu (FIFO unutar svakog buckta)
        private readonly Dictionary<int, Queue<Job>> buckets = new Dictionary<int, Queue<Job>>();
        private readonly Dictionary<Guid, JobHandle> handles = new Dictionary<Guid, JobHandle>();
        private readonly Dictionary<Guid, Job> jobs = new Dictionary<Guid, Job>();
        private readonly List<ExecutionRecord> records = new List<ExecutionRecord>();

        private readonly object queueLock = new object();
        private readonly object recordsLock = new object();
        private readonly SemaphoreSlim signal = new SemaphoreSlim(0);

        private readonly int capacity;
        private int totalPending;

        public event EventHandler<JobCompletedEventArgs>? JobCompleted;
        public event EventHandler<JobFailedEventArgs>? JobFailed;

        private readonly Task[] workers;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private readonly ReportWriter reporter;
        private readonly Timer reportTimer;
        private bool disposed;

        public ProcessingSystem(SystemConfig config, string? outputRoot = null)
        {
            capacity = config.MaxQueueSize;
            outputRoot = outputRoot ?? AppContext.BaseDirectory;
            reporter = new ReportWriter(Path.Combine(outputRoot, "reports"));

            // ucitaj inicijalne poslove prije nego sto pokrenemo workere
            foreach (var j in config.InitialJobs)
                EnqueueJob(j);

            workers = new Task[config.WorkerCount];
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(() => WorkerLoop(shutdown.Token));
            }

            // izvjestaj se generise svake minute
            reportTimer = new Timer(_ => SafeWriteReport(), null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public JobHandle Submit(Job job)
        {
            var h = EnqueueJob(job);
            if (h == null)
                throw new InvalidOperationException("Red je popunjen, posao je odbijen.");
            return h;
        }

        public Job GetJob(Guid id)
        {
            lock (queueLock)
            {
                if (jobs.ContainsKey(id))
                    return jobs[id];
            }
            throw new KeyNotFoundException("Posao sa Id " + id + " nije pronadjen.");
        }

        public IEnumerable<Job> GetTopJobs(int n)
        {
            if (n <= 0) return new List<Job>();

            lock (queueLock)
            {
                var top = new List<Job>();
                var sortedKeys = buckets.Keys.OrderBy(k => k).ToList();
                foreach (var key in sortedKeys)
                {
                    foreach (var j in buckets[key])
                    {
                        top.Add(j);
                        if (top.Count >= n) return top;
                    }
                }
                return top;
            }
        }

        public int PendingCount
        {
            get { lock (queueLock) return totalPending; }
        }

        public IReadOnlyCollection<ExecutionRecord> History
        {
            get { lock (recordsLock) return records.ToArray(); }
        }

        public string GenerateReport()
        {
            ExecutionRecord[] snapshot;
            lock (recordsLock) snapshot = records.ToArray();
            return reporter.Write(snapshot);
        }

        private JobHandle? EnqueueJob(Job job)
        {
            lock (queueLock)
            {
                // idempotentnost - ako je vec u sistemu, vrati postojeci handle
                if (handles.ContainsKey(job.Id))
                    return handles[job.Id];

                if (totalPending >= capacity)
                    return null;

                var handle = new JobHandle(job.Id);
                handles[job.Id] = handle;
                jobs[job.Id] = job;

                if (!buckets.ContainsKey(job.Priority))
                    buckets[job.Priority] = new Queue<Job>();
                buckets[job.Priority].Enqueue(job);
                totalPending++;

                signal.Release();
                return handle;
            }
        }

        private Job? TryDequeueHighest()
        {
            lock (queueLock)
            {
                if (totalPending == 0) return null;
                int top = buckets.Keys.OrderBy(k => k).First();
                var job = buckets[top].Dequeue();
                if (buckets[top].Count == 0)
                    buckets.Remove(top);
                totalPending--;
                return job;
            }
        }

        private async Task WorkerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try { await signal.WaitAsync(token); }
                catch (OperationCanceledException) { return; }

                var job = TryDequeueHighest();
                if (job == null) continue;

                JobHandle handle;
                lock (queueLock)
                {
                    if (!handles.ContainsKey(job.Id)) continue;
                    handle = handles[job.Id];
                }

                ExecuteWithRetry(job, handle, token);
            }
        }

        private void ExecuteWithRetry(Job job, JobHandle handle, CancellationToken token)
        {
            TimeSpan total = TimeSpan.Zero;

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                if (token.IsCancellationRequested)
                {
                    handle.Abort(new OperationCanceledException("Sistem se gasi."));
                    return;
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TaskTimeout);
                var sw = Stopwatch.StartNew();

                try
                {
                    int result = Dispatch(job, cts.Token);
                    sw.Stop();

                    lock (recordsLock)
                    {
                        records.Add(new ExecutionRecord(job.Id, job.Type, JobStatus.Done,
                            sw.Elapsed.TotalMilliseconds, DateTime.Now));
                    }
                    handle.Complete(result);
                    try { JobCompleted?.Invoke(this, new JobCompletedEventArgs(job, result, sw.Elapsed)); }
                    catch { /* greska u subscriber-u ne smije da rusi worker */ }
                    return;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    handle.Abort(new OperationCanceledException("Sistem se gasi."));
                    return;
                }
                catch (Exception err)
                {
                    sw.Stop();
                    total += sw.Elapsed;
                    string reason = err is OperationCanceledException ? "TIMEOUT" : "ERROR";
                    try { JobFailed?.Invoke(this, new JobFailedEventArgs(job, attempt, reason, err)); }
                    catch { /* greska u subscriber-u ne smije da rusi worker */ }

                    if (attempt == MaxRetries)
                    {
                        lock (recordsLock)
                        {
                            records.Add(new ExecutionRecord(job.Id, job.Type, JobStatus.Aborted,
                                total.TotalMilliseconds, DateTime.Now));
                        }
                        handle.Abort(err);
                        return;
                    }
                }
            }
        }

        private static int Dispatch(Job job, CancellationToken token)
        {
            switch (job.Type)
            {
                case JobType.Prime: return PrimeTask.Run(job.Payload, token);
                case JobType.IO: return IoTask.Run(job.Payload, token);
                default: throw new NotSupportedException("Nepoznat tip posla: " + job.Type);
            }
        }

        private void SafeWriteReport()
        {
            try
            {
                ExecutionRecord[] snapshot;
                lock (recordsLock) snapshot = records.ToArray();
                reporter.Write(snapshot);
            }
            catch { }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            try { reportTimer.Dispose(); } catch { }
            try { shutdown.Cancel(); } catch { }
            try { Task.WaitAll(workers, TimeSpan.FromSeconds(2)); } catch { }

            signal.Dispose();
            shutdown.Dispose();
        }
    }

    public class JobCompletedEventArgs : EventArgs
    {
        public Job Job { get; set; }
        public int Result { get; set; }
        public TimeSpan Elapsed { get; set; }

        public JobCompletedEventArgs(Job job, int result, TimeSpan elapsed)
        {
            Job = job;
            Result = result;
            Elapsed = elapsed;
        }
    }

    public class JobFailedEventArgs : EventArgs
    {
        public Job Job { get; set; }
        public int Attempt { get; set; }
        public string Reason { get; set; }
        public Exception Cause { get; set; }

        public JobFailedEventArgs(Job job, int attempt, string reason, Exception cause)
        {
            Job = job;
            Attempt = attempt;
            Reason = reason;
            Cause = cause;
        }
    }
}
