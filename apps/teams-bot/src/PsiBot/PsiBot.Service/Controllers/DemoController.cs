using PsiBot.Model.Constants;
using PsiBot.Service.Settings;
using PsiBot.Services.Bot;
using PsiBot.Services.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PsiBot.Services.Controllers
{
    /// <summary>
    /// Exposes helper endpoints that simplify validating and troubleshooting the bot locally.
    /// </summary>
    public class DemoController : ControllerBase
    {
        private readonly IGraphLogger _logger;

        private readonly ITeamsCallLifecycleService _callLifecycleService;

        private readonly BotConfiguration botConfiguration;

        private readonly InMemoryObserver _observer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoController" /> class.
        /// </summary>
        public DemoController(ITeamsCallLifecycleService callLifecycleService, IOptions<BotConfiguration> botConfiguration, IGraphLogger logger, InMemoryObserver observer)
        {
            _logger = logger;
            _callLifecycleService = callLifecycleService;
            this.botConfiguration = botConfiguration.Value;
            _observer = observer;
        }

        /// <summary>
        /// Returns active calls tracked by the bot service.
        /// </summary>
        /// <returns>List of call metadata.</returns>
        [HttpGet]
        [Route(HttpRouteConstants.Calls + "/")]
        public IActionResult OnGetCalls()
        {
            _logger.Info("Getting calls");

            if (_callLifecycleService.ActiveCallCoordinators.IsEmpty)
            {
                return StatusCode(203);
            }

            var calls = new List<Dictionary<string, string>>();
            foreach (var callCoordinator in _callLifecycleService.ActiveCallCoordinators.Values)
            {
                var call = callCoordinator.Call;
                var callPath = "/" + HttpRouteConstants.CallRoute.Replace("{callLegId}", call.Id);
                var callUri = new Uri(botConfiguration.CallControlBaseUrl, callPath).AbsoluteUri;
                var values = new Dictionary<string, string>
                {
                    { "legId", call.Id },
                    { "scenarioId", call.ScenarioId.ToString() },
                    { "call", callUri },
                    { "logs", callUri.Replace("/calls/", "/logs/") },
                };
                calls.Add(values);
            }
            return Ok(calls);
        }

        /// <summary>
        /// Ends a call and releases associated resources.
        /// </summary>
        /// <param name="callLegId">Id of the call to end.</param>
        /// <returns>200 if the call was terminated successfully.</returns>
        [HttpDelete]
        [Route(HttpRouteConstants.CallRoute)]
        public async Task<IActionResult> OnEndCallAsync(string callLegId)
        {
            var message = $"Ending call {callLegId}";
            _logger.Info(message);
            
            try
            {
                await _callLifecycleService.EndCallByCallLegIdAsync(callLegId).ConfigureAwait(false);
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.ToString());
            }
        }

        /// <summary>
        /// Returns stored logs for inspection.
        /// </summary>
        /// <param name="skip">Number of entries to skip before returning results.</param>
        /// <param name="take">Maximum number of entries to include.</param>
        [HttpGet]
        [Route(HttpRouteConstants.Logs + "/")]
        public ContentResult OnGetLogs(
            int skip = 0,
            int take = 1000)
        {
            var logs = this._observer.GetLogs(skip, take);

            return Content(logs);
        }

        /// <summary>
        /// Returns stored logs matching the supplied filter string.
        /// </summary>
        /// <param name="filter">Substring used to filter log output.</param>
        /// <param name="skip">Number of entries to skip before returning results.</param>
        /// <param name="take">Maximum number of entries to include.</param>
        [HttpGet]
        [Route(HttpRouteConstants.Logs + "/{filter}")]
        public ContentResult OnGetLogs(
            string filter,
            int skip = 0,
            int take = 1000)
        {
            var logs = this._observer.GetLogs(filter, skip, take);

            return Content(logs);
        }
    }
}
