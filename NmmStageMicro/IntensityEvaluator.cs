﻿using System.Linq;

namespace NmmStageMicro
{
    public class IntensityEvaluator
    {
        #region Ctor
        public IntensityEvaluator(int[] intensities)
        {
            this.intensities = intensities;
            maxIntensity = intensities.Max();
            minIntensity = intensities.Min();
            intensityRange = maxIntensity - minIntensity;
            CreateHistogram();
            // CreateHistogram must be called in advance
            lowerBound = FindPeak(0.0, 0.5);
            upperBound = FindPeak(0.5, 1.0);
        }
        #endregion

        #region Properties
        public int MaxIntensity { get { return maxIntensity; } }

        public int MinIntensity { get { return minIntensity; } }

        public int LowerBound { get { return lowerBound; } }

        public int UpperBound { get { return upperBound; } }

        public int[] Histogram { get; private set; }
        #endregion

        #region Private stuff
        // local fields are necessary
        // using computed properties will make the class extremly slow
        private int[] intensities;
        private int intensityRange;
        private int maxIntensity;
        private int minIntensity;
        private int lowerBound;
        private int upperBound;

        private void CreateHistogram()
        {
            Histogram = new int[intensityRange + 1];
            foreach (var intensity in intensities)
            {
                Histogram[intensity - minIntensity]++;
            }
        }

        private int FindPeak(double relLower, double relUpper)
        {
            int lower = (int)((double)intensityRange * relLower);
            int upper = (int)((double)intensityRange * relUpper);
            // rudimentary error check
            if (lower < 0) lower = 0; ;
            if (upper > intensityRange) upper = intensityRange;
            if (upper < lower)
            {
                int temp = upper;
                upper = lower;
                lower = temp;
            }
            int histMaximum = 0;
            int peakPosition = 0;
            for (int i = lower; i < upper; i++)
            {
                if (Histogram[i] > histMaximum)
                {
                    histMaximum = Histogram[i];
                    peakPosition = i;
                }
            }
            return peakPosition + minIntensity;
        }
        #endregion
    }
}