# GCRealTimeMon
This is a monitoring tool that tells you when GCs happen in a .NET process and characteristics about these GCs.

Right now it's super simple - given a PID or process name it will show you a few things about GCs as they happen in that process.

## Command Line Arguments

| Command Line Argument | Name | Description |
|-----|-----|-----|
| n | Name of the Process | Grabs the first process with the name matching that with the one specified with this argument |
| p | Process Id | Process Id to monitor GC for |
| m | Minimum Duration in Ms for GC Pause Duration | Any GC Pauses below this specified value will not be written to the console log - this is an optional argument |
| c | Path of the Configuration File | Path to the configuration file used by the monitor. This file is a YAML file. By default, it's the Default.yaml file is loaded | 

Note: Either the name of the process or the process id must be specified, else an ``ArgumentException`` is thrown.

## Runtime Keys

| Key | Action |
|-----|-----|
| `s` | Prints detailed stats of the last collection and the state of each generation |

## Example Usage

```
C:\realmon\src\windows\bin\x64\Release\net5.0>GCRealTimeMon -p 14028
```

or

```
C:\realmon\src\windows\bin\x64\Release\net5.0>GCRealTimeMon -n devenv
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
| after size (mb)        | The size on exit of thi sGC (includes fragmentation) in MB.                                                                                                                      | `TraceGC.HeapSizeAfterMB`                                                                 |
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
dotnet publish -c Release -r win-x64 # build on Windows
dotnet publish -c Release -r linux-x64 # build on Linux
dotnet publish -c Release -r osx-x64 # build on macOS
```

Additionaly, you can pass `/p:AotCompilation=true` to build GCRealTimeMon with [NativeAOT](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT).
This requires native C++ toolchain (MSVC or clang) to be installed on the machine.

```bash
dotnet publish -c Release -r win-x64 /p:AotCompilation=true # build on Windows
dotnet publish -c Release -r linux-x64 /p:AotCompilation=true # build on Linux
dotnet publish -c Release -r osx-x64 /p:AotCompilation=true # build on macOS
```

Build artifacts can be found in `bin/Release/net6.0/[rid]/publish`.

## Contribution

Contributions are very welcome! Tasks I'm currently thinking about -

+ More info about GCs (the TraceGC class in TraceEvent provides a ton of info about each GC).
+ Take more command line args that allow uses to specify things like only show GCs that are blocking or show the allocated bytes in gen0/LOH inbetween GCs.

If you are interested in working on any of these, your contributions are very much appreciated. Or if you have suggestions on other features, feel free to open an issue or a PR. 
