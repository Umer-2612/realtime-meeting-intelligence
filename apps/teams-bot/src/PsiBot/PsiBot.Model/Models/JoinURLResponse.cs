using Newtonsoft.Json;
using System;

namespace PsiBot.Model.Models
{
    /// <summary>
    /// Represents the server response returned after the bot attempts to join a meeting.
    /// </summary>
    public partial class JoinURLResponse
    {
        /// <summary>
        /// Identifier of the created call resource.
        /// </summary>
        /// <value>The call identifier.</value>
        [JsonProperty("callId")]
        public object CallId { get; set; }

        /// <summary>
        /// Operation-level correlation identifier useful for diagnostics.
        /// </summary>
        /// <value>The scenario identifier.</value>
        [JsonProperty("scenarioId")]
        public Guid ScenarioId { get; set; }

        /// <summary>
        /// Serialized call payload returned by Graph.
        /// </summary>
        /// <value>The call.</value>
        [JsonProperty("call")]
        public string Call { get; set; }

        /// <summary>
        /// Diagnostic log output produced during join processing.
        /// </summary>
        [JsonProperty("logs")]
        public string Logs { get; set; }
    }
}
