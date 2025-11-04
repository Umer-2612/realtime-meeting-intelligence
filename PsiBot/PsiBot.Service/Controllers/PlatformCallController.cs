using PsiBot.Model.Constants;
using PsiBot.Services.Bot;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PsiBot.Services.Controllers
{
    /// <summary>
    /// Entry point for handling call-related web hook requests from Graph Communications.
    /// </summary>
    [Route(HttpRouteConstants.CallSignalingRoutePrefix)]
    public class PlatformCallController : ControllerBase
    {
        private readonly IBotService _botService;
        private readonly IGraphLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformCallController" /> class.
        /// </summary>
        public PlatformCallController(IBotService botService, IGraphLogger logger)
        {
            this._botService = botService;
            this._logger = logger;
        }

        /// <summary>
        /// Handles webhook notifications for new incoming calls.
        /// </summary>
        /// <returns>HTTP response payload returned to the platform.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingRequestRoute)]
        public async Task<IActionResult> OnIncomingRequestAsync()
        {
            var log = $"Received HTTP {this.Request.Method}, {this.Request.Path.Value}";
            _logger.Info(log);

            var response = await _botService.Client.ProcessNotificationAsync(ConvertHttpRequestToHttpRequestMessage(this.Request)).ConfigureAwait(false);

            var content = response.Content == null ? null : await response.Content?.ReadAsStringAsync();
            return Ok(content);
        }

        /// <summary>
        /// Handles webhook notifications for state changes on existing calls.
        /// </summary>
        /// <returns>HTTP response payload returned to the platform.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnNotificationRequestRoute)]
        public async Task<IActionResult> OnNotificationRequestAsync()
        {
            var log = $"Received HTTP {this.Request.Method}, {this.Request.Path}";
            _logger.Info(log);

            var response = await _botService.Client.ProcessNotificationAsync(ConvertHttpRequestToHttpRequestMessage(this.Request)).ConfigureAwait(false);

            var content = response.Content == null ? null : await response.Content?.ReadAsStringAsync();
            return Ok(content);
        }

        /// <summary>
        /// Converts the ASP.NET Core request into an <see cref="HttpRequestMessage"/> compatible with the SDK.
        /// </summary>
        /// <param name="request">Incoming HTTP request.</param>
        /// <returns>Converted request message.</returns>
        private HttpRequestMessage ConvertHttpRequestToHttpRequestMessage(HttpRequest request)
        {
            var uri = new Uri(request.GetDisplayUrl());
            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }
    }
}
