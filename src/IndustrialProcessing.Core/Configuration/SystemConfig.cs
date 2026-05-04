using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace IndustrialProcessing.Configuration
{
    public class SystemConfig
    {
        public int WorkerCount { get; set; }
        public int MaxQueueSize { get; set; }
        public List<Job> InitialJobs { get; set; } = new List<Job>();

        public static SystemConfig FromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Konfiguracioni fajl nije pronadjen: " + path);

            var doc = new XmlDocument();
            doc.Load(path);
            return FromDocument(doc);
        }

        public static SystemConfig FromDocument(XmlDocument doc)
        {
            var root = doc.DocumentElement;
            if (root == null)
                throw new FormatException("Prazan konfiguracioni dokument.");

            var workerNode = root.SelectSingleNode("WorkerCount");
            var maxNode = root.SelectSingleNode("MaxQueueSize");
            if (workerNode == null || maxNode == null)
                throw new FormatException("Nedostaju WorkerCount ili MaxQueueSize.");

            var cfg = new SystemConfig();
            cfg.WorkerCount = int.Parse(workerNode.InnerText);
            cfg.MaxQueueSize = int.Parse(maxNode.InnerText);

            var jobNodes = root.SelectNodes("Jobs/Job");
            if (jobNodes != null)
            {
                foreach (XmlNode node in jobNodes)
                {
                    var typeAttr = node.Attributes?["Type"]?.Value;
                    var payload = node.Attributes?["Payload"]?.Value;
                    var prioAttr = node.Attributes?["Priority"]?.Value;

                    if (string.IsNullOrEmpty(typeAttr) || string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(prioAttr))
                        throw new FormatException("Job-u nedostaje neki atribut.");

                    JobType jt;
                    if (!Enum.TryParse<JobType>(typeAttr, true, out jt))
                        throw new FormatException("Nepoznat tip posla: " + typeAttr);

                    int prio = int.Parse(prioAttr);
                    cfg.InitialJobs.Add(new Job(jt, payload, prio));
                }
            }

            return cfg;
        }
    }
}
