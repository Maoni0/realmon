using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using realmon.Utilities;
using System.IO;

namespace realmon.CallStackResolution
{
    public static class CallStackManager
    {
        private static SymbolReader m_symbolReader;

        internal static TraceLogEventSource InitializeAndRegisterCallStacks(TraceEventSession traceEventSession, 
                                                                            IConsoleOut consoleOut,
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
                    HandlePrintingCallStacks(traceEvent, consoleOut);
                }
            };

            traceLogEventSource.TraceLog.Clr.GCAllocationTick += (GCAllocationTickTraceData traceEvent) =>
            {
                if (traceEvent.AllocationKind == GCAllocationKind.Large)
                {
                    HandlePrintingCallStacks(traceEvent, consoleOut); 
                }
            };

            return traceLogEventSource;
        }

        internal static void HandlePrintingCallStacks(TraceEvent data, IConsoleOut consoleOut)
        {
            var callStack = data.CallStack();
            if (callStack != null)
            {
                ResolveSymbols(callStack);
                // consoleOut.PrintCallStack holds a write lock => don't resolve symbols, that can potentially be a long running task in the lock.
                consoleOut.PrintCallStack(callStack, data.EventName);
            }
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
    }
}
