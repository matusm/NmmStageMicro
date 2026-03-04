//*******************************************************************************************
//
// Class to evaluate and store parameters of a single line mark
// The parameters are estimated by multiple samples of SimpleLineMark objects.
//
// The position of the line centers are referenced to the position 
// of a given line for each single sample.
// An object of this class is instantiated with an integer tag.
// This tag is completely arbitray but may be used as an index.
//
// the most important properties are:
// - AverageLineCenter
// - AverageLineWidth
// - LineCenterRange
// - LineWidthRange
// - NominalPosition
// - Deviation
// - SampleSize
// 
// Given a nominal position one may also get a deviation. The relevant parameters 
// (center position, line width, line type) of the given line mark are 
// accesible via properties.
//
// Attention! when referencing to a line other than #0, center line positions are
// offset by the difference of the nominal values!
// this must be corrected for in LineScale object!
//
//*******************************************************************************************

using At.Matus.StatisticPod;

namespace NmmStageMicro
{
    public class LineMark
    {
        private readonly StatisticPod stpCenter;
        private readonly StatisticPod stpWidth;

        public LineMark(int tag)
        {
            Tag = tag;
            stpCenter = new StatisticPod(tag.ToString());
            stpWidth = new StatisticPod(tag.ToString());
        }

        public double NominalPosition { get; set; }
        public ScaleMarkType ScaleType { get; private set; }
        public int SampleSize => (int)stpCenter.SampleSize;
        public int Tag { get; }
        public double AverageLineCenter => stpCenter.AverageValue;
        public double AverageLineWidth => stpWidth.AverageValue;
        public double LineCenterRange => stpCenter.Range;
        public double LineWidthRange => stpWidth.Range;
        public double LineCenterStdDev => stpCenter.StandardDeviation;
        public double LineWidthStdDev => stpWidth.StandardDeviation;
        public double Deviation => AverageLineCenter - NominalPosition;

        public void Update(SimpleLineMark simpleLineMark)
        {
            Update(simpleLineMark, new SimpleLineMark(0, 0));
        }

        public void Update(SimpleLineMark simpleLineMark, SimpleLineMark referenceLineMark)
        {
            double reducedCenter = simpleLineMark.LineCenter - referenceLineMark.LineCenter;
            ScaleType = simpleLineMark.LineType;
            stpCenter.Update(reducedCenter);
            stpWidth.Update(simpleLineMark.LineWidth);
        }

        public override string ToString()
        {
            return $"LineMark {Tag}, deviation {Deviation:F3}, width {AverageLineWidth:F3}";
        }

    }
}
