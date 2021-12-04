using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace realmon.Utilities
{
    internal static class PlatformUtilities
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [DllImport("libc")]
        public static extern uint getuid();
        public static bool IsRoot()
        {
            if (IsWindows)
            {
                bool isAdmin;
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }

                return isAdmin;
            }
            
            // For Linux/MacOS, checking getuid() == 0 gives us sudo access.
            else
            {
                return getuid() == 0;
            }
        }

        public static TraceEventDispatcher GetTraceEventDispatcherBasedOnPlatform(int processId, out IDisposable session)
        {
            if (IsWindows)
            {
                var traceEventSession = new TraceEventSession($"GCRealMonSession_{Guid.NewGuid()}");
                traceEventSession.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Informational, (ulong)ClrTraceEventParser.Keywords.GC);
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
