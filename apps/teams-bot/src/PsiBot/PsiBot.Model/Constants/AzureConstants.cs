namespace PsiBot.Model.Constants
{
    /// <summary>
    /// Shared constants for Azure-hosted integration points.
    /// </summary>
    public static class AzureConstants
    {
        /// <summary>
        /// OpenID configuration endpoint used to validate Skype authentication certificates.
        /// </summary>
        public const string AuthDomain = "https://api.aps.skype.com/v1/.well-known/OpenIdConfiguration";
    }
}
