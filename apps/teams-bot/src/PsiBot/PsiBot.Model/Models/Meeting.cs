using System.Runtime.Serialization;

namespace PsiBot.Model.Models
{
    /// <summary>
    /// Holds tenant and message identifiers referenced by a Teams meeting join link.
    /// </summary>
    [DataContract]
    public class Meeting
    {
        /// <summary>
        /// Azure AD tenant identifier associated with the meeting.
        /// </summary>
        /// <value>The tid.</value>
        [DataMember]
        public string Tid { get; set; }

        /// <summary>
        /// Azure AD object identifier for the organizer or participant.
        /// </summary>
        /// <value>The oid.</value>
        [DataMember]
        public string Oid { get; set; }

        /// <summary>
        /// Chat message identifier where the meeting invitation originated.
        /// </summary>
        /// <value>The message identifier.</value>
        [DataMember]
        public string MessageId { get; set; }
    }
}
