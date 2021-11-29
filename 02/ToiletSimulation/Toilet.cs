using System;
using System.Threading;

namespace ToiletSimulation
{
    public class Toilet
    {
        private readonly IQueue queue;
        private Thread _consumer;

        public Toilet(string name, IQueue queue)
        {
            Name = name;
            this.queue = queue;
        }

        public string Name { get; }

        public void Consume()
        {
            _consumer = new Thread(Run);
            _consumer.Start();
        }

        private void Run()
        {
            while (!queue.IsCompleted)
                if (queue.TryDequeue(out var job))
                    job.Process();
                else
                    Console.WriteLine("Toilet is starving :-/");
        }

        public void Join()
        {
            _consumer.Join();
        }
    }
}