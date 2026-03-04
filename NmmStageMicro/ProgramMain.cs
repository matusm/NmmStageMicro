using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NmmStageMicro
{
    public partial class MainClass
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


    }
}
