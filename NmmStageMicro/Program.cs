using At.Matus.IO.NmmReader;
using At.Matus.IO.NmmReader.scan_mode;
using At.Matus.IO.NmmReader.Tools;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NmmStageMicro
{
    public partial class MainClass
    {
        private const ReferenceTo defaultReference = ReferenceTo.LinePositive;
        private static Options options = new Options(); // this must be set in Run()
        private static NmmScanData nmmScanData; // the complete data of the scan
        private static NmmFileName nmmFileName;

        private static void Run(Options ops)
        {
            options = ops;
            DisplayWelcomeAndSetVerbosity();
            LoadScanData();
            CheckScanData();
            AdjustReference();
            DisplaySummary(); // TODO rename

            // evaluate the intensities for ALL profiles == the whole scan field            
            // we use the forward only data for this, because it is always present
            ConsoleUI.StartOperation("Classifying intensity data");
            double[] luminanceField = nmmScanData.ExtractProfile(options.ZAxisDesignation, 0, TopographyProcessType.ForwardOnly);
            
            // level topography data to reduce the influence of intensity variations across the scan field
            DataLeveling levelObject = new DataLeveling(luminanceField, nmmScanData.MetaData.NumberOfDataPoints, nmmScanData.MetaData.NumberOfProfiles);
            double[] leveledLuminanceField = levelObject.LevelData(defaultReference);

            // stretch the data to assure that the intensity values are in a range that allows a good classification of the line marks
            // some scans have very low values, which makes it difficult to find a good threshold for the line mark classification)
            leveledLuminanceField = StretchZValues(leveledLuminanceField);

            IntensityEvaluator evaluator = new IntensityEvaluator(leveledLuminanceField);
            ConsoleUI.Done();
            ConsoleUI.WriteLine($"Intensity range from {evaluator.MinIntensity} to {evaluator.MaxIntensity}");
            ConsoleUI.WriteLine($"Estimated bounds from {evaluator.LowerBound} to {evaluator.UpperBound}");
            double relativeSpan = (double)(evaluator.UpperBound - evaluator.LowerBound) / (double)(evaluator.MaxIntensity - evaluator.MinIntensity) * 100.0;
            ConsoleUI.WriteLine($"({relativeSpan:F0} % of full range)");
            ConsoleUI.WriteLine();

            // prepare object for the overall dimensional result
            LineScale result = new LineScale(options.ExpectedTargets);
            result.SetNominalValues(options.NominalDivision, options.RefLine);

            // wraps profiles extracted from NMM files to a more abstract structure: profiles
            List<IntensityProfile> profilesList = new List<IntensityProfile>();
            WarpNmmProfiles(TopographyProcessType.ForwardOnly, profilesList);
            if (nmmScanData.MetaData.ScanStatus == ScanDirectionStatus.ForwardAndBackward || nmmScanData.MetaData.ScanStatus == ScanDirectionStatus.ForwardAndBackwardJustified)
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
                int[] skeleton = classifier.GetSegmentedProfile(options.Threshold, evaluator.LowerBound, evaluator.UpperBound);
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

            string report = PrepareReport(result, evaluator, edgeReport);
            WriteReportToFile(report);
        }

        private static void LoadScanData()
        {
            ConsoleUI.StartOperation("Reading NMM scan files");
            nmmFileName = new NmmFileName(options.InputPath);               
            nmmFileName.SetScanIndex(options.ScanIndex);
            try
            {
                nmmScanData = new NmmScanData(nmmFileName);
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
            if (nmmScanData.MetaData.ScanStatus == ScanDirectionStatus.Unknown)
                ConsoleUI.ErrorExit("!Unknown scan type", 4);
            if (nmmScanData.MetaData.ScanStatus == ScanDirectionStatus.NoData)
                ConsoleUI.ErrorExit("!No scan data present", 5);
            if (!nmmScanData.ColumnPresent(options.XAxisDesignation))
                ConsoleUI.ErrorExit($"!Requested channel {options.XAxisDesignation} not in data files", 2);
            if (!nmmScanData.ColumnPresent(options.ZAxisDesignation))
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
            ConsoleUI.WriteLine($"{nmmScanData.MetaData.NumberOfDataPoints} data lines with {nmmScanData.MetaData.NumberOfColumnsInFile} channels, organized in {nmmScanData.MetaData.NumberOfProfiles} profiles");
            ConsoleUI.WriteLine($"SpuriousDataLines: {nmmScanData.MetaData.SpuriousDataLines}");
            ConsoleUI.WriteLine($"NumberOfGlitchedDataPoints: {nmmScanData.MetaData.NumberOfGlitchedDataPoints}"); ConsoleUI.WriteLine($"x-axis channel: {options.XAxisDesignation}");
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
            for (int profileIndex = 0; profileIndex < nmmScanData.MetaData.NumberOfProfiles; profileIndex++)
            {
                double[] xData = nmmScanData.ExtractProfile(options.XAxisDesignation, profileIndex + 1, processType);
                double[] zData = nmmScanData.ExtractProfile(options.ZAxisDesignation, profileIndex + 1, processType);
                // convert Xdata from meter to micrometer
                for (int i = 0; i < xData.Length; i++)
                    xData[i] = xData[i] * 1.0e6;
                // TODO : level profile
                zData = StretchZValues(zData);
                tempList.Add(new IntensityProfile(xData, zData));
            }
        }

        private static double ThermalCorrection(double length, double alpha)
        {
            // gives the thermal correction value for a given length (both in the same unit)
            // return value must be added to the given length to obtain the length at reference temperature
            double referenceTemperature = 20;
            double deltaT = nmmScanData.MetaData.SampleTemperature - referenceTemperature;
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
            sb.AppendLine($"InputFile            = {nmmScanData.MetaData.BaseFileName}");
            sb.AppendLine($"SampleIdentifier     = {nmmScanData.MetaData.SampleIdentifier}");
            sb.AppendLine($"SampleSpecies        = {nmmScanData.MetaData.SampleSpecies}");
            sb.AppendLine($"SampleSpecification  = {nmmScanData.MetaData.SampleSpecification}");
            sb.AppendLine($"MeasurementDate      = {nmmScanData.MetaData.CreationDate.ToString("dd-MM-yyyy")}");
            sb.AppendLine("======================");
            for (int i = 3; i < nmmScanData.MetaData.ScanComments.Count; i++)
            {
                sb.AppendLine($"ScanComment{i + 1:D2}        = {nmmScanData.MetaData.ScanComments[i]}");
            }
            sb.AppendLine("======================");
            if (options.LineScale)
            {
                sb.AppendLine($"ExpectedLineMarks    = {options.ExpectedTargets}");
                sb.AppendLine($"NominalDivision      = {options.NominalDivision} µm");
                sb.AppendLine($"ScaleType            = {result.ScaleType}");
            }
            sb.AppendLine($"ThermalExpansion     = {options.Alpha.ToString("E2")} 1/K");
            sb.AppendLine($"NumberOfScans        = {nmmScanData.MetaData.NumberOfScans}");
            sb.AppendLine($"ScanIndex            = {nmmScanData.MetaData.ScanIndex}");
            sb.AppendLine($"PointsPerProfile     = {nmmScanData.MetaData.NumberOfDataPoints}");
            sb.AppendLine($"Profiles             = {nmmScanData.MetaData.NumberOfProfiles}");
            sb.AppendLine($"InputChannels        = {nmmScanData.MetaData.NumberOfColumnsInFile}");
            sb.AppendLine($"PointSpacing         = {(nmmScanData.MetaData.ScanFieldDeltaX * 1e6).ToString("F4")} µm");
            sb.AppendLine($"ProfileSpacing       = {(nmmScanData.MetaData.ScanFieldDeltaY * 1e6).ToString("F4")} µm");
            sb.AppendLine($"ScanFieldCenterX     = {nmmScanData.MetaData.ScanFieldCenterX * 1000:F1} mm");
            sb.AppendLine($"ScanFieldCenterY     = {nmmScanData.MetaData.ScanFieldCenterY * 1000:F1} mm");
            sb.AppendLine($"ScanFieldCenterZ     = {nmmScanData.MetaData.ScanFieldCenterZ * 1000:F1} mm");
            sb.AppendLine($"AngularOrientation   = {nmmScanData.MetaData.ScanFieldRotation:F2}°");
            sb.AppendLine($"ScanSpeed            = {nmmScanData.MetaData.ScanSpeed} m/s");
            sb.AppendLine($"ScanDuration         = {nmmScanData.MetaData.ScanDuration} s");
            sb.AppendLine($"GlitchedDataPoints   = {nmmScanData.MetaData.NumberOfGlitchedDataPoints}");
            sb.AppendLine($"SpuriousDataLines    = {nmmScanData.MetaData.SpuriousDataLines}");
            sb.AppendLine($"Probe                = {nmmScanData.MetaData.ProbeDesignation}");
            // evaluation parameters, user supplied
            sb.AppendLine($"X-AxisChannel        = {options.XAxisDesignation}");
            sb.AppendLine($"Z-AxisChannel        = {options.ZAxisDesignation}");
            sb.AppendLine($"Threshold            = {options.Threshold}");
            sb.AppendLine($"FilterParameter      = {options.Morpho}");
            if (options.LineScale)
            {
                sb.AppendLine($"ReferencedToLine     = {options.RefLine}");
                double maximumThermalCorrection = ThermalCorrection(result.LineMarks.Last().AverageLineCenter, options.Alpha) - ThermalCorrection(result.LineMarks.First().AverageLineCenter, options.Alpha);
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
            sb.AppendLine($"SampleTemperature    = {nmmScanData.MetaData.SampleTemperature.ToString("F3")} °C");
            sb.AppendLine($"AirTemperature       = {nmmScanData.MetaData.AirTemperature.ToString("F3")} °C");
            sb.AppendLine($"AirPressure          = {nmmScanData.MetaData.BarometricPressure.ToString("F0")} Pa");
            sb.AppendLine($"AirHumidity          = {nmmScanData.MetaData.RelativeHumidity.ToString("F1")} %");
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
                        double deltaL = ThermalCorrection(line.AverageLineCenter, options.Alpha) - ThermalCorrection(result.LineMarks.First().AverageLineCenter, options.Alpha);
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
                outFileName = nmmFileName.GetFreeFileNameWithIndex(".prn");
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

        // stretches the Z values to a range that allows a good classification of the line marks (0 - 10000)
        private static double[] StretchZValues(double[] field)
        {
            double maxValue = field.Max();
            double minValue = field.Min();
            double span = maxValue - minValue;
            return field.Select(x => 10000 * (x - minValue)/span).ToArray();
        }

    }

}
