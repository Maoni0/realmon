# GCRealTimeMon
This is a monitoring tool that tells you when GCs happen in a .NET process and characteristics about these GCs.

Right now it's super simple - given a PID or process name it will show you a few things about GCs as they happen in that process.

## Command Line Arguments

| Command Line Argument | Name | Description |
|-----|-----|-----|
| n | Name of the Process | Grabs the first process with the name matching that with the one specified with this argument |
| p | Process Id | Process Id to monitor GC for |
| m | Minimum Duration in Ms for GC Pause Duration | Any GC Pauses below this specified value will not be written to the console log - this is an optional argument |

Note: Either the name of the process or the process id must be specified, else an ``ArgumentException`` is thrown.

## Example Usage

```
C:\realmon\src\windows\bin\x64\Release\net5.0>GCRealTimeMon 14028
```

or

```
C:\realmon\src\windows\bin\x64\Release\net5.0>GCRealTimeMon devenv
```

Example output:

```
-------press any key to exit ☺-------

Monitoring process 14028

GC#     index |            type |   gen | pause (ms)
----------------------------------------------------

GC#       674 | NonConcurrentGC |     1 |       7.24
GC#       675 | NonConcurrentGC |     1 |      17.29
GC#       676 | NonConcurrentGC |     1 |       9.33
GC#       677 | NonConcurrentGC |     1 |      22.98
GC#       678 | NonConcurrentGC |     1 |      20.00
GC#       679 | NonConcurrentGC |     1 |      14.12
GC#       680 | NonConcurrentGC |     1 |      10.58
GC#       681 | NonConcurrentGC |     1 |       4.94
GC#       682 | NonConcurrentGC |     1 |      12.43
```

**Building**

Currently it's a VS solution. It uses the TraceEvent library.

**Contribution**

Contributions are very welcome! Tasks I'm currently thinking about -

+ Build with dotnet CLI in addition to a VS solution so folks can use this on Linux.
+ More info about GCs (the TraceGC class in TraceEvent provides a ton of info about each GC).
+ Take more command line args that allow uses to specify things like process name or only show GCs that are blocking/longer than Xms/etc.

If you are interested in working on any of these, your contributions are very much appreciated. Or if you have suggestions on other features, feel free to open an issue or a PR. 
