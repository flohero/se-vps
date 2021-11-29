using System;
using System.Threading;

namespace ToiletSimulation
{
    public class Person : IJob
    {
        private Person()
        {
        }

        public Person(Random random, string id)
        {
            Id = id;
            CreationDate = DateTime.Now;
            WaitingTime = TimeSpan.MaxValue;
            ProcessedDate = DateTime.MaxValue;

            // calculate due date (normally distributed)
            var dueTimeRandom = new NormalRandom(random, Parameters.MeanDueTime, Parameters.StdDeviationDueTime);
            var dueTime = TimeSpan.FromMilliseconds((int) Math.Round(dueTimeRandom.NextDouble()));
            DueDate = CreationDate + dueTime;

            // calculate required processing time (normally distributed)
            var processingTimeRandom = new NormalRandom(random, Parameters.MeanProcessingTime,
                Parameters.StdDeviationProcessingTime);
            double processingTime;
            do
            {
                processingTime = processingTimeRandom.NextDouble();
            } while (processingTime <= 0.0);

            ProcessingTime = TimeSpan.FromMilliseconds((int) Math.Round(processingTime));
        }

        public string Id { get; }
        public DateTime CreationDate { get; }
        public DateTime DueDate { get; }
        public TimeSpan ProcessingTime { get; }
        public TimeSpan WaitingTime { get; private set; }
        public DateTime ProcessedDate { get; private set; }

        public void Process()
        {
            WaitingTime = DateTime.Now - CreationDate;

            if (Parameters.DisplayJobProcessing) Console.WriteLine(Id + ": Processing ...   ");
            Thread.Sleep(ProcessingTime); // simulate processing

            ProcessedDate = DateTime.Now;

            if (Parameters.DisplayJobProcessing)
            {
                if (ProcessedDate <= DueDate)
                    Console.WriteLine(Id + ": Ahhhhhhh, much better ...");
                else
                    Console.WriteLine(Id + ": OOOOh no, too late ......");
            }

            Analysis.CountJob(this);
        }
    }
}