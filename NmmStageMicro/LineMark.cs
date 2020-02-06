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
// Given a nominal position one may also get a deviationThe relevant parameters (center position, line width, line type) of the
// given line mark are accesible via properties.
//
//*******************************************************************************************

namespace NmmStageMicro
{
    class LineMark
    {

        #region Ctor
        public LineMark(int tag)
        {
            Tag = tag;
            SampleSize = 0;
        }
        #endregion

        #region Properties
        public ScaleMarkType ScaleType => scaleType;
        public int SampleSize { get; private set; }
        public int Tag { get; private set; }
        public double AverageLineCenter { get; private set; }
        public double AverageLineWidth { get; private set; }
        public double LineCenterRange => centerMax - centerMin;
        public double LineWidthRange => widthMax - widthMin;
        public double NominalPosition { get; set; }
        public double Deviation => AverageLineCenter - NominalPosition;
        #endregion

        #region Methods
        public void Update(SimpleLineMark simpleLineMark)
        {
            Update(simpleLineMark, new SimpleLineMark(0, 0));
        }

        public void Update(SimpleLineMark simpleLineMark, SimpleLineMark referenceLineMark)
        {
            double reducedCenter = simpleLineMark.LineCenter - referenceLineMark.LineCenter;
            SampleSize++;
            if(SampleSize==1)
            {
                scaleType = simpleLineMark.LineType;
                AverageLineCenter = reducedCenter;
                AverageLineWidth = simpleLineMark.LineWidth;
                centerMax = AverageLineCenter;
                centerMin = AverageLineCenter;
                widthMax = AverageLineWidth;
                widthMin = AverageLineWidth;
            }
            AverageLineCenter += (reducedCenter - AverageLineCenter) / SampleSize;
            AverageLineWidth += (simpleLineMark.LineWidth - AverageLineWidth) / SampleSize;
            if (reducedCenter > centerMax) centerMax = simpleLineMark.LineCenter;
            if (reducedCenter < centerMin) centerMin = simpleLineMark.LineCenter;
            if (simpleLineMark.LineWidth > centerMax) widthMax = simpleLineMark.LineWidth;
            if (simpleLineMark.LineWidth < centerMin) widthMin = simpleLineMark.LineWidth;
        }
        #endregion

        #region Private stuff
        public override string ToString()
        {
            return $"LineMark {Tag}, deviation {Deviation:F3}, width {AverageLineWidth:F3}";
        }

        private ScaleMarkType scaleType;
        private double centerMax;
        private double centerMin;
        private double widthMin;
        private double widthMax;
        #endregion

    }
}
