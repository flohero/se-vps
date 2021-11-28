using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RaceConditions
{
    public class Program
    {
        private static readonly object Locker = new();

        private static void Main()
        {
            Console.WriteLine("With Race Condition");
            WithRaceCondition();
            Console.WriteLine();
            
            Console.WriteLine("Without Race Condition");
            WithoutRaceCondition();
        }

        private static void WithRaceCondition()
        {
            var i = 0;
            var startTime = DateTime.Now.AddSeconds(1);
            var t1 = new Thread(() =>
            {
                while (DateTime.Now <= startTime)
                {
                }
                i++;
                Console.WriteLine("Thread 1");
            });
            var t2 = new Thread(() =>
            {
                while (DateTime.Now <= startTime)
                {
                }
                i++;
                Console.WriteLine("Thread 2");
            });
            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
            Console.WriteLine($"Actual: {i}; Expected {2}");
        }

        private static void WithoutRaceCondition()
        {
            var i = 0;

            void IncrementI()
            {
                lock (Locker)
                    i++;
            }

            var startTime = DateTime.Now.AddSeconds(1);
            var t1 = new Thread(() =>
            {
                while (DateTime.Now <= startTime)
                {
                }

                IncrementI();
                Console.WriteLine("Thread 1");
            });
            var t2 = new Thread(() =>
            {
                while (DateTime.Now <= startTime)
                {
                }

                IncrementI();
                Console.WriteLine("Thread 2");
            });
            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
            Console.WriteLine($"Actual: {i}; Expected {2}");
        }
    }
}