using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public static class Round
    {
        public static double RoundUpToStep(double value, double step)
        {
            return Math.Ceiling(value / step) * step;
        }

        public static double RoundToStep(double value, double step)
        {
            return Math.Round(value / step) * step;
        }

        public static double RoundDownToStep(double value, double step)
        {
            return Math.Floor(value / step) * step;
        }
    }
}
