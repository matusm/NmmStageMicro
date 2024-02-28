using CommandLine;

namespace NmmStageMicro
{
    class Options
    {
        [Option('s', "scan", Default = 0, HelpText = "Scan index for multi-scan files.")]
        public int ScanIndex { get; set; }

        [Option('X', "X-axis", Default = "XYvec", HelpText = "Channel for x-axis.")]
        public string XAxisDesignation { get; set; }

        [Option('Z', "Z-axis", Default = "AX", HelpText = "Channel for z-axis (brightness).")]
        public string ZAxisDesignation { get; set; }

        [Option("precision", Default = 3, HelpText = "Decimal digits for results.")]
        public int Precision { get; set; }

        [Option('d', "div", Default = 0, HelpText = "Nominal scale division in um.")]
        public double NominalDivision { get; set; }

        [Option('n', "nlines", Default = 0, HelpText = "Expected number of line marks.")]
        public int ExpectedTargets { get; set; }

        [Option('r', "reference", Default = 0, HelpText = "Line mark # to reference to.")]
        public int RefLine { get; set; }

        [Option("alpha", Default = 0.0, HelpText = "Thermal expansion coefficient in 1/K.")]
        public double Alpha { get; set; }

        [Option('m', "morpho", Default = 0, HelpText = "Kernel value of morphological filter.")]
        public int Morpho { get; set; }

        [Option('t', "threshold", Default = 0.5, HelpText = "Threshold value for line detection.")]
        public double Threshold { get; set; }

        [Option('q', "quiet", HelpText = "Quiet mode. No screen output (except for errors).")]
        public bool BeQuiet { get; set; }

        [Option('e', "edge", HelpText = "Extract edges only.")]
        public bool EdgeOnly { get; set; }

        public bool LineScale => !EdgeOnly;

        [Value(0, MetaName = "InputPath", Required = true, HelpText = "Input file-name including path")]
        public string InputPath { get; set; }

        [Value(1, MetaName = "OutputPath", HelpText = "Output file-name including path")]
        public string OutputPath { get; set; }

    }
}
