using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IndustrialProcessing.Reporting
{
    public class ReportWriter
    {
        public const int KeepLast = 10;

        private readonly string outputDir;
        private readonly object diskLock = new object();

        public ReportWriter(string outputDir)
        {
            this.outputDir = outputDir;
            Directory.CreateDirectory(outputDir);
        }

        public string Write(IEnumerable<ExecutionRecord> records)
        {
            var data = records.ToList();

            // broj uspjesno izvrsenih po tipu
            var doneByKind = data
                .Where(r => r.Status == JobStatus.Done)
                .GroupBy(r => r.Kind)
                .Select(g => new { Kind = g.Key, Count = g.Count() })
                .OrderBy(x => x.Kind)
                .ToList();

            // prosjecno trajanje po tipu
            var avgByKind = data
                .Where(r => r.Status == JobStatus.Done)
                .GroupBy(r => r.Kind)
                .Select(g => new { Kind = g.Key, Avg = g.Average(r => r.ElapsedMs) })
                .OrderBy(x => x.Kind)
                .ToList();

            // broj neuspjesnih (aborted) po tipu
            var failedByKind = data
                .Where(r => r.Status == JobStatus.Aborted)
                .GroupBy(r => r.Kind)
                .OrderBy(g => g.Key)
                .Select(g => new { Kind = g.Key, Count = g.Count() })
                .ToList();

            var doc = new XDocument(
                new XElement("ProcessingReport",
                    new XAttribute("createdAt", DateTime.Now.ToString("o")),
                    new XElement("CompletedByKind",
                        doneByKind.Select(x => new XElement("Entry",
                            new XAttribute("kind", x.Kind),
                            new XAttribute("count", x.Count)))),
                    new XElement("AverageElapsedByKind",
                        avgByKind.Select(x => new XElement("Entry",
                            new XAttribute("kind", x.Kind),
                            new XAttribute("ms", x.Avg.ToString("F2", CultureInfo.InvariantCulture))))),
                    new XElement("FailedByKind",
                        failedByKind.Select(x => new XElement("Entry",
                            new XAttribute("kind", x.Kind),
                            new XAttribute("count", x.Count))))));

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            string fileName = "processing_report_" + stamp + ".xml";
            string fullPath = Path.Combine(outputDir, fileName);

            lock (diskLock)
            {
                doc.Save(fullPath);

                // ostavi posljednjih KeepLast, najstarije obrisi
                var existing = Directory.GetFiles(outputDir, "processing_report_*.xml")
                    .OrderBy(f => File.GetCreationTimeUtc(f))
                    .ToList();

                while (existing.Count > KeepLast)
                {
                    try { File.Delete(existing[0]); } catch { }
                    existing.RemoveAt(0);
                }
            }

            return fullPath;
        }
    }
}
