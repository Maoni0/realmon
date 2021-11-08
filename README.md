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

**Building**

Currently it's a VS solution. It uses the TraceEvent library.

**Contribution**

Contributions are very welcome! Tasks I'm currently thinking about -

+ Build with dotnet CLI in addition to a VS solution so folks can use this on Linux.
+ More info about GCs (the TraceGC class in TraceEvent provides a ton of info about each GC).
+ Take more command line args that allow uses to specify things like only show GCs that are blocking or show the allocated bytes in gen0/LOH inbetween GCs.

If you are interested in working on any of these, your contributions are very much appreciated. Or if you have suggestions on other features, feel free to open an issue or a PR. 
