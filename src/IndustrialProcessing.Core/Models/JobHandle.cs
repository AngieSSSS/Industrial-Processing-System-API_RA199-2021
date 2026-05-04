using System;
using System.Threading.Tasks;

namespace IndustrialProcessing
{
    public class JobHandle
    {
        private readonly TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

        public Guid Id { get; set; }
        public Task<int> Result { get { return tcs.Task; } }

        public JobHandle(Guid id)
        {
            Id = id;
        }

        internal void Complete(int value)
        {
            tcs.TrySetResult(value);
        }

        internal void Abort(Exception? cause)
        {
            tcs.TrySetException(new JobAbortException(Id, cause));
        }
    }
}
