namespace realmon.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Microsoft.Diagnostics.Tracing.Analysis.GC;
    using Spectre.Console;
    using Configuration = realmon.Configuration.Configuration;

    /// <summary>
    /// This class manages a live Spectre.Console output table of GC info
    /// </summary>
    /// <remarks>This class uses a <see cref="Channel{T}"/> to handle queuing of writes between the threads listening to GC events and the thread writing to the live table output.</remarks>
    internal class LiveOutputTable : IDisposable
    {
        // A channel used to queue row data between the TraceEvent listening thread (publisher) and the Spectre Console writing thread (subscriber)
        private readonly Channel<IList<string>> channel;
        private readonly Configuration configuration;

        // A task used to track the currently running Spectre.Console live table thread
        private Task runningLiveTableTask;

        // Used to signal to the Spectre.Console thread that it should stop writing to the console (so we can write something else, like stats.)
        private CancellationTokenSource cts;
        private bool disposed;

        public LiveOutputTable(Configuration configuration)
        {
            channel = Channel.CreateUnbounded<IList<string>>();
            this.ResetCancellation();
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Starts a live table that will write output to the console as <see cref="WriteRowAsync(TraceGC, Configuration)"/> is called.
        /// </summary>
        public void Start()
        {
            Table table = new Table();
            AddHeaders(table);

            CancellationToken ct = cts.Token;

            // Start the live updates and keep a reference to the returned Task
            // This task doesn't complete until the async delegate passed to StartAsync completes.
            // We'll use this Task in StopAsync() to ensure live updates have stopped before returning to the caller.
            runningLiveTableTask = AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                while (!ct.IsCancellationRequested)
                {
                    IList<string> newRow = await channel.Reader.ReadAsync(ct);
                    table.AddRow(newRow.Select(str => new Markup(str)));
                    ctx.Refresh();
                }
            });
        }

        /// <summary>
        /// Stops the live table, which would allow for something else to write output to the console.
        /// </summary>
        /// <returns>A task that completes when the live table has been stopped.</returns>
        public async Task StopAsync()
        {
            this.cts.Cancel();
            try
            {
                await runningLiveTableTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Restarts a live table that was previously stopped.
        /// </summary>
        public void Restart()
        {
            this.ResetCancellation();
            Start();
        }

        /// <summary>
        /// Writes a new row to the table based on the input TraceGC data & the current configuration.
        /// </summary>
        /// <param name="gc">The trace GC data to write.</param>
        /// <returns>A task indicating completion.</returns>
        public void WriteRow(TraceGC gc)
        {
            List<string> rowDetails = PrintUtilities.GetRowDetailsList(gc, this.configuration);
            bool wasWritten = channel.Writer.TryWrite(rowDetails);
            System.Diagnostics.Debug.Assert(wasWritten, "Expected the write to the channel to be successful since it is unbounded.");
        }

        private void ResetCancellation()
        {
            this.cts?.Dispose();
            this.cts = new CancellationTokenSource();
        }

        private void AddHeaders(Table table)
        {
            var header = PrintUtilities.GetHeaderList(this.configuration);
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
        }
    }
}
