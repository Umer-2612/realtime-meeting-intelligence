namespace PsiBot.Model.Models
{
    /// <summary>
    /// Represents the payload supplied by the client when the bot joins a meeting.
    /// </summary>
    public class JoinCallBody
    {
        /// <summary>
        /// Teams meeting join URL used to resolve chat and meeting metadata.
        /// </summary>
        /// <value>The join URL.</value>
        public string JoinURL { get; set; }

        /// <summary>
        /// Optional guest display name; when provided, the bot joins as an anonymous participant.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName { get; set; }
    }
}
