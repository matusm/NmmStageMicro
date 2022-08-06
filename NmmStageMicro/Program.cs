using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bev.IO.NmmReader;
using Bev.IO.NmmReader.scan_mode;
using CommandLine;
using CommandLine.Text;

namespace NmmStageMicro
{
    class MainClass
    {

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Parser parser = new Parser(with => with.HelpWriter = null);
            ParserResult<Options> parserResult = parser.ParseArguments<Options>(args);
            parserResult
                .WithParsed<Options>(options => Run(options))
                .WithNotParsed(errs => DisplayHelp(parserResult, errs));
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            foreach (var err in errs)
            {
                Console.WriteLine(err);
            }
            HelpText helpText = HelpText.AutoBuild(result, h =>
            {
                h.AutoVersion = false;
                h.AdditionalNewLineAfterOption = false;
                h.AddPreOptionsLine(" ");
                h.AddPreOptionsLine("Program to evaluate scanning files by SIOS NMM-1 for calibrating stage micrometers using the laser focus probe. For multiple profiles the line marks are detected separatly and average position are calculated. The number of line marks must be provided (via -n option). The nominal scale divison (-d) is needed for evaluation of deviations.");
                h.AddPreOptionsLine("");
                h.AddPreOptionsLine($"Usage: {appName} InputPath [OutPath] [options]");
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }

        private static Options opts = new Options();
        private static NmmScanData theData;
        private static readonly StringBuilder edgesOnlyOutput = new StringBuilder();

        private static void SetVerbosity(Options options)
        {
            if (options.BeQuiet == true)
                ConsoleUI.BeSilent();
            else
                ConsoleUI.BeVerbatim();
            ConsoleUI.WriteLine(HeadingInfo.Default);
            ConsoleUI.WriteLine(CopyrightInfo.Default);
            ConsoleUI.WriteLine();
        }

        private static void Run(Options options)
        {
            opts = options;

            SetVerbosity(options);

            ConsoleUI.StartOperation("Reading NMM scan files");
            NmmFileName nmmFileNameObject = new NmmFileName(options.InputPath);
            nmmFileNameObject.SetScanIndex(options.ScanIndex);
            theData = new NmmScanData(nmmFileNameObject);
            ConsoleUI.Done();

            // check if data present
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.Unknown)
                ConsoleUI.ErrorExit("!Unknown scan type", 4);
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.NoData)
                ConsoleUI.ErrorExit("!No scan data present", 5);

            // Check if requested channels are present in raw data
            if (!theData.ColumnPresent(options.XAxisDesignation))
                ConsoleUI.ErrorExit($"!Requested channel {options.XAxisDesignation} not in data files", 2);
            if (!theData.ColumnPresent(options.ZAxisDesignation))
                ConsoleUI.ErrorExit($"!Requested channel {options.ZAxisDesignation} not in data files", 3);

            // one must avoid referencing to an line outside of expected number of line marks
            if (options.RefLine < 0) options.RefLine = 0;
            if (options.RefLine >= options.ExpectedTargets) options.RefLine = options.ExpectedTargets - 1;

            // output scan files peculiarities
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
            if (options.LineScale)
            {
                ConsoleUI.WriteLine($"Expected number of line marks: {options.ExpectedTargets}");
                ConsoleUI.WriteLine($"Nominal scale division: {options.NominalDivision} um");
            }
            ConsoleUI.WriteLine();

            // evaluate the intensities for ALL profiles == the whole scan field
            ConsoleUI.StartOperation("Classifying intensity data");
            double[] luminanceField = theData.ExtractProfile(options.ZAxisDesignation, 0, TopographyProcessType.ForwardOnly);
            IntensityEvaluator eval = new IntensityEvaluator(luminanceField);
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

            // wraps profiles extracted from NMM files to a more abstract structure: profiles
            List<IntensityProfile> profilesList = new List<IntensityProfile>();
            WarpNmmProfiles(options, TopographyProcessType.ForwardOnly, profilesList);
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.ForwardAndBackward || theData.MetaData.ScanStatus == ScanDirectionStatus.ForwardAndBackwardJustified)
            {
                WarpNmmProfiles(options, TopographyProcessType.BackwardOnly, profilesList);
            }
            IntensityProfile[] profiles = profilesList.ToArray();

            // the loop over all profiles
            for (int profileIndex = 0; profileIndex < profiles.Length; profileIndex++)
            {
                if (!profiles[profileIndex].IsValid)
                {
                    ConsoleUI.ErrorExit($"!Profile {profileIndex} invalid", 6);
                }
                Classifier classifier = new Classifier(profiles[profileIndex].Zvalues);
                int[] skeleton = classifier.GetSegmentedProfile(options.Threshold, eval.LowerBound, eval.UpperBound);
                MorphoFilter filter = new MorphoFilter(skeleton);
                skeleton = filter.FilterWithParameter(options.Morpho);
                LineDetector marks = new LineDetector(skeleton, profiles[profileIndex].Xvalues);
                if (options.LineScale)
                {
                    ConsoleUI.WriteLine($"profile: {profileIndex + 1,3} with {marks.LineCount} line marks {(marks.LineCount != options.ExpectedTargets ? "*" : " ")}");
                }
                result.UpdateSample(marks.LineMarks, options.RefLine);
                if (options.EdgeOnly)
                {
                    ConsoleUI.WriteLine($"profile: {profileIndex + 1,3} with L:{marks.LeftEdgePositions.Count} R:{marks.RightEdgePositions.Count}");
                    EdgesOnlyOutputToStringBuilder(options, marks, profileIndex);
                }
            }

            #region output
            // prepare output
            string outFormater = $"F{options.Precision}";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{HeadingInfo.Default} {CopyrightInfo.Default}");
            sb.AppendLine($"InputFile            = {theData.MetaData.BaseFileName}");
            // section for sample specific metadata
            sb.AppendLine($"SampleIdentifier     = {theData.MetaData.SampleIdentifier}");
            sb.AppendLine($"SampleSpecies        = {theData.MetaData.SampleSpecies}");
            sb.AppendLine($"SampleSpecification  = {theData.MetaData.SampleSpecification}");
            if (options.LineScale)
            {
                sb.AppendLine($"ExpectedLineMarks    = {options.ExpectedTargets}");
                sb.AppendLine($"NominalDivision      = {options.NominalDivision} µm");
                sb.AppendLine($"ScaleType            = {result.ScaleType}");
            }
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
            sb.AppendLine($"Threshold            = {options.Threshold}");
            sb.AppendLine($"FilterParameter      = {options.Morpho}");
            if (options.LineScale)
            {
                sb.AppendLine($"ReferencedToLine     = {options.RefLine}");
                double maximumThermalCorrection = ThermalCorrection(result.LineMarks.Last().NominalPosition, options.Alpha) - ThermalCorrection(result.LineMarks.First().NominalPosition, options.Alpha);
                sb.AppendLine($"MaxThermalCorrection = {maximumThermalCorrection:F3} µm");
            }
            // auxiliary values 
            sb.AppendLine($"MinimumIntensity     = {eval.MinIntensity}");
            sb.AppendLine($"MaximumIntensity     = {eval.MaxIntensity}");
            sb.AppendLine($"LowerPlateau         = {eval.LowerBound}");
            sb.AppendLine($"UpperPlateau         = {eval.UpperBound}");
            sb.AppendLine($"RelativeSpan         = {relativeSpan:F1} %");
            if (options.LineScale)
            {
                sb.AppendLine($"EvaluatedProfiles    = {result.SampleSize}");
            }
            // environmental data
            sb.AppendLine($"SampleTemperature    = {theData.MetaData.SampleTemperature.ToString("F3")} °C");
            sb.AppendLine($"AirTemperature       = {theData.MetaData.AirTemperature.ToString("F3")} °C");
            sb.AppendLine($"AirPressure          = {theData.MetaData.BarometricPressure.ToString("F0")} Pa");
            sb.AppendLine($"AirHumidity          = {theData.MetaData.RelativeHumidity.ToString("F1")} %");
            sb.AppendLine("======================");
            if (options.LineScale)
            {
                sb.AppendLine("1 : Line number (tag)");
                sb.AppendLine("2 : Nominal value / µm");
                sb.AppendLine("3 : Position deviation / µm");
                sb.AppendLine("4 : StdDev of line position values / µm");
                sb.AppendLine("5 : Range of line position values / µm");
                sb.AppendLine("6 : Line width / µm");
                sb.AppendLine("7 : StdDev of line widths / µm");
                sb.AppendLine("8 : Range of line widths / µm");
                sb.AppendLine("@@@@");
                if (result.SampleSize == 0)
                {
                    sb.AppendLine("*** No matching intensity pattern found ***");
                }
                else
                {
                    foreach (var line in result.LineMarks)
                    {
                        double deltaL = ThermalCorrection(line.NominalPosition, options.Alpha);
                        sb.AppendLine($"{line.Tag.ToString().PadLeft(5)}" +
                            $"{line.NominalPosition.ToString("F0").PadLeft(10)}" +
                            $"{(line.Deviation + deltaL).ToString(outFormater).PadLeft(10)}" +
                            $"{line.LineCenterStdDev.ToString(outFormater).PadLeft(10)}" +
                            $"{line.LineCenterRange.ToString(outFormater).PadLeft(10)}" +
                            $"{line.AverageLineWidth.ToString(outFormater).PadLeft(10)}" +
                            $"{line.LineWidthStdDev.ToString(outFormater).PadLeft(10)}" +
                            $"{(line.LineWidthRange).ToString(outFormater).PadLeft(10)}");
                    }
                }
            }
            if (options.EdgeOnly)
            {
                sb.Append(edgesOnlyOutput);
            }
            #endregion

            #region File output
            string outFileName;
            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                outFileName = nmmFileNameObject.GetFreeFileNameWithIndex(".prn");
            }
            else
            { 
                outFileName = options.OutputPath; 
            }
            ConsoleUI.WriteLine();
            ConsoleUI.WritingFile(outFileName);
            StreamWriter hOutFile = File.CreateText(outFileName);
            hOutFile.Write(sb);
            hOutFile.Close();
            ConsoleUI.Done();
            #endregion
        }

        private static void WarpNmmProfiles(Options options, TopographyProcessType processType, List<IntensityProfile> tempList)
        {
            for (int profileIndex = 0; profileIndex < theData.MetaData.NumberOfProfiles; profileIndex++)
            {
                double[] xData = theData.ExtractProfile(options.XAxisDesignation, profileIndex + 1, processType);
                double[] zData = theData.ExtractProfile(options.ZAxisDesignation, profileIndex + 1, processType);
                // convert Xdata from meter to micrometer
                for (int i = 0; i < xData.Length; i++)
                    xData[i] = xData[i] * 1.0e6;
                tempList.Add(new IntensityProfile(xData, zData));
            }
        }

        private static void EdgesOnlyOutputToStringBuilder(Options options, LineDetector marks, int profileIndex)
        {
            List<double> leftEdges = marks.LeftEdgePositions;
            List<double> rightEdges = marks.RightEdgePositions;
            leftEdges.Sort();
            rightEdges.Sort();
            edgesOnlyOutput.AppendLine($"Profile {profileIndex}");
            edgesOnlyOutput.AppendLine($"   Right edges ({rightEdges.Count}), position in µm");
            foreach (var edge in rightEdges)
            {
                double deltaL = ThermalCorrection(edge, options.Alpha);
                edgesOnlyOutput.AppendLine($"      {edge + deltaL:F3}");
            }
            edgesOnlyOutput.AppendLine($"   Left edges ({leftEdges.Count}), position in µm");
            foreach (var edge in leftEdges)
            {
                double deltaL = ThermalCorrection(edge, options.Alpha);
                edgesOnlyOutput.AppendLine($"      {edge + deltaL:F3}");
            }
        }

        // gives the thermal correction value for a given length (both in the same unit)
        // return value must be added to the given length to obtain the length at reference temperature
        private static double ThermalCorrection(double length, double alpha)
        {
            double referenceTemperature = 20;
            double deltaT = theData.MetaData.SampleTemperature - referenceTemperature;
            double deltaL = alpha * length * deltaT;
            return -deltaL;
        }

    }

}
