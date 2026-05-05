using System;
using System.IO;
using System.Xml;
using IndustrialProcessing;
using IndustrialProcessing.Configuration;
using Xunit;

namespace IndustrialProcessing.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void ParsiraValidnuKonfiguraciju()
        {
            var doc = new XmlDocument();
            doc.LoadXml(@"<SystemConfig>
                <WorkerCount>4</WorkerCount>
                <MaxQueueSize>50</MaxQueueSize>
                <Jobs>
                    <Job Type='Prime' Payload='numbers:100,threads:2' Priority='1'/>
                    <Job Type='IO' Payload='delay:300' Priority='3'/>
                </Jobs>
            </SystemConfig>");

            var cfg = SystemConfig.FromDocument(doc);

            Assert.Equal(4, cfg.WorkerCount);
            Assert.Equal(50, cfg.MaxQueueSize);
            Assert.Equal(2, cfg.InitialJobs.Count);
            Assert.Equal(JobType.Prime, cfg.InitialJobs[0].Type);
            Assert.Equal(1, cfg.InitialJobs[0].Priority);
        }

        [Fact]
        public void Baca_KadFajlNePostoji()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
            Assert.Throws<FileNotFoundException>(() => SystemConfig.FromFile(path));
        }

        [Fact]
        public void Baca_KadJeTipNepoznat()
        {
            var doc = new XmlDocument();
            doc.LoadXml(@"<SystemConfig>
                <WorkerCount>1</WorkerCount>
                <MaxQueueSize>1</MaxQueueSize>
                <Jobs><Job Type='Quantum' Payload='x:1' Priority='1'/></Jobs>
            </SystemConfig>");

            Assert.Throws<FormatException>(() => SystemConfig.FromDocument(doc));
        }
    }
}
