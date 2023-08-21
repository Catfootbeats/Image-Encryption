using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEncryption.Core
{
    class SeedRandom
    {
        private Random random;

        public SeedRandom(int seed)
        {
            random = new Random(seed);
        }

        public int Next(int maxValue)
        {
            return random.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }

        public double NextDouble(double min,double max)
        { 
            double range = max - min;
            double sample = random.NextDouble();
            double scaled = (sample * range) + min;
            float f = (float)scaled;
            return f;
        }
    }

}
