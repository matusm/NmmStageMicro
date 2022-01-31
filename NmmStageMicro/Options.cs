using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace NmmStageMicro
{
    class Options
    {
        [Option('s', "scan", DefaultValue = 0, HelpText = "Scan index for multi-scan files.")]
        public int ScanIndex { get; set; }

        [Option('X', "X-axis", DefaultValue = "XYvec", HelpText = "Channel for x-axis.")]
        public string XAxisDesignation { get; set; }

        [Option('Z', "Z-axis", DefaultValue = "AX", HelpText = "Channel for z-axis (brightness).")]
        public string ZAxisDesignation { get; set; }

        [Option("precision", DefaultValue = 3, HelpText = "Decimal digits for results.")]
        public int Precision { get; set; }

        [Option('d', "div", DefaultValue = 0, HelpText = "Nominal scale division in um.")]
        public double NominalDivision { get; set; }

        [Option('n', "nlines", DefaultValue = 0, HelpText = "Expected number of line marks.")]
        public int ExpectedTargets { get; set; }

        [Option('r', "reference", DefaultValue = 0, HelpText = "Line mark # to reference to.")]
        public int RefLine { get; set; }

        [Option("alpha", DefaultValue = 0.0, HelpText = "Thermal expansion coefficient in 1/K.")]
        public double Alpha { get; set; }

        [Option('m', "morpho", DefaultValue = 25, HelpText = "Kernel value of morphological filter.")]
        public int Morpho { get; set; }

        [Option('t', "threshold", DefaultValue = 0.5, HelpText = "Threshold value for line detection.")]
        public double Threshold { get; set; }

        [Option('q', "quiet", HelpText = "Quiet mode. No screen output (except for errors).")]
        public bool BeQuiet { get; set; }

        [Option('e', "edge", HelpText = "Extract edges only.")]
        public bool EdgeOnly { get; set; }

        public bool LineScale => !EdgeOnly;

        [ValueList(typeof(List<string>), MaximumElements = 2)]
        public IList<string> ListOfFileNames { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            string AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string AppVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            HelpText help = new HelpText
            {
                Heading = new HeadingInfo(AppName, "version " + AppVer),
                Copyright = new CopyrightInfo("Michael Matus", 2015),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };
            string sPre = "Program to evaluate scanning files by SIOS NMM-1 for calibrating stage micrometers using the laser focus probe. " +
                "For multiple profiles the line marks are detected separatly and average position are calculated. " +
                "The number of line marks must be provided (via -n option). The nominal scale divison (-d) is needed for evaluation of deviations. " +
                " ";
            help.AddPreOptionsLine(sPre);
            help.AddPreOptionsLine("");
            help.AddPreOptionsLine("Usage: " + AppName + " filename1 [filename2] [options]");
            // help.AddPostOptionsLine("Supported values for --filetype: 1=*.txt, 2=*.sig, 3=*.prf, 4=*.pr, 5=*.sdf, 6=*.smd");
            help.AddOptions(this);
            return help;
        }
    }
}
