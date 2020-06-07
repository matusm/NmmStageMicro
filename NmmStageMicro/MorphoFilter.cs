using System;

namespace NmmStageMicro
{
    public class MorphoFilter
    {
        private int[] h1;
        private int[] h2;


        public MorphoFilter(int[] originalProfile)
        {
            h1 = new int[originalProfile.Length];
            h2 = new int[originalProfile.Length];
            Array.Copy(originalProfile, h1, originalProfile.Length);
            h2[0] = h1[0];
            h2[h1.Length - 1] = h1[h1.Length - 1];
        }

        public int[] FilterWithParameter(int filterParameter)
        {
            if (filterParameter > 0)
            {
                Erode(filterParameter);
                Dilate(filterParameter);
            }
            if (filterParameter < 0)
            {
                Dilate(-filterParameter);
                Erode(-filterParameter);
            }
            // filterParameter==0 -> do nothing
            return h1;
        }

        private void Erode(int filterParameter) // abtragen
        {
            for (int j = 0; j < filterParameter; j++)
            {
                for (int i = 1; i < (h1.Length - 1); i++)
                {
                    if ((h1[i - 1] + h1[i] + h1[i + 1]) == 3)
                        h2[i] = 1;
                    else
                        h2[i] = 0;
                }
                for (int i = 1; i < (h1.Length - 1); i++)
                    h1[i] = h2[i]; // copy h2 to h1
            }
        }

        private void Dilate(int filterParameter) // erweitern
        {
            for (int j = 0; j < filterParameter; j++)
            {
                for (int i = 1; i < (h1.Length - 1); i++)
                {
                    if ((h1[i - 1] + h1[i] + h1[i + 1]) > 0)
                        h2[i] = 1;
                    else
                        h2[i] = 0;
                }
                for (int i = 1; i < (h1.Length - 1); i++)
                    h1[i] = h2[i]; // copy h2 to h1
            }
        }

    }
}
