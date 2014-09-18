using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTS_Game
{
    public class RandomStuff
    {
        private Random _r;
        private Int32 _seed;

        public RandomStuff(Int32 seed)
        {
            this._seed = seed;
            this._r = new Random(_seed);
        }

        // Simulates the throw of a number of N-sided dice. Returns their sum.
        // Up to ~60k sides, ~60k dice (ushort max).
        public UInt32 NSidedDice(UInt16 sides, UInt16 diceThrown)
        {
            // Validity check; should be redundant.
            if ((sides < 1) | (diceThrown < 1))
            {
                // return 1 to avoid possible divisions by 0?
                return 1;
            }

            UInt32 sum = 0;
            for (UInt16 i = 0; i < diceThrown; i++)
            {
                sum += (UInt32)(this._r.Next(sides) + 1);
            }
            return sum;
        }

        /// <summary>
        /// Returns a sample from N(0,1), obtained via the Box-Muller transform.
        /// </summary>
        /// <returns></returns>
        public double StandardNormalSample()
        {
            // Box-Muller transform: http://en.wikipedia.org/wiki/Box-Muller_transformation
            // Not particularly fast.
            return Math.Sqrt(-2 * Math.Log(this._r.NextDouble())) * Math.Cos(2 * Math.PI * this._r.NextDouble());
        }

        /// <summary>
        /// Returns nearest integer in (min,max) of a sample from N(mu, sigma).
        /// </summary>
        public int DiscreteNormalDistributionSample(double mu, double sigma, int min, int max)
        {
            return Math.Min(Math.Max((int)Math.Round(sigma * this.StandardNormalSample() + mu), min), max);
        }
    }
}
