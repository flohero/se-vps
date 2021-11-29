using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace ToiletSimulation
{
    public class ToiletQueue : Queue
    {
        private readonly Semaphore _sem =
            new(0, Parameters.Producers * Parameters.JobsPerProducer);
        public override void Enqueue(IJob job)
        {
            lock (queue)
            {
                int i;
                for (i = 0; i < queue.Count && job.DueDate.CompareTo(queue[i].DueDate) > 0; i++)
                {
                }

                queue.Insert(i, job);
            }

            _sem.Release();
        }

        public override bool TryDequeue(out IJob job)
        {
            _sem.WaitOne();
            lock (queue)
            {
                if (queue.Count > 0)
                {
                    job = queue[0];
                    queue.RemoveAt(0);
                    return true;
                }

                job = null;
                return false;
            }
        }

        public override void CompleteAdding()
        {
            base.CompleteAdding();
            if (producersComplete == Parameters.Producers) _sem.Release(Parameters.Consumers);
        }
    }
}