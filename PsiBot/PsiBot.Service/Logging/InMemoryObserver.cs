using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PsiBot.Services.Logging
{
    /// <summary>
    /// Captures log events in memory to support diagnostic route inspection.
    /// </summary>
    public class InMemoryObserver: IObserver<LogEvent>, IDisposable
    {
        private static readonly int MaxLogCount = 5000;

        private IDisposable subscription;

        private LinkedList<string> logs = new LinkedList<string>();

        private object lockLogs = new object();

        private ILogEventFormatter formatter = new CommsLogEventFormatter();

        /// <summary>
        /// Initializes a new observer instance and subscribes to Graph logging.
        /// </summary>
        /// <param name="logger">Graph logger that produces log events.</param>
        public InMemoryObserver(IGraphLogger logger)
        {
            // Log unhandled exceptions.
            AppDomain.CurrentDomain.UnhandledException += (_, e) => logger.Error(e.ExceptionObject as Exception, $"Unhandled exception");
            TaskScheduler.UnobservedTaskException += (_, e) => logger.Error(e.Exception, "Unobserved task exception");

            this.subscription = logger.Subscribe(this);
        }

        /// <summary>
        /// Returns the requested slice of stored log entries.
        /// </summary>
        /// <param name="skip">Number of entries to skip before returning results.</param>
        /// <param name="take">Maximum number of entries to return.</param>
        /// <returns>Concatenated log payload.</returns>
        public string GetLogs(int skip = 0, int take = int.MaxValue)
        {
            lock (this.lockLogs)
            {
                skip = skip < 0 ? Math.Max(0, this.logs.Count + skip) : skip;
                var filteredLogs = this.logs
                    .Skip(skip)
                    .Take(take);
                return string.Join(Environment.NewLine, filteredLogs);
            }
        }

        /// <summary>
        /// Returns the requested slice of stored log entries filtered by content.
        /// </summary>
        /// <param name="filter">Substring used to filter log messages.</param>
        /// <param name="skip">Number of entries to skip before returning results.</param>
        /// <param name="take">Maximum number of entries to return.</param>
        /// <returns>Concatenated log payload.</returns>
        public string GetLogs(string filter, int skip = 0, int take = int.MaxValue)
        {
            lock (this.lockLogs)
            {
                skip = skip < 0 ? Math.Max(0, this.logs.Count + skip) : skip;
                var filteredLogs = this.logs
                    .Where(log => log.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Skip(skip)
                    .Take(take);
                return string.Join(Environment.NewLine, filteredLogs);
            }
        }

        /// <inheritdoc />
        public void OnNext(LogEvent logEvent)
        {
            if (logEvent.EventType == LogEventType.Metric)
            {
                return;
            }

            var logString = this.formatter.Format(logEvent);
            lock (this.lockLogs)
            {
                this.logs.AddFirst(logString);
                if (this.logs.Count > MaxLogCount)
                {
                    this.logs.RemoveLast();
                }
            }
        }

        /// <inheritdoc />
        public void OnError(Exception error)
        {
        }

        /// <inheritdoc />
        public void OnCompleted()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (this.lockLogs)
            {
                this.logs?.Clear();
                this.logs = null;
            }

            this.subscription?.Dispose();
            this.subscription = null;
            this.formatter = null;
        }
    }
}
