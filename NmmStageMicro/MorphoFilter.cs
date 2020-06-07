namespace NmmStageMicro
{
    public class MorphoFilter
    {
        public MorphoFilter(int[] segmented)
        {
            this.segmented = segmented;
        }

        public int[] FilterWithParameter(int filterParameter)
        {
            if (filterParameter > 0)
            {
                Erode(filterParameter);
                Dilate(filterParameter);
                return segmented;
            }
            if (filterParameter < 0)
            {
                Dilate(-filterParameter);
                Erode(-filterParameter);
                return segmented;
            }
            // filterParameter==0 -> do nothing
            return segmented;
        }

        private void Erode(int filterParameter) // abtragen
        {

        }

        private void Dilate(int filterParameter) // erweitern
        {

        }

        private int[] segmented;
    }
}
