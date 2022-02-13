using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ParallelNumericalIntegration_TPL
{
    class Program
    {
        private static double F(double x) => 4.0 / (1.0 + x * x);

        private static double IntegrateSeq(int n, double a, double b)
        {
            var sum = 0.0;
            var w = (b - a) / n;
            for (var i = 0; i < n; i++)
            {
                sum += w * F(a + w * (i + 0.5));
            }

            return sum;
        }

        private static double IntegrateTpl(int n, double a, double b)
        {
            var sum = 0.0;
            var w = (b - a) / n;
            object locker = new();
            Parallel.For(0, n, (i, state) =>
            {
                lock (locker)
                {
                    sum += w * F(a + w * (i + 0.5));
                }
            });
            return sum;
        }

        private static double IntegrateTpl2(int n, double a, double b)
        {
            var sum = 0.0;
            var w = (b - a) / n;
            object locker = new();
            Parallel.For(0, n,
                () => 0.0,
                (i, state, partialSum) => partialSum + w * F(a + w * (i + 0.5)),
                partialSum =>
                {
                    lock (locker)
                    {
                        sum += partialSum;
                    }
                }
            );
            return sum;
        }

        private static double IntegrateTpl3(int n, double a, double b)
        {
            var sum = 0.0;
            var w = (b - a) / n;
            object locker = new();

            var rangePartitioner = Partitioner.Create(0, n);
            Parallel.ForEach(rangePartitioner,
                () => 0.0,
                (range, state, partialSum) =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        partialSum += w * F(a + w * (i + 0.5));
                    }

                    return partialSum;
                },
                partialSum =>
                {
                    lock (locker)
                    {
                        sum += partialSum;
                    }
                }
            );
            return sum;
        }

        private static void Main(string[] args)
        {

            var sw = new Stopwatch();
            for (var n = 1; n <= 1_000_000_001; n *= 10)
            {
                Console.WriteLine($"Width: {n}");
                
                sw.Start();
                var resSeq = IntegrateSeq(n, 0.0, 1.0);
                sw.Stop();
                var timeSeq = sw.Elapsed;
                Console.WriteLine($"Seq\t{resSeq:F10}\t{timeSeq}");

                sw.Start();
                var resTpl = IntegrateTpl3(n, 0.0, 1.0);
                sw.Stop();
                var timeTpl = sw.Elapsed;
                Console.WriteLine($"TPL \t{resTpl:F10}\t{timeTpl}");
                Console.WriteLine($"Speedup: {timeSeq / timeTpl}");
            }
        }
    }
}