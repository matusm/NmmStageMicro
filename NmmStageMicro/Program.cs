using System;
using System.IO;
using System.Linq;
using System.Text;
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
                ConsoleUI.ErrorExit("!Missing input file name", 1);

            // read all relevant scan data
            ConsoleUI.StartOperation("Reading NMM scan files");
            nmmFileNameObject = new NmmFileName(fileNames[0]);
            nmmFileNameObject.SetScanIndex(options.ScanIndex);
            theData = new NmmScanData(nmmFileNameObject);
            ConsoleUI.Done();
            // TODO check if data present
          

            // Check if requested channels are present in raw data
            if (!theData.ColumnPresent(options.XAxisDesignation))
                ConsoleUI.ErrorExit($"!Requested channel {options.XAxisDesignation} not in data files", 2);
            if (!theData.ColumnPresent(options.ZAxisDesignation))
                ConsoleUI.ErrorExit($"!Requested channel {options.ZAxisDesignation} not in data files", 3);

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

            // one must avoid referencing to an line outside of expected number of line marks
            if (options.RefLine < 0) options.RefLine = 0;
            if (options.RefLine >= options.ExpectedTargets) options.RefLine = options.ExpectedTargets - 1;

            // output scan files peculiaritues
            ConsoleUI.WriteLine();
            ConsoleUI.WriteLine($"SpuriousDataLines: {theData.MetaData.SpuriousDataLines}");
            ConsoleUI.WriteLine($"NumberOfGlitchedDataPoints: {theData.MetaData.NumberOfGlitchedDataPoints}");

            // some screen output
            ConsoleUI.WriteLine();
            ConsoleUI.WriteLine($"{theData.MetaData.NumberOfDataPoints} data lines with {theData.MetaData.NumberOfColumnsInFile} channels, organized in {theData.MetaData.NumberOfProfiles} profiles");
            ConsoleUI.WriteLine($"x-axis channel: {options.XAxisDesignation}");
            ConsoleUI.WriteLine($"z-axis channel: {options.ZAxisDesignation}");
            ConsoleUI.WriteLine($"Threshold: {options.Threshold}");
            ConsoleUI.WriteLine($"Morphological filter parameter: {options.Morpho}");
            ConsoleUI.WriteLine($"Trace: {topographyProcessType}");
            ConsoleUI.WriteLine($"Expected number of line marks: {options.ExpectedTargets}");
            ConsoleUI.WriteLine($"Nominal scale division: {options.NominalDivision} um");
            ConsoleUI.WriteLine();

            // evaluate the intensities for ALL profiles == the whole scan field
            ConsoleUI.StartOperation("Classifying intensity data");
            double[] luminanceField = theData.ExtractProfile(options.ZAxisDesignation, 0, topographyProcessType);
            IntensityEvaluator eval = new IntensityEvaluator(DoubleToInt(luminanceField));
            ConsoleUI.Done();
            ConsoleUI.WriteLine();
            ConsoleUI.WriteLine($"Intensity range from {eval.MinIntensity} to {eval.MaxIntensity}");
            ConsoleUI.WriteLine($"Estimated bounds from {eval.LowerBound} to {eval.UpperBound}");
            double relativeSpan = (double)(eval.UpperBound - eval.LowerBound) / (double)(eval.MaxIntensity - eval.MinIntensity) * 100.0;
            ConsoleUI.WriteLine($"({relativeSpan:F1} % of full range)");
            ConsoleUI.WriteLine();

            // prepare object for the overall dimensional result
            LineScale result = new LineScale(options.ExpectedTargets);
            result.SetNominalValues(options.NominalDivision, options.RefLine);

            // the loop over all profiles
            for (int profileIndex = 1; profileIndex <= theData.MetaData.NumberOfProfiles; profileIndex++)
            {
                xData = theData.ExtractProfile(options.XAxisDesignation, profileIndex, topographyProcessType);
                zData = theData.ExtractProfile(options.ZAxisDesignation, profileIndex, topographyProcessType);
                // convert Xdata from meter to micrometer
                for (int i = 0; i < xData.Length; i++)
                    xData[i] = xData[i] * 1.0e6;
                // generate black/white profilen(a skeleton)
                Classifier classifier = new Classifier(DoubleToInt(zData));
                int[] skeleton = classifier.GetSegmentedProfile(options.Threshold, eval.LowerBound, eval.UpperBound);
                // morphological filter
                MorphoFilter filter = new MorphoFilter(skeleton);
                skeleton = filter.FilterWithParameter(options.Morpho);
                // find line marks
                LineDetector marks = new LineDetector(skeleton, xData);
                ConsoleUI.WriteLine($"profile: {profileIndex,3} with {marks.LineCount} line marks {(marks.LineCount != options.ExpectedTargets ? "*" : " ")}");
                result.UpdateSample(marks.LineMarks, options.RefLine);
                // debug *** output edges
                if(options.EdgeOnly)
                { 
                    OutputEdges(marks);
                    //Exit();
                }
                
            }
            // prepare output
            string outFormater = $"F{options.Precision}";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{ConsoleUI.WelcomeMessage}");
            sb.AppendLine($"InputFile            = {theData.MetaData.BaseFileName}");
            // section for sample specific metadata
            sb.AppendLine($"SampleIdentifier     = {theData.MetaData.SampleIdentifier}");
            sb.AppendLine($"SampleSpecies        = {theData.MetaData.SampleSpecies}");
            sb.AppendLine($"SampleSpecification  = {theData.MetaData.SampleSpecification}");
            sb.AppendLine($"ExpectedLineMarks    = {options.ExpectedTargets}");
            sb.AppendLine($"NominalDivision      = {options.NominalDivision} µm");
            sb.AppendLine($"ScaleType            = {result.ScaleType}");
            sb.AppendLine($"ThermalExpansion     = {options.Alpha.ToString("E2")} 1/K");
            // scan file specific data
            sb.AppendLine($"NumberOfScans        = {theData.MetaData.NumberOfScans}");
            sb.AppendLine($"ScanIndex            = {theData.MetaData.ScanIndex}");
            sb.AppendLine($"PointsPerProfile     = {theData.MetaData.NumberOfDataPoints}");
            sb.AppendLine($"Profiles             = {theData.MetaData.NumberOfProfiles}");
            sb.AppendLine($"InputChannels        = {theData.MetaData.NumberOfColumnsInFile}");
            sb.AppendLine($"PointSpacing         = {(theData.MetaData.ScanFieldDeltaX * 1e6).ToString("F4")} µm");
            sb.AppendLine($"ProfileSpacing       = {(theData.MetaData.ScanFieldDeltaY * 1e6).ToString("F4")} µm");
            sb.AppendLine($"ScanFieldCenterX     = {theData.MetaData.ScanFieldCenterX * 1000:F1} mm");
            sb.AppendLine($"ScanFieldCenterY     = {theData.MetaData.ScanFieldCenterY * 1000:F1} mm");
            sb.AppendLine($"ScanFieldCenterZ     = {theData.MetaData.ScanFieldCenterZ * 1000:F1} mm");
            sb.AppendLine($"AngularOrientation   = {theData.MetaData.ScanFieldRotation:F2}°");
            sb.AppendLine($"ScanSpeed            = {theData.MetaData.ScanSpeed} µm/s");
            sb.AppendLine($"GlitchedDataPoints   = {theData.MetaData.NumberOfGlitchedDataPoints}");
            sb.AppendLine($"SpuriousDataLines    = {theData.MetaData.SpuriousDataLines}");
            sb.AppendLine($"Probe                = {theData.MetaData.ProbeDesignation}");
            // evaluation parameters, user supplied
            sb.AppendLine($"X-AxisChannel        = {options.XAxisDesignation}");
            sb.AppendLine($"Z-AxisChannel        = {options.ZAxisDesignation}");
            sb.AppendLine($"Trace                = {topographyProcessType}");
            sb.AppendLine($"Threshold            = {options.Threshold}");
            sb.AppendLine($"FilterParameter      = {options.Morpho}");
            sb.AppendLine($"ReferencedToLine     = {options.RefLine}");
            double maximumThermalCorrection = ThermalCorrection(result.LineMarks.Last().NominalPosition) - ThermalCorrection(result.LineMarks.First().NominalPosition);
            sb.AppendLine($"MaxThermalCorrection = {maximumThermalCorrection:F3} µm");
            // auxiliary values 
            sb.AppendLine($"MinimumIntensity     = {eval.MinIntensity}");
            sb.AppendLine($"MaximumIntensity     = {eval.MaxIntensity}");
            sb.AppendLine($"LowerPlateau         = {eval.LowerBound}");
            sb.AppendLine($"UpperPlateau         = {eval.UpperBound}");
            sb.AppendLine($"RelativeSpan         = {relativeSpan:F1} %");
            sb.AppendLine($"EvaluatedProfiles    = {result.SampleSize}");
            // environmental data
            sb.AppendLine($"SampleTemperature    = {theData.MetaData.SampleTemperature.ToString("F3")} °C");
            sb.AppendLine($"AirTemperature       = {theData.MetaData.AirTemperature.ToString("F3")} °C");
            sb.AppendLine($"AirPressure          = {theData.MetaData.BarometricPressure.ToString("F0")} Pa");
            sb.AppendLine($"AirHumidity          = {theData.MetaData.RelativeHumidity.ToString("F1")} %");
            sb.AppendLine("======================");
            sb.AppendLine("1 : Line number (tag)");
            sb.AppendLine("2 : Nominal value / µm");
            sb.AppendLine("3 : Position deviation / µm");
            sb.AppendLine("4 : Range of line position values / µm");
            sb.AppendLine("5 : Line width / µm");
            sb.AppendLine("6 : Range of line widths / µm");
            sb.AppendLine("@@@@");

            if (result.SampleSize == 0)
            {
                sb.AppendLine("*** No matching intensity pattern found ***");
            }
            else
            {
                foreach (var line in result.LineMarks)
                {
                    double deltaL = ThermalCorrection(line.NominalPosition);

                    sb.AppendLine($"{line.Tag.ToString().PadLeft(5)}" +
                        $"{line.NominalPosition.ToString("F0").PadLeft(10)}" +
                        $"{(line.Deviation + deltaL).ToString(outFormater).PadLeft(10)}" +
                        $"{line.LineCenterRange.ToString(outFormater).PadLeft(10)}" +
                        $"{line.AverageLineWidth.ToString(outFormater).PadLeft(10)}" +
                        $"{(line.LineWidthRange).ToString(outFormater).PadLeft(10)}");
                }
            }

            #region File output
            string outFileName;
            if (fileNames.Length >= 2)
                outFileName = fileNames[1]; // path and extension must be explicitely given
            else
            {
                outFileName = nmmFileNameObject.GetFreeFileNameWithIndex(".prn"); // extension will be added by WriteToFile()
            }
            ConsoleUI.WriteLine();
            ConsoleUI.WritingFile(outFileName);
            StreamWriter hOutFile = File.CreateText(outFileName);
            hOutFile.Write(sb);
            hOutFile.Close();
            ConsoleUI.Done();
            #endregion

        }

        private static void OutputEdges(LineDetector marks)
        {
            Console.WriteLine("position of left edges");
            foreach (var edge in marks.LeftEdgePositions)
            {
                Console.WriteLine($"   {edge:F3} µm");
            }
            Console.WriteLine("position of right edges");
            foreach (var edge in marks.RightEdgePositions)
            {
                Console.WriteLine($"   {edge:F3} µm");
            }
            Console.WriteLine();
        }

        // gives the thermal correction value for a given length (both in the same unit)
        // return value must be added to the given length to obtain the true length
        private static double ThermalCorrection(double length)
        {
            double alpha = options.Alpha;
            double deltaT = theData.MetaData.SampleTemperature - 20.0;
            double deltaL = alpha * length * deltaT;
            return -deltaL;
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
