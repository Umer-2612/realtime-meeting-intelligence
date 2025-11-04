using Newtonsoft.Json;

namespace PsiBot.Model.Models
{
    /// <summary>
    /// Represents the payload posted back to the bot event pipeline.
    /// </summary>
    public class BotEventData
    {
        /// <summary>
        /// Human readable event message or status.
        /// </summary>
        /// <value>The message.</value>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}
