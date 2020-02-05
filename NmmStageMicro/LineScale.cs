//*******************************************************************************************
//
// Class to store and evaluate parameters of a whole line scale 
//
// An object is instantiated with the expected number of line marks.
//
// The parameters (center position, line width, line type) of the line scale
// are accesible via properties.
//
//*******************************************************************************************

using System.Collections.Generic;

namespace NmmStageMicro
{
    class LineScale
    {
        public LineScale(int numberOfLineMarks)
        {
            this.numberOfLineMarks = numberOfLineMarks;
            lineMarks = new LineMark[numberOfLineMarks];
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                lineMarks[i] = new LineMark(i);
            }
        }

        public LineMark[] LineMarks => lineMarks;

        public void UpdateSample(List<SimpleLineMark> simpleLineMarks, int referenceIndex)
        {
            if (simpleLineMarks.Count != numberOfLineMarks) return;
            if (referenceIndex < 0) return;
            if (referenceIndex >= numberOfLineMarks) return;
            //simpleLineMarks.Sort();
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                lineMarks[i].Update(simpleLineMarks[i], simpleLineMarks[referenceIndex]);
            }
        }

        // set nominal line values for irregular scales 
        public void SetNominalValues(List<double> nominalValues)
        {
            if (nominalValues.Count != numberOfLineMarks) return;
            //nominalValues.Sort();
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                lineMarks[i].NominalPosition=nominalValues[i];
            }
        }

        // set nominal line values for equidistant lines
        public void SetNominalValues(double division)
        {
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                lineMarks[i].NominalPosition = (double)i*division;
            }
        }

        private LineMark[] lineMarks;
        private int numberOfLineMarks;
    }
}
