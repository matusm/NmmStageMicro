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
            ConsoleUI.WriteLine();

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
                ConsoleUI.ErrorExit("!Unknown scan type", 2);
            if (theData.MetaData.ScanStatus == ScanDirectionStatus.NoData)
                ConsoleUI.ErrorExit("!No scan data present", 3);





        }
    }
}
