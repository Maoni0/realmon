using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System;

namespace realmon.Service
{
    internal interface IGCRealTimeMonService
    {
        IGCRealTimeMonResult Initialize(int pid, Configuration.Configuration configuration);
    }

    internal interface IGCRealTimeMonResult : IDisposable
    {
        TraceEventDispatcher Source { get; }
        IObservable<TraceGC> GCEndObservable { get; }
    }
}
