using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using IndustrialProcessing;
using IndustrialProcessing.Logging;
using IndustrialProcessing.Reporting;
using Xunit;

namespace IndustrialProcessing.Tests
{
    public class LoggerAndReportTests
    {
        [Fact]
        public async Task Logger_UpisujeLinijeUFajl()
        {
            var path = Path.Combine(Path.GetTempPath(), "log_" + Guid.NewGuid() + ".log");
            try
            {
                using (var logger = new AsyncLogger(path))
                {
                    await logger.EmitAsync("COMPLETED", Guid.NewGuid(), 42);
                    await logger.EmitAsync("FAILED", Guid.NewGuid(), "reason=TIMEOUT");
                }

                var lines = File.ReadAllLines(path);
                Assert.Equal(2, lines.Length);
                Assert.Contains("[COMPLETED]", lines[0]);
                Assert.Contains("[FAILED]", lines[1]);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Fact]
        public void Report_PraviXmlSaSekcijama()
        {
            var dir = Path.Combine(Path.GetTempPath(), "rep_" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try
            {
                var w = new ReportWriter(dir);
                var data = new[]
                {
                    new ExecutionRecord(Guid.NewGuid(), JobType.Prime, JobStatus.Done, 100, DateTime.Now),
                    new ExecutionRecord(Guid.NewGuid(), JobType.IO, JobStatus.Aborted, 50, DateTime.Now)
                };

                string path = w.Write(data);
                var doc = XDocument.Load(path);

                Assert.NotNull(doc.Root);
                Assert.NotNull(doc.Root!.Element("CompletedByKind"));
                Assert.NotNull(doc.Root.Element("AverageElapsedByKind"));
                Assert.NotNull(doc.Root.Element("FailedByKind"));
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public async Task Report_ZadrzavaSamoPosljednjih10()
        {
            var dir = Path.Combine(Path.GetTempPath(), "rep_" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try
            {
                var w = new ReportWriter(dir);
                var rec = new ExecutionRecord(Guid.NewGuid(), JobType.IO, JobStatus.Done, 1, DateTime.Now);

                for (int i = 0; i < 13; i++)
                {
                    w.Write(new[] { rec });
                    await Task.Delay(5);
                }

                var files = Directory.GetFiles(dir, "processing_report_*.xml");
                Assert.Equal(ReportWriter.KeepLast, files.Length);
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
