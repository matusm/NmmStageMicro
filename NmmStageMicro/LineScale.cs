//*******************************************************************************************
//
// Class to store and evaluate parameters of a whole line scale 
//
// A line scale is basically an array of LineMark objects. 
// This class must be instantiated with the expected number of line marks.
//
// One can set the nominal values of each line with a single method
// The class can handle both, equidistant and irregular, scales.
//
//*******************************************************************************************

using System.Collections.Generic;

namespace NmmStageMicro
{
    class LineScale
    {

        #region Ctor
        public LineScale(int numberOfLineMarks)
        {
            this.numberOfLineMarks = numberOfLineMarks;
            LineMarks = new LineMark[numberOfLineMarks];
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                LineMarks[i] = new LineMark(i);
            }
        }
        #endregion

        #region Properties
        public LineMark[] LineMarks { get; }
        public int SampleSize => LineMarks[0].SampleSize;
        public ScaleMarkType ScaleType => LineMarks[0].ScaleType;
        #endregion

        #region Methods
        // updates all line marks at once with a given sample, referenced to a given line
        public void UpdateSample(List<SimpleLineMark> simpleLineMarks, int referenceIndex)
        {
            if (simpleLineMarks.Count != numberOfLineMarks) return;
            if (referenceIndex < 0) return;
            if (referenceIndex >= numberOfLineMarks) return;
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                LineMarks[i].Update(simpleLineMarks[i], simpleLineMarks[referenceIndex]);
            }
        }

        // updates all line marks at once with a given sample
        public void UpdateSample(List<SimpleLineMark> simpleLineMarks)
        {
            if (simpleLineMarks.Count != numberOfLineMarks) return;
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                LineMarks[i].Update(simpleLineMarks[i]);
            }
        }

        // set nominal line values for irregular scales 
        public void SetNominalValues(List<double> nominalValues)
        {
            if (nominalValues.Count != numberOfLineMarks) return;
            //nominalValues.Sort();
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                LineMarks[i].NominalPosition=nominalValues[i];
            }
        }

        // set nominal line values for equidistant lines
        public void SetNominalValues(double division)
        {
            for (int i = 0; i < numberOfLineMarks; i++)
            {
                LineMarks[i].NominalPosition = (double)i*division;
            }
        }
        #endregion

        private readonly int numberOfLineMarks;
    }
}
