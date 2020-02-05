//*******************************************************************************************
//
// Class to store parameters of a single line mark 
//
// An object is instantiated  with the left and right edge position of the given line mark.
// The parameters (center position, line width, line type) of the line mark 
// are accesible via properties.
//
// Objects of this class can be compared via the LineCenter property.
//
//*******************************************************************************************


using System;

namespace NmmStageMicro
{
    public class SimpleLineMark : IComparable
    {
        public SimpleLineMark(double leftPos, double rightPos)
        {
            LeftEdgePosition = leftPos;
            RightEdgePosition = rightPos;
            if (LeftEdgePosition < RightEdgePosition)
                LineType = ScaleMarkType.Reflective;
            else
                LineType = ScaleMarkType.Transparent;
        }

        public double LeftEdgePosition { get; private set; }
        public double RightEdgePosition { get; private set; }
        public double LineCenter => (LeftEdgePosition + RightEdgePosition) / 2.0;
        public double LineWidth => Math.Abs(LeftEdgePosition - RightEdgePosition);
        public ScaleMarkType LineType { get; private set; }

        public int CompareTo(object obj)
        {
            return LineCenter.CompareTo(obj);
        }
    }

    public enum ScaleMarkType
    {
        Unknown,
        NoMark,
        Transparent,
        Reflective
    }
}
