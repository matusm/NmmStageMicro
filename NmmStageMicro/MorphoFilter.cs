using System;
namespace NmmStageMicro
{
    public class MorphoFilter
    {
        public MorphoFilter(int[] skeleton)
        {
            this.skeleton = skeleton;
        }

        public int[] FilterWithParameter(int filterParameter)
        {
            Erode(filterParameter);
            Dilate(filterParameter);

            return skeleton;
        }

        private void Erode(int filterParameter) // abtragen
        {

        }

        private void Dilate(int filterParameter) // erweitern
        {

        }

        private int[] skeleton;
    }
}
