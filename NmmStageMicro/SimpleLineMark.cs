//*******************************************************************************************
//
// Class to store parameters of a single line mark 
//
// A line mark is defined by the position of a left and a right edge position.
// An object of this class is instantiated with the left and right edge
// position of the given line mark.
// The relevant parameters (center position, line width, line type) of the
// given line mark are accesible via properties.
//
// Objects of this class can be compared via the LineCenter property.
//
//*******************************************************************************************

using System;

namespace NmmStageMicro
{
    public class SimpleLineMark : IComparable<SimpleLineMark>
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

        public int CompareTo(SimpleLineMark other)
        {
            return LineCenter.CompareTo(other.LineCenter);
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
