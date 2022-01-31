using System;
using System.Linq;

namespace NmmStageMicro
{
    public class IntensityProfile
    {
        public double[] Xvalues { get; }
        public int[] Zvalues { get; }
        public bool IsValid => ProfileIsValid();

        public IntensityProfile(double[] xvalues, int[] zvalues)
        {
            Xvalues = xvalues;
            Zvalues = zvalues;
        }

        public IntensityProfile(double[] xvalues, double[] zvalues) : this (xvalues, zvalues.Select(x => Convert.ToInt32(x)).ToArray()) {}

        private bool ProfileIsValid()
        {
            if (Xvalues == null) return false;
            if (Zvalues == null) return false;
            if (Xvalues.Length != Zvalues.Length) return false;
            if (Xvalues.Length < 2) return false;
            return true;
        }

        public override string ToString()
        {
            if (Xvalues == null) return "[null]";
            return $"[Xvalues:{Xvalues.Length} Yvalues:{Zvalues.Length}]";
        }
    }
}
