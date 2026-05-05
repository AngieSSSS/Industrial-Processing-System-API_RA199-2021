using System;
using System.IO;
using System.Threading.Tasks;

namespace IndustrialProcessing.Logging
{
    public class AsyncLogger : IDisposable
    {
        private readonly StreamWriter writer;
        private readonly object writeLock = new object();
        private bool disposed;

        public AsyncLogger(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            writer = new StreamWriter(filePath, append: true);
            writer.AutoFlush = true;
        }

        public Task EmitAsync(string status, Guid jobId, object? result)
        {
            string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [" + status + "] " + jobId + ", " + result;

            return Task.Run(() =>
            {
                try
                {
                    lock (writeLock)
                    {
                        if (disposed) return;
                        writer.WriteLine(line);
                    }
                }
                catch
                {
                    // logger ne smije da rusi sistem
                }
            });
        }

        public void Dispose()
        {
            lock (writeLock)
            {
                if (disposed) return;
                disposed = true;
                writer.Dispose();
            }
        }
    }
}
