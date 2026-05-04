using System;

namespace IndustrialProcessing
{
    public class Job
    {
        public Guid Id { get; set; }
        public JobType Type { get; set; }
        public string Payload { get; set; } = "";
        public int Priority { get; set; }

        public Job() { Id = Guid.NewGuid(); }

        public Job(JobType type, string payload, int priority)
        {
            Id = Guid.NewGuid();
            Type = type;
            Payload = payload;
            Priority = priority;
        }

        public Job(Guid id, JobType type, string payload, int priority)
        {
            Id = id;
            Type = type;
            Payload = payload;
            Priority = priority;
        }

        public override string ToString()
        {
            return $"Job [{Type}] prio={Priority} id={Id}";
        }
    }
}
