using Microsoft.Graph.Communications.Calls;
using System.Collections.Generic;

namespace PsiBot.Model.Models
{
    /// <summary>
    /// Captures participant change details raised from Graph call events.
    /// </summary>
    public class ParticipantData
    {
        /// <summary>
        /// Collection of participants added during the event.
        /// </summary>
        /// <value>The added resources.</value>
        public ICollection<IParticipant> AddedResources { get; set; }
        /// <summary>
        /// Collection of participants removed during the event.
        /// </summary>
        /// <value>The removed resources.</value>
        public ICollection<IParticipant> RemovedResources { get; set; }
    }
}
