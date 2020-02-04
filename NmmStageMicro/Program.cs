using System;
using System.Linq;
using Bev.IO.NmmReader;
using Bev.IO.NmmReader.scan_mode;

namespace NmmStageMicro
{
    class MainClass
    {
        static readonly Options options = new Options();
        static NmmFileName nmmFileNameObject;
        static NmmScanData theData;
        static TopographyProcessType topographyProcessType;
        static string[] fileNames;
        static double[] xData;
        static double[] zData;

        public static void Main(string[] args)
        {
            // parse command line arguments
            if (!CommandLine.Parser.Default.ParseArgumentsStrict(args, options))
                Console.WriteLine("*** ParseArgumentsStrict returned false");
            // consume the verbosity option
            if (options.BeQuiet == true)
                ConsoleUI.BeSilent();
            else
                ConsoleUI.BeVerbatim();
            // print a welcome message
            ConsoleUI.Welcome();
            ConsoleUI.WriteLine();
            // get the filename(s)
            fileNames = options.ListOfFileNames.ToArray();
            if (fileNames.Length == 0)
                ConsoleUI.ErrorExit("!Missing input file", 1);

            // read all relevant scan data
            ConsoleUI.StartOperation("Reading and evaluating files");
            nmmFileNameObject = new NmmFileName(fileNames[0]);
            nmmFileNameObject.SetScanIndex(options.ScanIndex);
            theData = new NmmScanData(nmmFileNameObject);
            ConsoleUI.Done();


            // Check if requested channels present in raw data
            if (!theData.ColumnPresent(options.XAxisDesignation))
                ConsoleUI.ErrorExit($"!Channel {options.XAxisDesignation} not in data files", 2);
            if (!theData.ColumnPresent(options.ZAxisDesignation))
                ConsoleUI.ErrorExit($"!Channel {options.ZAxisDesignation} not in data files", 3);

            topographyProcessType = TopographyProcessType.ForwardOnly;
            if (options.UseBack)
                topographyProcessType = TopographyProcessType.BackwardOnly;
            if (options.UseBoth)
                topographyProcessType = TopographyProcessType.Average;
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.ForwardOnly)
            {
                if (topographyProcessType != TopographyProcessType.ForwardOnly)
                    ConsoleUI.WriteLine("No backward scan data present, switching to forward only.");
                topographyProcessType = TopographyProcessType.ForwardOnly;
            }
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.Unknown)
                ConsoleUI.ErrorExit("!Unknown scan type", 4);
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.NoData)
                ConsoleUI.ErrorExit("!No scan data present", 5);

            // some screen output
            ConsoleUI.WriteLine();
            ConsoleUI.WriteLine($"{theData.MetaData.NumberOfDataPoints} data lines with {theData.MetaData.NumberOfColumnsInFile} channels, organized in {theData.MetaData.NumberOfProfiles} profiles");
            ConsoleUI.WriteLine($"x-axis channel: {options.XAxisDesignation}");
            ConsoleUI.WriteLine($"z-axis channel: {options.ZAxisDesignation}");
            ConsoleUI.WriteLine($"threshold: {options.Threshold}");
            ConsoleUI.WriteLine($"morphological filter parameter: {options.Morpho}");
            ConsoleUI.WriteLine($"trace: {topographyProcessType}");
            ConsoleUI.WriteLine($"expected number of line marks: {options.ExpectedTargets}");
            ConsoleUI.WriteLine($"nominal scale division: {options.NominalDivision} um");
            ConsoleUI.WriteLine();

            // evaluate the intensities for ALL profiles
            ConsoleUI.StartOperation("Classifying intensity data");
            double[] luminanceField = theData.ExtractProfile(options.ZAxisDesignation, 0, topographyProcessType);
            IntensityEvaluator eval = new IntensityEvaluator(DoubleToInt(luminanceField));
            ConsoleUI.Done();
            ConsoleUI.WriteLine();
            ConsoleUI.WriteLine($"intensity range from {eval.MinIntensity} to {eval.MaxIntensity}");
            ConsoleUI.WriteLine($"estimated bounds from {eval.LowerBound} to {eval.UpperBound}");
            double relativeSpan = (double)(eval.UpperBound - eval.LowerBound) / (double)(eval.MaxIntensity - eval.MinIntensity) * 100.0;
            ConsoleUI.WriteLine($"({relativeSpan:F1} % of full range)");
            ConsoleUI.WriteLine();

            // the loop over all profiles
            for (int profileIndex = 1; profileIndex <= theData.MetaData.NumberOfProfiles; profileIndex++)
            {
                xData = theData.ExtractProfile(options.XAxisDesignation, profileIndex, topographyProcessType);
                zData = theData.ExtractProfile(options.ZAxisDesignation, profileIndex, topographyProcessType);
                // convert Xdata from meter to micrometer
                for (int i = 0; i < xData.Length; i++)
                    xData[i] *= 1.0e6;
                // generate black/white profile
                Classifier classifier = new Classifier(DoubleToInt(zData));
                int[] skeleton = classifier.GetSkeleton(options.Threshold, eval.LowerBound, eval.UpperBound);
                // morphological filter
                MorphoFilter filter = new MorphoFilter(skeleton);
                
                // find line marks
                MarkFinder marks = new MarkFinder(skeleton, xData);
                ConsoleUI.WriteLine($"profile: {profileIndex} with {marks.LineCenterPositions.Count} line marks");
            }
        }

        private static int[] DoubleToInt(double[] rawIntensities)
        {
            int[] result = new int[rawIntensities.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (int)rawIntensities[i];
            }
            return result;
        }

    }

}
