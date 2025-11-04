using Microsoft.Graph;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Provides helpers for exception logging and HTTP response generation.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Executes a task in the background while logging any raised exception.
        /// </summary>
        /// <param name="task">Task to execute and capture any exceptions.</param>
        /// <param name="logger">Graph logger.</param>
        /// <param name="description">Friendly description of the task for debugging purposes.</param>
        /// <param name="memberName">Calling function.</param>
        /// <param name="filePath">File name where code is located.</param>
        /// <param name="lineNumber">Line number where code is located.</param>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        public static async Task ForgetAndLogExceptionAsync(
            this Task task,
            IGraphLogger logger,
            string description = null,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                await task.ConfigureAwait(false);
                logger?.Verbose(
                    $"Completed running task successfully: {description ?? string.Empty}",
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);
            }
            catch (Exception e)
            {
                logger?.Error(
                    e,
                    $"Caught an Exception running the task: {description ?? string.Empty}",
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);
            }
        }
        /// <summary>
        /// Builds an <see cref="HttpResponseMessage"/> representing the exception.
        /// </summary>
        /// <param name="exception">The caught exception.</param>
        /// <returns>HTTP response representing the failure.</returns>
        public static HttpResponseMessage InspectExceptionAndReturnResponse(this Exception exception)
        {
            HttpResponseMessage responseToReturn;
            if (exception is ServiceException e)
            {
                responseToReturn = (int)e.StatusCode >= 200
                    ? new HttpResponseMessage(e.StatusCode)
                    : new HttpResponseMessage(HttpStatusCode.InternalServerError);
                if (e.ResponseHeaders != null)
                {
                    foreach (var responseHeader in e.ResponseHeaders)
                    {
                        responseToReturn.Headers.TryAddWithoutValidation(responseHeader.Key, responseHeader.Value);
                    }
                }

                responseToReturn.Content = new StringContent(e.ToString());
            }
            else
            {
                responseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(exception.ToString()),
                };
            }

            return responseToReturn;
        }
    }
}
