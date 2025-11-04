using PsiBot.Model.Constants;
using PsiBot.Model.Models;
using PsiBot.Service.Settings;
using PsiBot.Services.Bot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PsiBot.Services.Controllers
{
    /// <summary>
    /// Handles join requests issued by external callers in cloud video interop scenarios.
    /// </summary>
    public class JoinCallController : ControllerBase
    {
        private readonly IGraphLogger _logger;

        private readonly IBotService _botService;

        private readonly BotConfiguration botConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinCallController" /> class.
        /// </summary>
        public JoinCallController(IBotService botService, IOptions<BotConfiguration> botConfiguration, IGraphLogger logger)
        {
            _logger = logger;
            _botService = botService;
            this.botConfiguration = botConfiguration.Value;
        }

        /// <summary>
        /// Joins a meeting using the supplied join payload and returns call metadata.
        /// </summary>
        /// <param name="joinCallBody">Join payload received from an external caller.</param>
        /// <returns>Success result containing call metadata when the join is successful.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.JoinCall)]
        public async Task<IActionResult> JoinCallAsync([FromBody] JoinCallBody joinCallBody)
        {
            try
            {
                var call = await _botService.JoinCallAsync(joinCallBody).ConfigureAwait(false);
                var callPath = $"/{HttpRouteConstants.CallRoute.Replace("{callLegId}", call.Id)}";
                var callUri = $"{botConfiguration.ServiceCname}{callPath}";

                var values = new JoinURLResponse()
                {
                    Call = callUri,
                    CallId = call.Id,
                    ScenarioId = call.ScenarioId,
                    Logs = callUri.Replace("/calls/", "/logs/")
                };

                return Ok(values);
            }
            catch (ServiceException e)
            {
                HttpResponseMessage response = (int)e.StatusCode >= 300
                    ? new HttpResponseMessage(e.StatusCode)
                    : new HttpResponseMessage(HttpStatusCode.InternalServerError);

                if (e.ResponseHeaders != null)
                {
                    foreach (var responseHeader in e.ResponseHeaders)
                    {
                        response.Headers.TryAddWithoutValidation(responseHeader.Key, responseHeader.Value);
                    }
                }

                response.Content = new StringContent(e.ToString());
                return StatusCode(500, e.ToString());
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Received HTTP {this.Request.Method}, {this.Request.Path.Value}");
                return StatusCode(500, e.Message);
            }
        }
    }
}
