using System.Collections.Generic;
using System.Threading;

namespace ToiletSimulation
{
    public abstract class Queue : IQueue
    {
        protected readonly List<IJob> queue;
        protected int producersComplete;

        protected Queue()
        {
            queue = new List<IJob>();
        }

        public abstract void Enqueue(IJob job);

        public abstract bool TryDequeue(out IJob job);

        public virtual void CompleteAdding()
        {
            Interlocked.Increment(ref producersComplete);
        }

        public virtual bool IsCompleted
        {
            get
            {
                lock (queue)
                {
                    return producersComplete == Parameters.Producers && queue.Count == 0;
                }
            }
        }
    }
}