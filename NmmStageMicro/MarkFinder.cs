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

        public int LineCount => LineMarks.Count;
        public List<double> LeftEdgePositions { get; private set; } = new List<double>();
        public List<double> RightEdgePositions { get; private set; } = new List<double>();
        public List<SimpleLineMark> LineMarks { get; private set; } = new List<SimpleLineMark>();
        
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
                LineMarks.Add(new SimpleLineMark(LeftEdgePositions[i], RightEdgePositions[i]));
        }

        private int[] skeleton;
        private double[] xData;

    }



}
