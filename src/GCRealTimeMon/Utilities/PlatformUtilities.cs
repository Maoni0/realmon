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

        public static bool IsAdminForWindows()
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
            
            // Since this method is only for Windows, return false in the case we are running this for MacOS/Linux. 
            else
            {
                return false;
            }
        }

        public static TraceEventDispatcher GetTraceEventDispatcherBasedOnPlatform(int processId, 
                                                                                  IConsoleOut consoleOut,
                                                                                  bool enableCallStacks,
                                                                                  out IDisposable session)
        {
            if (IsWindows)
            {
                var traceEventSession = new TraceEventSession($"GCRealMonSession_{Guid.NewGuid()}");

                // If the user requested call stacks, enable them via the CallStackManager.
                if (enableCallStacks)
                {
                    CallStackResolution.CallStackManager.InitializeAndRegisterCallStacks(traceEventSession: traceEventSession,
                                                                                         consoleOut: consoleOut,
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
