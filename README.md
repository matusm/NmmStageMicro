NmmStageMicro
=============

A standalone command line tool that evaluates files produced by the [SIOS](https://sios-de.com) NMM-1 for calibrating stage micrometers using the laser focus probe. The results are written to an ASCII text file.

## Functionality
Text to be inserted

### Valid Input 
During a standard scan procedure of the NMM-1 eight files are produced. However, depending on set parameters this number can change from four to virtually any number (for a multi-scan). Depending on the software version these files can even come in different syntacitc flavours. The library [Bev.IO.NmmReader](https://github.com/matusm/Bev.IO.NmmReader) takes care of all the parsing, invalid data files just produce no output. 

### Result File Format
The output is written in a plain text file. 

### Line Detection Algorithm
Text to be inserted

## Command Line Usage:

```
NmmStageMicro inputfile [outputfile] [options]
```

### Options:  

`--X-axis (-X)` : Channel for the x-axis. The default is "XYvec".

`--Z-axis (-Z)` : Channel for z-axis (brightness) The default is "AX".

`--scan (-s)` : Scan index for multi-scan files.

`--precision` : Decimal digits for the results given in µm. The default is 3, meaning a resolution of nm. 

`--div (-d)` : Nominal scale division in µm. Used for the determination of deviations from nominal values.

`--nlines (-n)` : Expected number of line marks.

`--alpha` : Thermal expansion coefficient of the scale material in 1/K. The default is 0, which effectively does not correct for thermal expansion.

`--reference (-r)` : Defines the zero line mark.

`--morpho (-m)` : Kernel value for the morphological filter. Positve values causes erosion followed by dilatation. Negative values causes dilatation followed by erosion.

`--threshold (-t)` : Threshold value for edge detection. Default value is 0.5.

`--edge (-e)` : Extract edge positions only. Data is not evaluated as a line measure.

`--quiet (-q)` : Quiet mode. No screen output (except for errors).

`--help` : Display a help screen.

## Dependencies  

Bev.IO.NmmReader:  https://github.com/matusm/Bev.IO.NmmReader  

At.Matus.StatisticPod: https://github.com/matusm/At.Matus.StatisticPod

CommandLineParser: https://github.com/commandlineparser/commandline 

