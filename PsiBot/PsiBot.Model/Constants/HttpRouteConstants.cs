namespace PsiBot.Model.Constants
{
    /// <summary>
    /// Defines API route segments used by bot call signaling endpoints.
    /// </summary>
    public static class HttpRouteConstants
    {
        /// <summary>
        /// Route prefix applied to all call signaling endpoints.
        /// </summary>
        public const string CallSignalingRoutePrefix = "api/calling";

        /// <summary>
        /// Route for initial call signaling requests.
        /// </summary>
        public const string OnIncomingRequestRoute = "";

        /// <summary>
        /// Route for notifications emitted by the Graph calling platform.
        /// </summary>
        public const string OnNotificationRequestRoute = "notification";

        /// <summary>
        /// Route for retrieving in-memory diagnostic logs.
        /// </summary>
        public const string Logs = "logs";

        /// <summary>
        /// Route for retrieving or creating call resources.
        /// </summary>
        public const string Calls = "calls";

        /// <summary>
        /// Route for initiating a new call join request.
        /// </summary>
        public const string JoinCall = "joinCall";

        /// <summary>
        /// Route for addressing a specific call record using its leg identifier.
        /// </summary>
        public const string CallRoute = Calls + "/{callLegId}";

        /// <summary>
        /// Route that serves the management page for manual testing.
        /// </summary>
        public const string Management = "manage";
    }
}
