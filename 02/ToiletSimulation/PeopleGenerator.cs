using System;
using System.Threading;

namespace ToiletSimulation
{
    public class PeopleGenerator
    {
        private readonly ExponentialRandom exponentialRandom;
        private readonly IQueue queue;
        private readonly Random random;

        private int personId;

        public PeopleGenerator(string name, IQueue queue, int randomSeed)
        {
            Name = name;
            this.queue = queue;
            random = new Random(randomSeed);
            exponentialRandom = new ExponentialRandom(random, 1.0 / Parameters.MeanArrivalTime);
        }

        public string Name { get; }

        public void Produce()
        {
            var thread = new Thread(Run);
            thread.Name = Name;
            thread.Start();
        }

        private void Run()
        {
            personId = 0;
            for (var i = 0; i < Parameters.JobsPerProducer; i++)
            {
                Thread.Sleep((int) Math.Round(exponentialRandom.NextDouble()));
                personId++;
                queue.Enqueue(new Person(random, Name + " - Person " + personId.ToString("00")));
            }

            queue.CompleteAdding();
        }
    }
}