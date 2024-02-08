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
        private static Options options = new Options(); // this must be set in Run()
        private static NmmScanData theNmmData; // the complete data of the scan
        private static NmmFileName theNmmFileName;

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
            HelpText helpText = HelpText.AutoBuild(result, h =>
            {
                h.AutoVersion = false;
                h.AdditionalNewLineAfterOption = false;
                h.AddPreOptionsLine("\nProgram to evaluate scanning files by SIOS NMM-1 for calibrating stage micrometers using the laser focus probe. For multiple profiles the line marks are detected separatly and average position are calculated. The number of line marks must be provided (via -n option). The nominal scale divison (-d) is needed for evaluation of deviations.");
                h.AddPreOptionsLine("");
                h.AddPreOptionsLine($"Usage: {appName} InputPath [OutPath] [options]");
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }

        private static void DisplayWelcomeAndSetVerbosity()
        {
            if (options.BeQuiet == true)
                ConsoleUI.BeSilent();
            else
                ConsoleUI.BeVerbatim();
            ConsoleUI.WriteLine(HeadingInfo.Default);
            ConsoleUI.WriteLine(CopyrightInfo.Default);
            ConsoleUI.WriteLine();
        }

        private static double[] NormalizeField(double[] field)
        {
            double maxValue = field.Max();
            double minValue = field.Min();
            if (maxValue < 1)
                return field.Select(x => x * 1e9).ToArray();
            return field;
        }

        private static void Run(Options ops)
        {
            options = ops;
            DisplayWelcomeAndSetVerbosity();
            LoadScanData();
            CheckScanData();
            AdjustReference();
            DisplaySummary(); // TODO rename

            // evaluate the intensities for ALL profiles == the whole scan field
            ConsoleUI.StartOperation("Classifying intensity data");
            double[] luminanceField = theNmmData.ExtractProfile(options.ZAxisDesignation, 0, TopographyProcessType.ForwardOnly);
            luminanceField = NormalizeField(luminanceField);
            IntensityEvaluator eval = new IntensityEvaluator(luminanceField);
            ConsoleUI.Done();
            ConsoleUI.WriteLine($"Intensity range from {eval.MinIntensity} to {eval.MaxIntensity}");
            ConsoleUI.WriteLine($"Estimated bounds from {eval.LowerBound} to {eval.UpperBound}");
            double relativeSpan = (double)(eval.UpperBound - eval.LowerBound) / (double)(eval.MaxIntensity - eval.MinIntensity) * 100.0;
            ConsoleUI.WriteLine($"({relativeSpan:F0} % of full range)");
            ConsoleUI.WriteLine();

            // prepare object for the overall dimensional result
            LineScale result = new LineScale(options.ExpectedTargets);
            result.SetNominalValues(options.NominalDivision, options.RefLine);

            // wraps profiles extracted from NMM files to a more abstract structure: profiles
            List<IntensityProfile> profilesList = new List<IntensityProfile>();
            WarpNmmProfiles(TopographyProcessType.ForwardOnly, profilesList);
            if (theNmmData.MetaData.ScanStatus == ScanDirectionStatus.ForwardAndBackward || theNmmData.MetaData.ScanStatus == ScanDirectionStatus.ForwardAndBackwardJustified)
            {
                WarpNmmProfiles(TopographyProcessType.BackwardOnly, profilesList);
            }
            IntensityProfile[] profiles = profilesList.ToArray();

            // the loop over all profiles
            StringBuilder edgeReport = new StringBuilder();
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
                    edgeReport.AppendLine(EdgesOnlyOutput(marks, profileIndex));
                }
            }

            string report = PrepareReport(result, eval, edgeReport);
            WriteReportToFile(report);
        }

        private static void LoadScanData()
        {
            ConsoleUI.StartOperation("Reading NMM scan files");
            theNmmFileName = new NmmFileName(options.InputPath);               
            theNmmFileName.SetScanIndex(options.ScanIndex);
            try
            {
                theNmmData = new NmmScanData(theNmmFileName);
            }
            catch (Exception)
            {
                ConsoleUI.Abort();
                ConsoleUI.ErrorExit("!Problem loading data (invalid scan index?)", 1);
            }
            ConsoleUI.Done();
        }

        private static void CheckScanData()
        {
            if (theNmmData.MetaData.ScanStatus == ScanDirectionStatus.Unknown)
                ConsoleUI.ErrorExit("!Unknown scan type", 4);
            if (theNmmData.MetaData.ScanStatus == ScanDirectionStatus.NoData)
                ConsoleUI.ErrorExit("!No scan data present", 5);
            if (!theNmmData.ColumnPresent(options.XAxisDesignation))
                ConsoleUI.ErrorExit($"!Requested channel {options.XAxisDesignation} not in data files", 2);
            if (!theNmmData.ColumnPresent(options.ZAxisDesignation))
                ConsoleUI.ErrorExit($"!Requested channel {options.ZAxisDesignation} not in data files", 3);
        }

        private static void AdjustReference()
        {
            if (options.RefLine < 0) options.RefLine = 0;
            if (options.RefLine >= options.ExpectedTargets) options.RefLine = options.ExpectedTargets - 1;
        }
        
        private static void DisplaySummary()
        {
            ConsoleUI.WriteLine();
            ConsoleUI.WriteLine($"{theNmmData.MetaData.NumberOfDataPoints} data lines with {theNmmData.MetaData.NumberOfColumnsInFile} channels, organized in {theNmmData.MetaData.NumberOfProfiles} profiles");
            ConsoleUI.WriteLine($"SpuriousDataLines: {theNmmData.MetaData.SpuriousDataLines}");
            ConsoleUI.WriteLine($"NumberOfGlitchedDataPoints: {theNmmData.MetaData.NumberOfGlitchedDataPoints}"); ConsoleUI.WriteLine($"x-axis channel: {options.XAxisDesignation}");
            ConsoleUI.WriteLine($"z-axis channel: {options.ZAxisDesignation}");
            ConsoleUI.WriteLine($"Threshold: {options.Threshold}");
            ConsoleUI.WriteLine($"Morphological filter parameter: {options.Morpho}");
            if (options.LineScale)
            {
                ConsoleUI.WriteLine($"Expected number of line marks: {options.ExpectedTargets}");
                ConsoleUI.WriteLine($"Nominal scale division: {options.NominalDivision} um");
            }
            ConsoleUI.WriteLine();
        }

        private static void WarpNmmProfiles(TopographyProcessType processType, List<IntensityProfile> tempList)
        {
            for (int profileIndex = 0; profileIndex < theNmmData.MetaData.NumberOfProfiles; profileIndex++)
            {
                double[] xData = theNmmData.ExtractProfile(options.XAxisDesignation, profileIndex + 1, processType);
                double[] zData = theNmmData.ExtractProfile(options.ZAxisDesignation, profileIndex + 1, processType);
                // convert Xdata from meter to micrometer
                for (int i = 0; i < xData.Length; i++)
                    xData[i] = xData[i] * 1.0e6;
                zData = NormalizeField(zData);
                tempList.Add(new IntensityProfile(xData, zData));
            }
        }

        private static double ThermalCorrection(double length, double alpha)
        {
            // gives the thermal correction value for a given length (both in the same unit)
            // return value must be added to the given length to obtain the length at reference temperature
            double referenceTemperature = 20;
            double deltaT = theNmmData.MetaData.SampleTemperature - referenceTemperature;
            double deltaL = alpha * length * deltaT;
            return -deltaL;
        }

        private static string EdgesOnlyOutput(LineDetector marks, int profileIndex)
        {
            StringBuilder sb = new StringBuilder();
            List<double> leftEdges = marks.LeftEdgePositions;
            List<double> rightEdges = marks.RightEdgePositions;
            leftEdges.Sort();
            rightEdges.Sort();
            sb.AppendLine($"Profile {profileIndex}");
            sb.AppendLine($"   Right edges ({rightEdges.Count}), position in µm");
            foreach (var edge in rightEdges)
            {
                double deltaL = ThermalCorrection(edge, options.Alpha);
                sb.AppendLine($"      {edge + deltaL:F3}");
            }
            sb.AppendLine($"   Left edges ({leftEdges.Count}), position in µm");
            foreach (var edge in leftEdges)
            {
                double deltaL = ThermalCorrection(edge, options.Alpha);
                sb.AppendLine($"      {edge + deltaL:F3}");
            }
            return sb.ToString();
        }

        private static string PrepareReport(LineScale result, IntensityEvaluator eval, StringBuilder edgeReport)
        {
            string outFormater = $"F{options.Precision}";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{HeadingInfo.Default} {CopyrightInfo.Default}");
            sb.AppendLine($"InputFile            = {theNmmData.MetaData.BaseFileName}");
            sb.AppendLine($"SampleIdentifier     = {theNmmData.MetaData.SampleIdentifier}");
            sb.AppendLine($"SampleSpecies        = {theNmmData.MetaData.SampleSpecies}");
            sb.AppendLine($"SampleSpecification  = {theNmmData.MetaData.SampleSpecification}");
            sb.AppendLine($"MeasurementDate      = {theNmmData.MetaData.CreationDate.ToString("dd-MM-yyyy")}");
            sb.AppendLine("======================");
            for (int i = 3; i < theNmmData.MetaData.ScanComments.Count; i++)
            {
                sb.AppendLine($"ScanComment{i + 1:D2}        = {theNmmData.MetaData.ScanComments[i]}");
            }
            sb.AppendLine("======================");
            if (options.LineScale)
            {
                sb.AppendLine($"ExpectedLineMarks    = {options.ExpectedTargets}");
                sb.AppendLine($"NominalDivision      = {options.NominalDivision} µm");
                sb.AppendLine($"ScaleType            = {result.ScaleType}");
            }
            sb.AppendLine($"ThermalExpansion     = {options.Alpha.ToString("E2")} 1/K");
            sb.AppendLine($"NumberOfScans        = {theNmmData.MetaData.NumberOfScans}");
            sb.AppendLine($"ScanIndex            = {theNmmData.MetaData.ScanIndex}");
            sb.AppendLine($"PointsPerProfile     = {theNmmData.MetaData.NumberOfDataPoints}");
            sb.AppendLine($"Profiles             = {theNmmData.MetaData.NumberOfProfiles}");
            sb.AppendLine($"InputChannels        = {theNmmData.MetaData.NumberOfColumnsInFile}");
            sb.AppendLine($"PointSpacing         = {(theNmmData.MetaData.ScanFieldDeltaX * 1e6).ToString("F4")} µm");
            sb.AppendLine($"ProfileSpacing       = {(theNmmData.MetaData.ScanFieldDeltaY * 1e6).ToString("F4")} µm");
            sb.AppendLine($"ScanFieldCenterX     = {theNmmData.MetaData.ScanFieldCenterX * 1000:F1} mm");
            sb.AppendLine($"ScanFieldCenterY     = {theNmmData.MetaData.ScanFieldCenterY * 1000:F1} mm");
            sb.AppendLine($"ScanFieldCenterZ     = {theNmmData.MetaData.ScanFieldCenterZ * 1000:F1} mm");
            sb.AppendLine($"AngularOrientation   = {theNmmData.MetaData.ScanFieldRotation:F2}°");
            sb.AppendLine($"ScanSpeed            = {theNmmData.MetaData.ScanSpeed} m/s");
            sb.AppendLine($"ScanDuration         = {theNmmData.MetaData.ScanDuration} s");
            sb.AppendLine($"GlitchedDataPoints   = {theNmmData.MetaData.NumberOfGlitchedDataPoints}");
            sb.AppendLine($"SpuriousDataLines    = {theNmmData.MetaData.SpuriousDataLines}");
            sb.AppendLine($"Probe                = {theNmmData.MetaData.ProbeDesignation}");
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
            double relativeSpan = (double)(eval.UpperBound - eval.LowerBound) / (double)(eval.MaxIntensity - eval.MinIntensity) * 100.0;
            sb.AppendLine($"RelativeSpan         = {relativeSpan:F1} %");
            if (options.LineScale)
            {
                sb.AppendLine($"EvaluatedProfiles    = {result.SampleSize}");
            }
            // environmental data
            sb.AppendLine($"SampleTemperature    = {theNmmData.MetaData.SampleTemperature.ToString("F3")} °C");
            sb.AppendLine($"AirTemperature       = {theNmmData.MetaData.AirTemperature.ToString("F3")} °C");
            sb.AppendLine($"AirPressure          = {theNmmData.MetaData.BarometricPressure.ToString("F0")} Pa");
            sb.AppendLine($"AirHumidity          = {theNmmData.MetaData.RelativeHumidity.ToString("F1")} %");
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
            sb.Append(edgeReport); // empty for linescales
            return sb.ToString();
        }

        private static void WriteReportToFile(string report)
        {
            string outFileName;
            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                outFileName = theNmmFileName.GetFreeFileNameWithIndex(".prn");
            }
            else
            {
                outFileName = options.OutputPath;
            }
            ConsoleUI.WriteLine();
            ConsoleUI.WritingFile(outFileName);
            StreamWriter hOutFile = File.CreateText(outFileName);
            hOutFile.Write(report);
            hOutFile.Close();
            ConsoleUI.Done();
        }

    }

}
