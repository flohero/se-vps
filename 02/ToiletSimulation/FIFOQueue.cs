using System.Threading;

namespace ToiletSimulation
{
    public class FIFOQueue : Queue
    {
        private readonly Semaphore _sem =
            new(0, Parameters.Producers * Parameters.JobsPerProducer);

        public override void Enqueue(IJob job)
        {
            lock (queue)
            {
                queue.Add(job);
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