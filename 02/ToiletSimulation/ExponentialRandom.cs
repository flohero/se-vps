using System;

namespace ToiletSimulation
{
    public class ExponentialRandom
    {
        private readonly double lambda;
        private readonly Random random;

        public ExponentialRandom(Random random, double lambda)
        {
            this.random = random;
            this.lambda = lambda;
        }

        public double NextDouble()
        {
            return -Math.Log(random.NextDouble()) / lambda;
        }
    }
}