using PsiBot.Model.Models;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Abstraction over bot operations required by the web controllers and hosting infrastructure.
    /// </summary>
    public interface IBotService
    {
        /// <summary>
        /// Active call handlers keyed by call leg identifier.
        /// </summary>
        ConcurrentDictionary<string, CallHandler> CallHandlers { get; }

        /// <summary>
        /// Communications client that manages call state for the bot.
        /// </summary>
        ICommunicationsClient Client { get; }

        /// <summary>
        /// Ends a call by its call leg identifier.
        /// </summary>
        /// <param name="callLegId">Call leg identifier.</param>
        /// <returns>Task tracking the asynchronous operation.</returns>
        Task EndCallByCallLegIdAsync(string callLegId);

        /// <summary>
        /// Joins a call using the provided payload.
        /// </summary>
        /// <param name="joinCallBody">Join request payload.</param>
        /// <returns>The created call.</returns>
        Task<ICall> JoinCallAsync(JoinCallBody joinCallBody);

        /// <summary>
        /// Initializes the bot service and associated clients.
        /// </summary>
        void Initialize();
    }
}
