namespace NmmStageMicro
{
    class LineMark
    {

        public LineMark(int tag)
        {
            Tag = tag;
            SampleSize = 0;
        }

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
                EstimatedLineCenter = reducedCenter;
                EstimatedLineWidth = simpleLineMark.LineWidth;
                centerMax = EstimatedLineCenter;
                centerMin = EstimatedLineCenter;
                widthMax = EstimatedLineWidth;
                widthMin = EstimatedLineWidth;
            }
            EstimatedLineCenter += (reducedCenter - EstimatedLineCenter) / SampleSize;
            EstimatedLineWidth += (simpleLineMark.LineWidth - EstimatedLineWidth) / SampleSize;
            if (reducedCenter > centerMax) centerMax = simpleLineMark.LineCenter;
            if (reducedCenter < centerMin) centerMin = simpleLineMark.LineCenter;
            if (simpleLineMark.LineWidth > centerMax) widthMax = simpleLineMark.LineWidth;
            if (simpleLineMark.LineWidth < centerMin) widthMin = simpleLineMark.LineWidth;
        }

        public int SampleSize { get; private set; }
        public int Tag { get; private set; }
        public double EstimatedLineCenter { get; private set; }
        public double EstimatedLineWidth { get; private set; }
        public double LineCenterRange => centerMax - centerMin;
        public double LineWidthRange => widthMax - widthMin;
        public double NominalPosition { get; set; }
        public double Deviation => EstimatedLineCenter - NominalPosition;

        public override string ToString()
        {
            return $"LineMark {Tag}, deviation {Deviation:F3}, width {EstimatedLineWidth:F3}";
        }

        private double centerMax;
        private double centerMin;
        private double widthMin;
        private double widthMax;

    }
}
