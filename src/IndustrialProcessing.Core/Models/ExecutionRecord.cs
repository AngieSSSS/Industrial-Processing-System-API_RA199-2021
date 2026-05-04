using System;

namespace IndustrialProcessing
{
    public enum JobStatus
    {
        Done,
        Aborted
    }

    public class ExecutionRecord
    {
        public Guid JobId { get; set; }
        public JobType Kind { get; set; }
        public JobStatus Status { get; set; }
        public double ElapsedMs { get; set; }
        public DateTime Timestamp { get; set; }

        public ExecutionRecord(Guid jobId, JobType kind, JobStatus status, double elapsedMs, DateTime timestamp)
        {
            JobId = jobId;
            Kind = kind;
            Status = status;
            ElapsedMs = elapsedMs;
            Timestamp = timestamp;
        }
    }
}
