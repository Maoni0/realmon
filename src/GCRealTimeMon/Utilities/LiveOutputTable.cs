namespace GCRealTimeMon.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    using Microsoft.Diagnostics.Tracing.Analysis.GC;

    using realmon.Utilities;

    using Spectre.Console;

    using Configuration = realmon.Configuration.Configuration;

    internal class LiveOutputTable : IDisposable
    {
        // A channel used to queue row data between the TraceEvent listening thread (publisher) and the Spectre Console writing thread (subscriber)
        private readonly Channel<string[]> channel;
        private readonly Configuration configuration;

        // A task used to track the currently running Spectre.Console live table thread
        private Task runningLiveTable;

        // Used to signal to the Spectre.Console thread that it should stop writing to the console (so we can write something else, like stats.)
        private CancellationTokenSource cts;
        private bool disposed;

        public LiveOutputTable(Configuration configuration)
        {
            channel = Channel.CreateUnbounded<string[]>();
            this.ResetCancellation();
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private void ResetCancellation()
        {
            this.cts?.Dispose();
            this.cts = new CancellationTokenSource();
        }

        public void Start()
        {
            Table table = new Table();
            AddHeaders(table);

            CancellationToken ct = cts.Token;

            // Start the live updates and keep a reference to the task
            // We'll use this in Stop() to ensure live updates have stopped before returning to the caller.
            runningLiveTable = AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                while (!ct.IsCancellationRequested)
                {
                    var newRow = await channel.Reader.ReadAsync(ct);
                    table.AddRow(newRow);
                    ctx.Refresh();
                }
            });
        }

        internal async Task Stop()
        {
            this.cts.Cancel();
            try
            {
                await runningLiveTable;
            }
            catch (OperationCanceledException)
            {
            }
        }

        internal void Restart()
        {
            this.ResetCancellation();
            Start();
        }

        internal async Task WriteRow(TraceGC gc, Configuration configuration)
        {
            List<string> rowDetails = PrintUtilities.GetRowDetails2(gc, configuration);
            await channel.Writer.WriteAsync(rowDetails.ToArray());
        }

        private void AddHeaders(Table table)
        {
            var header = PrintUtilities.GetHeader2(this.configuration);
            foreach (var column in header)
            {
                table.AddColumn(column);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.cts.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
