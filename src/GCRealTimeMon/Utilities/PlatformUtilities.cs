using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace realmon.Utilities
{
    internal static class PlatformUtilities
    {
        public static TraceEventDispatcher GetTraceEventDispatcherBasedOnPlatform(Configuration.Configuration configuration,
                                                                                  int processId, 
                                                                                  bool enableCallStacks,
                                                                                  out IDisposable session)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var traceEventSession = new TraceEventSession($"GCRealMonSession_{Guid.NewGuid()}");

                // If the user requested call stacks, enable them via the CallStackManager.
                if (enableCallStacks)
                {
                    CallStackResolution.CallStackManager.InitializeAndRegisterCallStacks(configuration: configuration,
                                                                                         traceEventSession: traceEventSession,
                                                                                         processId: processId);
                }

                else
                {
                    traceEventSession.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Informational, (ulong)ClrTraceEventParser.Keywords.GC);
                }

                session = traceEventSession;
                return traceEventSession.Source;
            }

            else
            {
                var providers = new List<EventPipeProvider>()
                {
                    new EventPipeProvider("Microsoft-Windows-DotNETRuntime",
                        EventLevel.Informational, (long)ClrTraceEventParser.Keywords.GC)
                };

                var client = new DiagnosticsClient(processId);
                EventPipeSession eventPipeSession = client.StartEventPipeSession(providers, false);
                var source = new EventPipeEventSource(eventPipeSession.EventStream);
                session = eventPipeSession;
                return source;
            }
        }
    }
}
