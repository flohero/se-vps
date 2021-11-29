using System;

namespace ToiletSimulation
{
    public class ToiletSimulation
    {
        public static void Main()
        {
            var randomSeed = new Random().Next();
            IQueue q;

            q = new FIFOQueue();
            TestQueue(q, randomSeed);

            // q = new ToiletQueue();
            // TestQueue(q, randomSeed);

            Console.WriteLine("Done.");
        }

        public static void TestQueue(IQueue queue, int randomSeed)
        {
            var random = new Random(randomSeed);

            var producers = new PeopleGenerator[Parameters.Producers];
            for (var i = 0; i < producers.Length; i++)
                producers[i] = new PeopleGenerator("People Generator " + i, queue, random.Next());

            var consumers = new Toilet[Parameters.Consumers];
            for (var i = 0; i < consumers.Length; i++)
                consumers[i] = new Toilet("Toilet " + i, queue);

            Console.WriteLine("Testing " + queue.GetType().Name + ":");

            Analysis.Reset();

            for (var i = 0; i < producers.Length; i++)
                producers[i].Produce();
            for (var i = 0; i < consumers.Length; i++)
                consumers[i].Consume();

            foreach (var consumer in consumers) consumer.Join();

            Analysis.Display();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}