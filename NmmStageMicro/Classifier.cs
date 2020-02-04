using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NmmStageMicro
{
    class Classifier
    {

        public Classifier(int[] intensity)
        {
            this.intensity = intensity;
        }

        public int[] GetSkeleton(double threshold, int lower, int upper)
        {
            int intThreshold = (int)((double)(upper - lower) * threshold);
            int[] skeleton = new int[intensity.Length];
            for (int i = 0; i < skeleton.Length; i++)
            {
                if (intensity[i] > intThreshold)
                    skeleton[i] = 1;
                else
                    skeleton[i] = 0;
            }
            return skeleton;
        }

        public List<double> GetLeftEdges()
        {

        }



        private int[] intensity;

    }
}
