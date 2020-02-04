using System;
using System.Collections.Generic;

namespace NmmStageMicro
{
    class MarkFinder
    {

        public MarkFinder(int[] skeleton, double[] xData)
        {
            this.skeleton = skeleton;
            this.xData = xData;
            // first detect edges
            EdgeDetector();
            // find line marks from the edges
            LineFinder();
        }

        public ScaleMarkType MarkType { get; private set; } = ScaleMarkType.Unknown;
        public List<double> LeftEdgePositions { get; private set; } = new List<double>();
        public List<double> RightEdgePositions { get; private set; } = new List<double>();
        public List<double> LineCenterPositions { get; private set; } = new List<double>();
        public List<double> LineWidths { get; private set; } = new List<double>();

        private void EdgeDetector()
        {
            for (int i = 1; i < skeleton.Length; i++)
            {
                if (skeleton[i - 1] == 0 && skeleton[i] == 1)
                    LeftEdgePositions.Add((xData[i - 1] + xData[i]) / 2.0);
                if (skeleton[i - 1] == 1 && skeleton[i] == 0)
                    RightEdgePositions.Add((xData[i - 1] + xData[i]) / 2.0);
            }
        }

        private void LineFinder()
        {
            if (LeftEdgePositions.Count != RightEdgePositions.Count) return;
            if (LeftEdgePositions.Count == 0) return;
            for (int i = 0; i < LeftEdgePositions.Count; i++)
            {
                double position = (LeftEdgePositions[i] + RightEdgePositions[i]) / 2.0;
                double width = Math.Abs(LeftEdgePositions[i] - RightEdgePositions[i]);
                LineCenterPositions.Add(position);
                LineWidths.Add(width);
                if (LeftEdgePositions[i] < RightEdgePositions[i])
                    MarkType = ScaleMarkType.Reflective;
                else
                    MarkType = ScaleMarkType.Transparent;
            }
        }

        private int[] skeleton;
        private double[] xData;

    }

    enum ScaleMarkType
    {
        Unknown,
        NoMark,
        Transparent,
        Reflective
    }

}
