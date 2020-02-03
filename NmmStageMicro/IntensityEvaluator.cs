using System.Linq;

namespace NmmStageMicro
{
    public class IntensityEvaluator
    {

        public IntensityEvaluator(int[] intensities)
        {
            this.intensities = intensities;
            intensityRange = MaxIntensity - MinIntensity;
            CreateHistogram();
        }

        public int MaxIntensity { get { return intensities.Max(); } }

        public int MinIntensity { get { return intensities.Min(); } }

        public int LowerBound { get { return FindPeak(0.0, 0.5); } }

        public int UpperBound { get { return FindPeak(0.5, 1.0); } }

        public int[] Histogram { get; private set; }


        private int[] intensities;
        private int intensityRange;

        private void CreateHistogram()
        {
            Histogram = new int[intensityRange + 1];
            foreach (var intensity in intensities)
            {
                Histogram[intensity - MinIntensity]++;
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
            return peakPosition + MinIntensity;
        }

    }
}
