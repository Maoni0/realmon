using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using realmon.Utilities;
using System;
using System.Reactive.Subjects;

namespace realmon.Service
{
    internal sealed class GCRealTimeMonService : IGCRealTimeMonService
    {
        public static Lazy<IGCRealTimeMonService> Instance = new Lazy<IGCRealTimeMonService>(new GCRealTimeMonService());

        public IGCRealTimeMonResult Initialize(int pid, Configuration.Configuration configuration)
        {
            double? minDurationForGCPausesInMSec = null;
            if (configuration.DisplayConditions != null && 
                configuration.DisplayConditions.TryGetValue("min gc duration (msec)", out var minDuration))
            {
                minDurationForGCPausesInMSec = double.Parse(minDuration);
            }

            var source = PlatformUtilities.GetTraceEventDispatcherBasedOnPlatform(pid, out var session);
            Subject<TraceGC> gcEndSubject = new Subject<TraceGC>();
            source.NeedLoadedDotNetRuntimes();
            source.AddCallbackOnProcessStart((TraceProcess proc) =>
            {
                if (proc.ProcessID != pid)
                {
                    return;
                }

                Action<TraceProcess, TraceGC> gcEndAction = 
                    (p, gc) =>
                    {
                        if (p.ProcessID == pid)
                        {
                            // If no min duration is specified or if the min duration specified is less than the pause duration, log the event.
                            if (!minDurationForGCPausesInMSec.HasValue ||
                                (minDurationForGCPausesInMSec.HasValue && minDurationForGCPausesInMSec.Value < gc.PauseDurationMSec))
                            {
                                gcEndSubject.OnNext(gc);
                            }
                        }
                    };

                proc.AddCallbackOnDotNetRuntimeLoad((TraceLoadedDotNetRuntime runtime) =>
                {
                    // TODO: When there are multiple clients, fix this leak by unsubscribing. 
                    runtime.GCEnd += gcEndAction; 
                });
            });

            return new GCRealTimeMonResult(gcEndSubject: gcEndSubject, 
                                           source: source,
                                           session: session);
        }

        private sealed class GCRealTimeMonResult : IGCRealTimeMonResult 
        {
            private Subject<TraceGC> m_gcEndSubject;
            private IDisposable m_session;

            public GCRealTimeMonResult(Subject<TraceGC> gcEndSubject, TraceEventDispatcher source, IDisposable session)
            {
                m_gcEndSubject = gcEndSubject;
                Source = source;
                m_session = session;
            }

            private bool disposedValue;

            public IObservable<TraceGC> GCEndObservable => m_gcEndSubject;
            public TraceEventDispatcher Source { get; } 

            private void Dispose(bool disposing)
            {
                m_gcEndSubject?.Dispose();

                if (!disposedValue)
                {
                    Source?.Dispose();
                    m_session?.Dispose();
                    m_gcEndSubject = null;
                    disposedValue = true;
                }
            }

             ~GCRealTimeMonResult()
             {
                 Dispose(disposing: false);
             }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
