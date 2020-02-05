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
            Erode(filterParameter);
            Dilate(filterParameter);

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
