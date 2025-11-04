using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Skype.Bots.Media;
using PsiBot.Model.Models;
using PsiBot.Services.Authentication;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using PsiBot.Service.Settings;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using PsiBot.Model.Constants;
using System.Linq;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Orchestrates Graph call lifecycle management and surfaces active call coordinators.
    /// </summary>
    public class TeamsCallLifecycleService : IDisposable, ITeamsCallLifecycleService
    {
        private readonly IGraphLogger logger;

        private readonly BotConfiguration botConfiguration;

        /// <summary>
        /// Active call coordinators keyed by call leg identifier.
        /// </summary>
        public ConcurrentDictionary<string, TeamsCallSessionCoordinator> ActiveCallCoordinators { get; } = new ConcurrentDictionary<string, TeamsCallSessionCoordinator>();

        /// <summary>
        /// Microsoft Graph communications client instance used for call control.
        /// </summary>
        public ICommunicationsClient Client { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Client?.Dispose();
        }

        /// <summary>
        /// Creates a new bot service configured with logging and application settings.
        /// </summary>
        /// <param name="logger">Structured logger for diagnostic output.</param>
        /// <param name="botConfiguration">Bot configuration options.</param>
        public TeamsCallLifecycleService(
            IGraphLogger graphLogger,
            IOptions<BotConfiguration> botConfiguration

        )
        {
            this.logger = graphLogger;
            this.botConfiguration = botConfiguration.Value;
        }

        /// <summary>
        /// Bootstraps the communications client and hooks call events.
        /// </summary>
        public void Initialize()
        {
            var name = this.GetType().Assembly.GetName().Name;
            var builder = new CommunicationsClientBuilder(
                name,
                this.botConfiguration.AadAppId,
                this.logger);

            var authProvider = new AuthenticationProvider(
                name,
                this.botConfiguration.AadAppId,
                this.botConfiguration.AadAppSecret,
                this.logger);

            builder.SetAuthenticationProvider(authProvider);
            builder.SetNotificationUrl(this.botConfiguration.CallControlBaseUrl);
            builder.SetMediaPlatformSettings(this.botConfiguration.MediaPlatformSettings);
            builder.SetServiceBaseUrl(this.botConfiguration.PlaceCallEndpointUrl);

            this.Client = builder.Build();
            this.Client.Calls().OnIncoming += this.HandleIncomingCalls;
            this.Client.Calls().OnUpdated += this.HandleUpdatedCalls;
        }

        /// <summary>
        /// Ends a call by its leg identifier, removing any lingering SDK state on failure.
        /// </summary>
        /// <param name="callLegId">Identifier of the call leg to terminate.</param>
        public async Task EndCallByCallLegIdAsync(string callLegId)
        {
            try
            {
                await this.GetCallCoordinatorOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                this.Client.Calls().TryForceRemove(callLegId, out ICall _);
            }
        }

        /// <summary>
        /// Joins a Teams meeting using the supplied join URL payload and returns the resulting call.
        /// </summary>
        /// <param name="joinCallBody">Payload containing join URL and optional display name.</param>
        public async Task<ICall> JoinCallAsync(JoinCallBody joinCallBody)
        {
            // A tracking id for logging purposes. Helps identify this call in logs.
            var scenarioId = Guid.NewGuid();

            var (chatInfo, meetingInfo) = this.ParseJoinUrl(joinCallBody.JoinURL);

            var tenantId = (meetingInfo as OrganizerMeetingInfo).Organizer.GetPrimaryIdentity().GetTenantId();
            var mediaSession = this.CreateLocalMediaSession();

            var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession)
            {
                TenantId = tenantId,
            };

            if (!string.IsNullOrWhiteSpace(joinCallBody.DisplayName))
            {
                // Teams client does not allow changing of ones own display name.
                // If display name is specified, we join as anonymous (guest) user
                // with the specified display name.  This will put bot into lobby
                // unless lobby bypass is disabled.
                joinParams.GuestIdentity = new Identity
                {
                    Id = Guid.NewGuid().ToString(),
                    DisplayName = joinCallBody.DisplayName,
                };
            }

            var statefulCall = await this.Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);
            statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");
            return statefulCall;
        }

        /// <summary>
        /// Creates the local media session.
        /// </summary>
        /// <param name="mediaSessionId">The media session identifier.
        /// This should be a unique value for each call.</param>
        /// <returns>The <see cref="ILocalMediaSession" />.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default)
        {
            try
            {
                var videoSocketSettings = new List<VideoSocketSettings>
                {
                    // add the main video socket sendrecv capable
                    new VideoSocketSettings
                    {
                        StreamDirections = StreamDirection.Sendrecv,

                        // We loop back the video in this sample. The MediaPlatform always sends only NV12 frames.
                        // So include only NV12 video in supportedSendVideoFormats
                        ReceiveColorFormat = VideoColorFormat.NV12,
                        SupportedSendVideoFormats = TeamsMediaStreamRouter.VideoFormatMap.Values.OfType<VideoFormat>().ToList(),
                        MaxConcurrentSendStreams = 1,
                    },
                };

                // create the receive only sockets settings for the multiview support
                for (int i = 0; i < BotConstants.NumberOfMultiviewSockets; i++)
                {
                    videoSocketSettings.Add(new VideoSocketSettings
                    {
                        StreamDirections = StreamDirection.Recvonly,
                        ReceiveColorFormat = VideoColorFormat.NV12,
                    });
                }

                // Create the VBSS socket settings
                var vbssSocketSettings = new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    ReceiveColorFormat = VideoColorFormat.NV12,
                    MediaType = MediaType.Vbss,
                    SupportedSendVideoFormats = new List<VideoFormat>
                    {
                        VideoFormat.NV12_1920x1080_15Fps,
                    },
                };

                // create media session object, this is needed to establish call connections
                return this.Client.CreateMediaSession(
                    new AudioSocketSettings
                    {
                        StreamDirections = StreamDirection.Sendrecv,
                        // Note! Currently, the only audio format supported when receiving unmixed audio is Pcm16K
                        SupportedAudioFormat = AudioFormat.Pcm16K,
                        ReceiveUnmixedMeetingAudio = true //get the extra buffers for the speakers
                    },
                    videoSocketSettings,
                    vbssSocketSettings,
                    mediaSessionId: mediaSessionId);
            }
            catch (Exception e)
            {
                this.logger.Log(System.Diagnostics.TraceLevel.Error, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Incoming call handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{TResource}" /> instance containing the event data.</param>
        private void HandleIncomingCalls(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                // The context associated with the incoming call.
                IncomingContext incomingContext =
                    call.Resource.IncomingContext;

                // The RP participant.
                string observedParticipantId =
                    incomingContext.ObservedParticipantId;

                // If the observed participant is a delegate.
                IdentitySet onBehalfOfIdentity =
                    incomingContext.OnBehalfOf;

                // If a transfer occured, the transferor.
                IdentitySet transferorIdentity =
                    incomingContext.Transferor;

                string countryCode = null;
                EndpointType? endpointType = null;

                // Note: this should always be true for CR calls.
                if (incomingContext.ObservedParticipantId == incomingContext.SourceParticipantId)
                {
                    // The dynamic location of the RP.
                    countryCode = call.Resource.Source.CountryCode;

                    // The type of endpoint being used.
                    endpointType = call.Resource.Source.EndpointType;
                }

                IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                    ? this.CreateLocalMediaSession(callId)
                    : this.CreateLocalMediaSession();

                // Answer call
                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(
                    call.GraphLogger,
                    $"Answering call {call.Id} with scenario {call.ScenarioId}.");
            });
        }

        /// <summary>
        /// Updated call handler.
        /// </summary>
        /// <param name="sender">The <see cref="ICallCollection" /> sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{ICall}" /> instance containing the event data.</param>
        private void HandleUpdatedCalls(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                var callCoordinator = new TeamsCallSessionCoordinator(call, this.botConfiguration);
                this.ActiveCallCoordinators[call.Id] = callCoordinator;
            }

            foreach (var call in args.RemovedResources)
            {
                if (this.ActiveCallCoordinators.TryRemove(call.Id, out TeamsCallSessionCoordinator coordinator))
                {
                    coordinator.Dispose();
                }
            }
        }

        /// <summary>
        /// The get handler or throw.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="TeamsCallSessionCoordinator" />.</returns>
        /// <exception cref="ArgumentException">call ({callLegId}) not found</exception>
        private TeamsCallSessionCoordinator GetCallCoordinatorOrThrow(string callLegId)
        {
            if (!this.ActiveCallCoordinators.TryGetValue(callLegId, out TeamsCallSessionCoordinator coordinator))
            {
                throw new ArgumentException($"call ({callLegId}) not found");
            }

            return coordinator;
        }

        /// <summary>
        /// Parse Join URL into its components.
        /// </summary>
        /// <param name="joinUrl">Join URL from Team's meeting body.</param>
        /// <returns>Parsed data.</returns>
        /// <exception cref="ArgumentException">Join URL cannot be null or empty: {joinUrl} - joinUrl</exception>
        /// <exception cref="ArgumentException">Join URL cannot be parsed: {joinUrl} - joinUrl</exception>
        /// <exception cref="ArgumentException">Join URL is invalid: missing Tid - joinUrl</exception>
        private (ChatInfo, MeetingInfo) ParseJoinUrl(string joinUrl)
        {
            if (string.IsNullOrEmpty(joinUrl))
            {
                throw new ArgumentException($"Join URL cannot be null or empty: {joinUrl}", nameof(joinUrl));
            }

            var decodedURL = WebUtility.UrlDecode(joinUrl);

            //// URL being needs to be in this format.
            //// https://teams.microsoft.com/l/meetup-join/19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype/1509579179399?context={"Tid":"72f988bf-86f1-41af-91ab-2d7cd011db47","Oid":"550fae72-d251-43ec-868c-373732c2704f","MessageId":"1536978844957"}

            var regex = new Regex("https://teams\\.microsoft\\.com.*/(?<thread>[^/]+)/(?<message>[^/]+)\\?context=(?<context>{.*})");
            var match = regex.Match(decodedURL);
            if (!match.Success)
            {
                throw new ArgumentException($"Join URL cannot be parsed: {joinUrl}", nameof(joinUrl));
            }

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(match.Groups["context"].Value)))
            {
                var ctxt = (Meeting)new DataContractJsonSerializer(typeof(Meeting)).ReadObject(stream);

                if (string.IsNullOrEmpty(ctxt.Tid))
                {
                    throw new ArgumentException("Join URL is invalid: missing Tid", nameof(joinUrl));
                }

                var chatInfo = new ChatInfo
                {
                    ThreadId = match.Groups["thread"].Value,
                    MessageId = match.Groups["message"].Value,
                    ReplyChainMessageId = ctxt.MessageId,
                };

                var meetingInfo = new OrganizerMeetingInfo
                {
                    Organizer = new IdentitySet
                    {
                        User = new Identity { Id = ctxt.Oid },
                    },
                };
                meetingInfo.Organizer.User.SetTenantId(ctxt.Tid);

                return (chatInfo, meetingInfo);
            }
        }
    }
}
