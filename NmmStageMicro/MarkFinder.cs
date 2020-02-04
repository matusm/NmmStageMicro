using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NmmStageMicro
{
    class MarkFinder
    {
        public MarkFinder(int[] skeleton, double[] xData)
        {
            this.skeleton = skeleton;
            this.xData = xData;
            EdgeFinder();
        }

        private void EdgeFinder()
        {

            throw new NotImplementedException();
        }

        private List<double> leftEdges;

        private int[] skeleton;
        private double[] xData;

    }
}
