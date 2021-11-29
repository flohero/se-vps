using System;

namespace ToiletSimulation
{
    public class NormalRandom
    {
        private readonly double mu;
        private readonly Random random;
        private readonly double sigma;

        public NormalRandom(Random random, double mu, double sigma)
        {
            this.random = random;
            this.mu = mu;
            this.sigma = sigma;
        }

        public double NextDouble()
        {
            var a = random.NextDouble();
            var b = random.NextDouble();
            var c = Math.Sqrt(-2.0 * Math.Log(a)) * Math.Cos(2.0 * Math.PI * b);

            return c * sigma + mu;
        }
    }
}