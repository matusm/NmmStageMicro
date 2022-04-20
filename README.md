NmmStageMicro
=============

A standalone command line tool that evaluates files produced by the [SIOS](https://sios-de.com) NMM-1 for calibrating stage micrometers using the laser focus probe. The results are written to an ASCII text file.

## Command Line Usage:

```
NmmStageMicro inputfile [outputfile] [options]
```

## Options:  

`--X-axis (-X)` : Channel for the x-axis. The default is "XYvec".

`--Z-axis (-Z)` : Channel for z-axis (brightness) The default is "AX".

`--scan (-s)` : Scan index for multi-scan files.

`--precision` : Decimal digits for the result when given in µm. The default is 3, meaning a resolution of nm. 

`--div (-d)` : Nominal scale division in µm. Used for the determination of deviations from nominal values.

`--nlines (-n)` : Expected number of line marks.

`--reference (-r)` : The number of the line mark which is the zero mark.

`--alpha` : Thermal expansion coefficient of the scale material in 1/K. The default is 0, which effectively does not correct for thermal expansion.

`--reference (-r)` : The number of the line mark which is the zero mark.

`--morpho (-m)` : Kernel value for the morphological filter. Positve values causes erosion followed by dilatation. Negative values causes dilatation followed by erosion.

`--threshold (-t)` : Threshold value for edge detection. Default value is 0.5.

`--edge (-e)` : Extract edge positions only. Files are not evaluated as a line measure.

`--quiet (-q)` : Quiet mode. No screen output (except for errors).

`--help` : Display a help screen.

## Dependencies  

Bev.IO.NmmReader:  https://github.com/matusm/Bev.IO.NmmReader  

At.Matus.StatisticPod: https://github.com/matusm/At.Matus.StatisticPod

CommandLineParser: https://github.com/commandlineparser/commandline 

