using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IndustrialProcessing;
using IndustrialProcessing.Configuration;
using IndustrialProcessing.Logging;

namespace IndustrialProcessing.App
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("== Industrial Processing System ==");

            SystemConfig config;
            try
            {
                string path = args.Length > 0 ? args[0] : "config.xml";
                config = SystemConfig.FromFile(path);
                Console.WriteLine("Konfiguracija: workers=" + config.WorkerCount
                    + ", maxJobs=" + config.MaxQueueSize
                    + ", initialJobs=" + config.InitialJobs.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ne mogu da ucitam konfiguraciju: " + ex.Message);
                return 1;
            }

            string root = AppContext.BaseDirectory;
            string logFile = Path.Combine(root, "logs", "events.log");

            using var logger = new AsyncLogger(logFile);
            using var system = new ProcessingSystem(config, root);

            // pretplate koriscenjem lambda izraza (kako spec trazi)
            system.JobCompleted += (s, e) =>
            {
                Console.WriteLine("[OK] " + e.Job.Type + " " + e.Job.Id + " -> " + e.Result
                    + " (" + e.Elapsed.TotalMilliseconds.ToString("F0") + " ms)");
                _ = logger.EmitAsync("COMPLETED", e.Job.Id, e.Result);
            };

            system.JobFailed += (s, e) =>
            {
                Console.WriteLine("[FAIL] " + e.Job.Type + " " + e.Job.Id
                    + " attempt=" + e.Attempt + " reason=" + e.Reason);

                string status = e.Attempt >= system.MaxRetries ? "ABORT" : "FAILED";
                string detail = status == "ABORT"
                    ? "after " + system.MaxRetries + " attempts"
                    : "attempt=" + e.Attempt + " reason=" + e.Reason;
                _ = logger.EmitAsync(status, e.Job.Id, detail);
            };

            // producer niti - broj iz konfiguracije
            using var producerStop = new CancellationTokenSource();
            var producers = new Task[config.WorkerCount];
            for (int i = 0; i < config.WorkerCount; i++)
            {
                int id = i;
                producers[i] = Task.Run(() => RandomProducer(id, system, producerStop.Token));
            }

            Console.WriteLine("Pokrenuto " + config.WorkerCount + " producer niti. ENTER za stop.");
            Console.ReadLine();

            producerStop.Cancel();
            try { await Task.WhenAll(producers); } catch { }

            try
            {
                string finalReport = system.GenerateReport();
                Console.WriteLine("Finalni izvjestaj: " + finalReport);
                Console.WriteLine("Event log:         " + logFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska kod generisanja izvjestaja: " + ex.Message);
            }

            Console.WriteLine("Gasenje...");
            return 0;
        }

        private static async Task RandomProducer(int id, ProcessingSystem system, CancellationToken token)
        {
            var rng = new Random(Guid.NewGuid().GetHashCode());

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var job = MakeRandomJob(rng);
                    system.Submit(job);
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("[producer " + id + "] red pun - odbijen");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[producer " + id + "] " + ex.GetType().Name + ": " + ex.Message);
                }

                try { await Task.Delay(rng.Next(150, 700), token); }
                catch (OperationCanceledException) { return; }
            }
        }

        private static Job MakeRandomJob(Random rng)
        {
            int prio = rng.Next(1, 4);

            if (rng.Next(2) == 0)
            {
                int n = rng.Next(2000, 40000);
                int t = rng.Next(2, 6);
                return new Job(JobType.Prime, "numbers:" + n + ",threads:" + t, prio);
            }

            int delay = rng.Next(200, 2500);
            return new Job(JobType.IO, "delay:" + delay, prio);
        }
    }
}
