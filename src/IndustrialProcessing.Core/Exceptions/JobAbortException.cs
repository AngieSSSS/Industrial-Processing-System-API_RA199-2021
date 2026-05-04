using System;

namespace IndustrialProcessing
{
    public class JobAbortException : Exception
    {
        public Guid JobId;

        public JobAbortException(Guid jobId, Exception? inner)
            : base("Posao " + jobId + " je odbacen.", inner)
        {
            JobId = jobId;
        }
    }
}
