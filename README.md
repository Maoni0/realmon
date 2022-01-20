# GCRealTimeMon
This is a monitoring tool that tells you when GCs happen in a .NET process and characteristics about these GCs.

Right now it's super simple - given a PID or process name it will show you a few things about GCs as they happen in that process.

## Command Line Arguments

| Command Line Argument | Name | Description |
|-----|-----|-----|
| n | Name of the Process | Grabs the first process with the name matching that with the one specified with this argument |
| p | Process Id | Process Id to monitor GC for |
| c | Path of the Configuration File | Path to the configuration file used by the monitor. This file is a YAML file. By default, it's the Default.yaml file is loaded. If no argument is specified, the command line prompt to create the config will be displayed and the default config will be overwritten with the inputted values; this new config will be used for this session. | 
| g | Path of the New Configuration File | Path to the configuration file that'll be created using a command line prompt, persisted to the given path and used by the monitor for this particular session. This file will be of the YAML format. |
| s | Flag to enable Call Stacks | Passing the 's' command line option will enable displaying call stacks for induced GCs and large object allocations. This mode is currently only available for Windows and only if realmon is run with admin privileges. | 

  ## Runtime Keys

| Key | Action |
|-----|-----|
| `s` | Prints detailed stats of the last collection and the state of each generation |

## Example Usage

```
GCRealTimeMon -p 14028
```

or

```
GCRealTimeMon -n devenv
```

Example output:

```
------- press s for current stats or any other key to exit -------

Monitoring process with name: Samples.AspNet5 and pid: 45044
GC#     index |            type |   gen | pause (ms) |                reason |
------------------------------------------------------------------------------
GC#         1 | NonConcurrentGC |     0 |       7.45 |            AllocSmall |
GC#         2 | NonConcurrentGC |     1 |      17.88 |            AllocSmall |
GC#         3 | NonConcurrentGC |     0 |       3.20 |            AllocSmall |
------------------------------------------------------------------------------
Heap Stats as of 2021-11-08 03:15:30Z (Run 1 for gen 0):
  Heaps: 16
  Handles: 2,015
  Pinned Obj Count: 8
  Last Run Stats:
    Total Heap: 15,846,992 Bytes
      Gen 0:               384 Bytes
      Gen 1:        10,718,432 Bytes
      Gen 2:               384 Bytes
      Gen 3:         4,358,056 Bytes
      Gen 4:           769,736 Bytes
------------------------------------------------------------------------------
```

## Configuration

The configuration file is a YAML based file currently with the following sections:
- __columns__: The columns to display. 
- __available_columns__: All columns that are available to display.
- __stats_mode__: Configurations related to the heap stats. 
  - ``timer``: Specifying this with a period magnitude and type that dictates the candence of the timer that prints the heap stats.
    - the period type can either be in minutes 'm' or seconds 's'.
    - the period magnitude has to be as an int.
    - Examples: 
      - ``"timer" : "5m"  # 5 minutes``
      - ``"timer" : "20s" # 20 seconds``
  - A full example with the Heap printing every 30 seconds can be found [here](tests/GCRealTimeMon.UnitTests/ConfigurationReader/TestConfigurations/DefaultWithStatsMode.yaml)
- __display_conditions__: Conditions via which info about each GC is displayed.
  - ``min gc duration (msec)``: Specifying this value will filter GCs with pause durations less than the said value. 
  - Examples:
    - ``min gc duration (msec) : 200``
    - ``min gc duration (msec) : 10.0``
    - ``min gc duration (msec) : 200.254``

Currently, the available columns are:

| Column Name            | Full Name / Description                                                                                                                                                         | Trace Event Property                                                                      |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| index | The GC Index. | ``TraceGC.Number``
| gen | The Generation. | ``TraceGC.Generation``
| type | The Type of GC. | ``TraceGC.Type``
| reason | Reason for GC. | ``TraceGC.Reason``
| suspension time (ms)   | The time in milliseconds that it took to suspend all threads to start this GC | `TraceGC.SuspendDurationMSec`                                                             |
| pause time (ms)   | The time managed threads were paused during this GC, in milliseconds | `TraceGC.PauseDurationMSec`    |
| pause time (%)           | The amount of time that execution in managed code is blocked because the GC needs exclusive use to the heap. For background GCs this is small.                                   | `TraceGC.PauseTimePercentageSinceLastGC`                                                  |
| gen0 alloc (mb)        | Amount allocated in Gen0 since the last GC occurred in MB.                                                                                                                       | `TraceGC.UserAllocated[(int)Gens.Gen0]`                                                   |
| gen0 alloc rate        | The average allocation rate since the last GC.                                                                                                                                   | `(TraceGC.UserAllocated[(int)Gens.Gen0] * 1000.0) / TraceGC.DurationSinceLastRestartMSec` |
| peak size (mb)         | The size on entry of this GC (includes fragmentation) in MB.                                                                                                                     | `TraceGC.HeapSizePeakMB`                                                                  |
| after size (mb)        | The size on exit of this GC (includes fragmentation) in MB.                                                                                                                      | `TraceGC.HeapSizeAfterMB`                                                                 |
| peak/after             | Peak / After                                                                                                                                                                    | `TraceGC.HeapSizePeakMB / TraceGC.HeapSizeAfterMB`                                        |
| promoted (mb)          | Memory this GC promoted in MB.                                                                                                                                                   | `TraceGC.PromotedMB`                                                                      |
| gen0 size (mb)         | Size of gen0 at the end of this GC in MB.                                                                                                                                        | `TraceGC.GenSizeAfterMB(Gens.Gen0)`                                                       |
| gen0 survival rate     | The % of objects in Gen0 that survived this GC.                                                                                                                                 | `TraceGC.SurvivalPercent(Gens.Gen0)`                                                      |
| gen0 frag ratio        | The % of fragmentation on Gen0 at the end of this GC.                                                                                                                            | `TraceGC.GenFragmentationPercent(Gens.Gen0)`                                              |
| gen1 size (mb)         | Size of gen1 at the end of this GC in MB.                                                                                                                                        | `TraceGC.GenSizeAfterMB(Gens.Gen1)`                                                       |
| gen1 survival rate     | The % of objects in Gen1 that survived this GC. Only available if we are doing a gen1 GC.                                                                                       | `TraceGC.SurvivalPercent(Gens.Gen1)`                                                      |
| gen1 frag ratio        | The % of fragmentation on Gen1 at the end of this GC.                                                                                                                            | `TraceGC.GenFragmentationPercent(Gens.Gen1)`                                              |
| gen2 size (mb)         | Size of Gen2 in MB at the end of this GC.                                                                                                                                       | `TraceGC.GenSizeAfterMB(Gens.Gen2)`                                                       |
| gen2 survival rate     | The % of objects in Gen2 that survived this GC. Only available if we are doing a gen2 GC.                                                                                       | `TraceGC.SurvivalPercent(Gens.Gen2)`                                                      |
| gen2 frag ratio        | The % of fragmentation on Gen2 at the end of this GC.                                                                                                                            | `TraceGC.GenFragmentationPercent(Gens.Gen2)`                                              |
| LOH size (mb)          | Size of Large object heap (LOH) at the end of this GC in MB.                                                                                                                     | `TraceGC.GenSizeAfterMB(Gens.LargeObj)`                                                   |
| LOH survival rate      | The % of objects in the large object heap (LOH) that survived the GC. Only available if we are doing a gen2 GC.                                                                  | `TraceGC.SurvivalPercent(Gens.LargeObj)`                                                  |
| LOH frag ratio         | The % of fragmentation on the large object heap (LOH) at the end of this GC.                                                                                                     | `TraceGC.GenFragmentationPercent(Gens.LargeObj)`                                          |
| finalize promoted (mb) | The size of finalizable objects that were discovered to be dead and so promoted during this GC, in MB.                                                                           | `TraceGC.HeapStats.FinalizationPromotedSize / 1000000.0`                                  |
| pinned objects         | Number of pinned objects observed in this GC.                                                                                                                                    | `TraceGC.HeapStats.PinnedObjectCount`                                                     |

## Call Stacks

In an effort to aid with perf diagnosis, call stacks are currently available for induced GCs and large object allocations on Windows, only. Enabling call stacks requires the process to run with administrator privileges.

### Troubleshooting

#### Symbols aren't properly resolved

The symbol resolution logic relies on the fact that _NT_SYMBOL_PATH is populated with the appropriate symbol path(s). More details on how this can be troubleshooted [here](https://docs.microsoft.com/en-us/visualstudio/profiling/how-to-specify-symbol-file-locations-from-the-command-line?view=vs-2017).

An example of the _NT_SYMBOL_PATH is:
``;SRV*C:\Symbols*https://msdl.microsoft.com/download/symbols;SRV*C:\Symbols*https://nuget.smbsrc.net;SRV*C:\Symbols*https://referencesource.microsoft.com/symbols``

#### Getting the following exception: ``Unhandled exception. System.Runtime.InteropServices.COMException (0x800705AA): Insufficient system resources exist to complete the requested service. (0x800705AA)`` 

This exception is thrown as a result of session exhaustion i.e. enough sessions are being created that you're unable to create another. You can troubleshoot this issue in 2 ways:

1. Rebooting your machine will clear up any zombie sessions.
2. Manually stopping sessions
   1. Open up a command prompt / powershell instance in admin mode.
   2. Enter the following command to check to view the list of all the sessions: ``logman query -ets``.
   3. Stop dangling session(s) using: ``logman -ets stop <NameOfSession>``.

More details on this issue can be found [here](https://github.com/microsoft/perfview/issues/1166).

## Adding New Columns

The process to add a new column from the ``TraceGC`` event is the following:

1. Include the column name in the ``available_columns`` in the Default.yaml config.
2. Define a ``ColumnInfo`` object in the ``ColumnInfoMap`` with the following properties:
   1. The name
   2. Alignment
   3. A ``Func<TraceGC, object>`` that looks up an object in via a ``TraceGC`` event.
   4. Format (optional)
3. Optionally add corresponding unit tests.
4. Update the documentation here with the new column.

## Theming
By default, output is colored and formatted using [Spectre.Console](https://spectreconsole.net/). If stdout is being redirected, then plain text formatting is used instead. A dark theme is used by default unless the console background color is detected as white, then a light theme is used. You
can configure all colors using the following fields in the `theme` yaml node:

(All color values can be defined using [Spectre Color strings](https://spectreconsole.net/appendix/colors) or hex values.)

| Name                  | Description                                                                                                                                       |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| use_plain_text        | Set to true to force the output to be plain text. Default is false. Note that the [NO_COLOR initiative](https://no-color.org/) is also supported. |
| gc_table_header_color | The color for the GC table headers.                                                                                                               |
| gen0_row_color        | The color for row entries of Gen 0 events in the GC table.                                                                                        |
| gen1_row_color        | The color for row entries of Gen 1 events in the GC table.                                                                                        |
| gen2_row_color        | The color for row entries of Gen 2 events in the GC table.                                                                                        |
| gen0_heap_color       | The color for gen0 stats in the heap stats table.                                                                                                 |
| gen1_heap_color       | The color for gen1 stats in the heap stats table.                                                                                                 |
| gen2_heap_color       | The color for gen2 stats in the heap stats table.                                                                                                 |
| gen3_heap_color       | The color for gen3 (LOH) stats in the heap stats table.                                                                                           |
| gen4_heap_color       | The color for gen4 (pinned object heap) stats in the heap stats table.                                                                            |
| total_heap_color      | The color used to for information about total heap stats in the heap stats table.                                                                 |
| highlight_color       | The color used to call out special information, like the process being monitored.                                                                 |
| message_color         | The color used for standard messages written to output.                                                                                           |
| warning_color         | The color used for warning messages written to output.                                                                                            |

## Unit Tests

The unit tests are in the ``test`` directory and can be run by:

```
dotnet test
```

## Building

**Build with VS**

Open ``GCRealTimeMon.sln`` and build it with Visual Studio.

**Build with command line**

```bash
cd src/GCRealTimeMon
dotnet publish -c Release -r win-x64 # build on Windows
dotnet publish -c Release -r linux-x64 # build on Linux
dotnet publish -c Release -r osx-x64 # build on macOS
```

Additionaly, you can pass `/p:AotCompilation=true` to build GCRealTimeMon with [NativeAOT](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT).
This requires native C++ toolchain (MSVC or clang) to be installed on the machine.

```bash
cd src/GCRealTimeMon
dotnet publish -c Release -r win-x64 /p:AotCompilation=true # build on Windows
dotnet publish -c Release -r linux-x64 /p:AotCompilation=true # build on Linux
dotnet publish -c Release -r osx-x64 /p:AotCompilation=true # build on macOS
```

Build artifacts can be found in `bin/Release/netcoreapp3.1/[rid]/publish`.

**Build dotnet-gcmon tool with command line**

```bash
cd src/dotnet-gcmon
dotnet build -c Release
```

## How to generate the global .NET CLI tool dotnet-gcmon

Such a tool is simply a console application stored in a nuget package with some specific properties.
The dotnet-gcmon.csproj file contains the corresponding settings and link to the implementation of the GCRealTimeMon console application.

When building this dotnet-gcmon C# project, a nuget package (dotnet-gcmon.(version x.y.z).nupkg) is generated under the nupkg folder.
Ensure that the configuration is "Release" to publish a new version.

It is also possible to manually generate the package in Release, by using the following command in the .csproj folder:
```bash
dotnet pack -c Release
```

If you make changes to the global tool you should test it locally by first uninstalling an existing version 

````bash
dotnet tool uninstall -g dotnet-gcmon
````

and then installing the local .nupkg with this command line:

```bash
dotnet tool install -g dotnet-gcmon --version 0.5.0 --add-source C:\realmon\src\dotnet-gcmon\nupkg\
```

(replace `0.5.0` with the version you specified in the .csproj and  `C:\realmon` with the name of your dir for the tool)

To publish a new version, upload the new dotnet-gcmon.(version x.y.z).nupkg file to https://www.nuget.org/packages/manage/upload.

After a while, it should appear under https://www.nuget.org/packages/dotnet-gcmon.

At that point, use the following command to install it on a machine:

```bash
   dotnet tool install -g dotnet-gcmon
```

NOTE: to prepare a new version, update the following property
    <PackageVersion>x.y.z</PackageVersion>
in the .csproj file before building/generating the nuget package.

NOTE: when new files are added to GCRealTimeMon project, don't forget to add them as links in the dotnet-gcmon.csproj file.

Read https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create for more details about creating a global .NET CLI tool.


## Contribution

Contributions are very welcome! Tasks I'm currently thinking about -

+ More info about GCs (the TraceGC class in TraceEvent provides a ton of info about each GC).
+ Take more command line args that allow uses to specify things like only show GCs that are blocking or show the allocated bytes in gen0/LOH inbetween GCs.

If you are interested in working on any of these, your contributions are very much appreciated. Or if you have suggestions on other features, feel free to open an issue or a PR. 
