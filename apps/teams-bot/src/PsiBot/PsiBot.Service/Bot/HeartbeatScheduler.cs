using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Base class that manages a timer-driven heartbeat for derived components.
    /// </summary>
    public abstract class HeartbeatScheduler : ObjectRootDisposable
    {
        /// <summary>
        /// Timer responsible for invoking the heartbeat callback.
        /// </summary>
        private Timer heartbeatTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartbeatScheduler"/> class.
        /// </summary>
        /// <param name="frequency">The frequency of the heartbeat.</param>
        /// <param name="logger">The graph logger.</param>
        public HeartbeatScheduler(TimeSpan frequency, IGraphLogger logger)
            : base(logger)
        {
            // initialize the timer
            var timer = new Timer(frequency.TotalMilliseconds);
            timer.Enabled = true;
            timer.AutoReset = true;
            timer.Elapsed += this.HeartbeatDetected;
            this.heartbeatTimer = timer;
        }

        /// <summary>
        /// Invoked whenever the configured heartbeat interval elapses.
        /// </summary>
        /// <param name="args">The elapsed event args.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        protected abstract Task HeartbeatAsync(ElapsedEventArgs args);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.heartbeatTimer.Elapsed -= this.HeartbeatDetected;
            this.heartbeatTimer.Stop();
            this.heartbeatTimer.Dispose();
        }

        /// <summary>
        /// The heartbeat function.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The elapsed event args.</param>
        private void HeartbeatDetected(object sender, ElapsedEventArgs args)
        {
            var task = $"{this.GetType().FullName}.{nameof(this.HeartbeatAsync)}(args)";
            this.GraphLogger.Verbose($"Starting running task: " + task);
            _ = Task.Run(() => this.HeartbeatAsync(args)).ForgetAndLogExceptionAsync(this.GraphLogger, task);
        }
    }
}
