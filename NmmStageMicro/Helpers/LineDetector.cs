//*******************************************************************************************
//
// Class to analyze a segmented reflectivity profile for edges and lines 
//
// Positions of detected edges are stored in two lists.  
// If lines are detected they are stored as a list od SimpleLineMark objects.
// The skeletized reflectivity profile and the horizontal grid
// must be provided during instantiation.
//
//*******************************************************************************************

using System.Collections.Generic;

namespace NmmStageMicro
{
    class LineDetector
    {
        public LineDetector(int[] segmentedProfile, double[] xData)
        {
            this.segmentedProfile = segmentedProfile;
            this.xData = xData;
            // first detect edges
            EdgeDetector();
            // find line marks from the edges
            LineFinder();
        }

        public List<SimpleLineMark> LineMarks { get; private set; } = new List<SimpleLineMark>();
        public int LineCount => LineMarks.Count;
        public List<double> LeftEdgePositions { get; private set; } = new List<double>();
        public List<double> RightEdgePositions { get; private set; } = new List<double>();

        private void EdgeDetector()
        {
            for (int i = 1; i < segmentedProfile.Length; i++)
            {
                if (segmentedProfile[i - 1] == 0 && segmentedProfile[i] == 1)
                    LeftEdgePositions.Add((xData[i - 1] + xData[i]) / 2.0);
                if (segmentedProfile[i - 1] == 1 && segmentedProfile[i] == 0)
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

        private readonly int[] segmentedProfile;
        private readonly double[] xData;

    }
}
