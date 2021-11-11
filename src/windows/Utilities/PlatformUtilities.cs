using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
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
        public static TraceEventDispatcher GetTraceEventDispatcherBasedOnPlatform(int processId, out IDisposable session)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var traceEventSession = new TraceEventSession("MySession");
                traceEventSession.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Informational, (ulong)ClrTraceEventParser.Keywords.GC);
                session = traceEventSession;
                return traceEventSession.Source;
            }

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var providers = new List<EventPipeProvider>()
                {
                    new EventPipeProvider("Microsoft-Windows-DotNETRuntime",
                        EventLevel.Informational, (long)ClrTraceEventParser.Keywords.GC)
                };

                var client = new DiagnosticsClient(processId);
                EventPipeSession eventPipeSession = client.StartEventPipeSession(providers, false))
                var source = new EventPipeEventSource(eventPipeSession.EventStream);
                session = eventPipeSession;
                return source;
            }

            throw new PlatformNotSupportedException();
        }
    }
}
