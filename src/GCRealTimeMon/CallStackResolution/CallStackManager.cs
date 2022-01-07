using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using realmon.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace realmon.CallStackResolution
{
    public static class CallStackManager
    {
        private static SymbolReader m_symbolReader;

        public static TraceLogEventSource InitializeAndRegisterCallStacks(Configuration.Configuration configuration, 
                                                                          TraceEventSession traceEventSession, 
                                                                          int processId)
        {
            // TODO: Add configurations for the Symbol Reader Text Output path and the NT_SymbolPath.
            string symbolPath = SymbolPath.SymbolPathFromEnvironment;

            // If _NT_SYMBOL_PATH isn't set, force it to default to the one mentioned in the README of the project.
            if (string.IsNullOrWhiteSpace(symbolPath))
            {
                symbolPath = @";SRV*C:\Symbols*https://msdl.microsoft.com/download/symbols;SRV*C:\Symbols*https://nuget.smbsrc.net;SRV*C:\Symbols*https://referencesource.microsoft.com/symbols";
            }

            m_symbolReader = new SymbolReader(TextWriter.Null, symbolPath); 
            m_symbolReader.SecurityCheck = path => true;

            // Setup the additional providers needed for the enabling of the callstacks.
            // Image Load and Process needed for Native Symbol Resolution.
            // Requires Admin privileges, else this will throw.
            traceEventSession.EnableKernelProvider(
                flags:
             KernelTraceEventParser.Keywords.ImageLoad |
             KernelTraceEventParser.Keywords.Process,
                stackCapture:
             KernelTraceEventParser.Keywords.None
             );

            // Needed for the GC events + call stacks.
            TraceEventProviderOptions options = new TraceEventProviderOptions() { ProcessIDFilter = new[] { processId }};
            traceEventSession.EnableProvider(
                ClrTraceEventParser.ProviderGuid,
                TraceEventLevel.Verbose, // Verbose needed for call stacks.
                (ulong)(ClrTraceEventParser.Keywords.GC                |
                ClrTraceEventParser.Keywords.Loader                    |
                ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
                ClrTraceEventParser.Keywords.JITSymbols                |
                ClrTraceEventParser.Keywords.JitTracing                |
                ClrTraceEventParser.Keywords.GCAllObjectAllocation     |
                ClrTraceEventParser.Keywords.Jit                       |
                ClrTraceEventParser.Keywords.Codesymbols               |
                ClrTraceEventParser.Keywords.Interop                   |
                ClrTraceEventParser.Keywords.NGen                      |
                ClrTraceEventParser.Keywords.MethodDiagnostic          |
                ClrTraceEventParser.Keywords.Stack), options);

            // Needed for JIT Compile code that was already compiled. 
            traceEventSession.EnableProvider(ClrRundownTraceEventParser.ProviderGuid, TraceEventLevel.Verbose,
                (ulong)(ClrTraceEventParser.Keywords.Jit               |
                ClrTraceEventParser.Keywords.Loader                    |
                ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
                ClrTraceEventParser.Keywords.JITSymbols                |
                ClrTraceEventParser.Keywords.Jit                       |
                ClrTraceEventParser.Keywords.GCAllObjectAllocation     |
                ClrTraceEventParser.Keywords.JitTracing                |
                ClrTraceEventParser.Keywords.Codesymbols               |
                ClrTraceEventParser.Keywords.Interop                   |
                ClrTraceEventParser.Keywords.GC                        |
                ClrTraceEventParser.Keywords.NGen                      |
                ClrTraceEventParser.Keywords.MethodDiagnostic          |
                ClrTraceEventParser.Keywords.StartEnumeration), options);

            // Subscribe to the requested events.
            TraceLogEventSource traceLogEventSource = TraceLog.CreateFromTraceEventSession(traceEventSession);
            traceLogEventSource.TraceLog.Clr.GCTriggered += (GCTriggeredTraceData traceEvent) =>
            {
                if (traceEvent.Reason == GCReason.Induced)
                {
                    PrintCallStack(traceEvent, configuration);
                }
            };

            traceLogEventSource.TraceLog.Clr.GCAllocationTick += (GCAllocationTickTraceData traceEvent) =>
            {
                if (traceEvent.AllocationKind == GCAllocationKind.Large)
                {
                    PrintCallStack(traceEvent, configuration);
                }
            };

            return traceLogEventSource;
        }

        internal static void PrintCallStack(TraceEvent data, Configuration.Configuration configuration)
        {
            Console.WriteLine(PrintUtilities.GetLineSeparator(configuration));
            Console.WriteLine($"Callstack Type: {data.EventName}\n");
            var callStack = data.CallStack();
            if (callStack != null)
            {
                ResolveSymbols(callStack);
                PrintCallStack(callStack);
            }
            Console.WriteLine(PrintUtilities.GetLineSeparator(configuration));
        }

        internal static void ResolveSymbols(TraceCallStack callStack)
        {
            while (callStack != null)
            {
                var codeAddress = callStack.CodeAddress;
                if (codeAddress.Method == null)
                {
                    var moduleFile = codeAddress.ModuleFile;
                    if (moduleFile != null)
                    {
                        codeAddress.CodeAddresses.LookupSymbolsForModule(m_symbolReader, moduleFile);
                    }
                }

                callStack = callStack.Caller;
            }
        }

        internal static void PrintCallStack(TraceCallStack callStack)
        {
            while (callStack != null)
            {
                var codeAddress = callStack.CodeAddress;

                // Like WinDbg, display unresolved modules with the address in Hex form.
                if (codeAddress.ModuleFile == null)
                {
                    Console.WriteLine("0x{0:x}", codeAddress.Address);
                }
                else
                {
                    Console.WriteLine($"{codeAddress.ModuleName}!{codeAddress.FullMethodName}");
                }

                callStack = callStack.Caller;
            }
        }
    }
}
