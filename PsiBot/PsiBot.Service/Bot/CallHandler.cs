using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using PsiBot.Model.Constants;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using PsiBot.Service.Settings;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.Skype.Bots.Media;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.TeamsBot;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Coordinates call lifecycle and manages media subscriptions for a single call instance.
    /// </summary>
    public class CallHandler : HeartbeatHandler
    {
        /// <summary>
        /// Gets the call.
        /// </summary>
        /// <value>The call.</value>
        public ICall Call { get; }

        /// <summary>
        /// Gets the bot media stream.
        /// </summary>
        /// <value>The bot media stream.</value>
        public BotMediaStream BotMediaStream { get; private set; }

        /// <summary>
        /// MSI when there is no dominant speaker.
        /// </summary>
        public const uint DominantSpeakerNone = DominantSpeakerChangedEventArgs.None;

        private readonly BotConfiguration botConfiguration;

        /// <summary>
        /// Video socket identifiers available for allocation.
        /// </summary>
        private readonly HashSet<uint> availableSocketIds = new HashSet<uint>();

        /// <summary>
        /// Least-recently-used cache of media stream identifiers used to prioritize active speakers.
        /// </summary>
        private readonly LRUCache currentVideoSubscriptions = new LRUCache(BotConstants.NumberOfMultiviewSockets + 1);

        private readonly object subscriptionLock = new object();

        /// <summary>
        /// Mapping between media stream identifiers and the sockets they consume.
        /// </summary>
        private readonly ConcurrentDictionary<uint, uint> msiToSocketIdMapping = new ConcurrentDictionary<uint, uint>();

        private readonly Pipeline pipeline;
        private readonly ITeamsBot teamsBot;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="statefulCall">The stateful call.</param>
        /// <param name="botConfiguration">The bot configuration.</param>
        public CallHandler(ICall statefulCall, BotConfiguration botConfiguration)
            : base(TimeSpan.FromMinutes(10), statefulCall?.GraphLogger)
        {
            this.botConfiguration = botConfiguration;

            this.pipeline = Pipeline.Create(enableDiagnostics: true);
            this.teamsBot = CreateTeamsBot(this.pipeline);
            PsiExporter exporter = null;

            if (!string.IsNullOrEmpty(botConfiguration.PsiStoreDirectory))
            {
                exporter = PsiStore.Create(this.pipeline, $"CallStore_{statefulCall.Id}", botConfiguration.PsiStoreDirectory);
                this.pipeline.Diagnostics.Write("Diagnostics", exporter);
            }

            this.Call = statefulCall;
            this.Call.OnUpdated += this.CallOnUpdated;

            // Track participant membership changes to maintain socket subscriptions.
            this.Call.Participants.OnUpdated += this.ParticipantsOnUpdated;

            // Subscribe to dominant speaker events to drive video switching.
            this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged += this.OnDominantSpeakerChanged;

            foreach (var videoSocket in this.Call.GetLocalMediaSession().VideoSockets)
            {
                this.availableSocketIds.Add((uint)videoSocket.SocketId);
            }

            var waitingToShare = true;
            this.Call.OnUpdated += (call, args) =>
            {
                if (waitingToShare && call.Resource.State == CallState.Established && this.teamsBot.EnableScreenSharing)
                {
                    // Enable screen sharing once the call is established.
                    this.Call.ChangeScreenSharingRoleAsync(ScreenSharingRole.Sharer).Wait();
                    waitingToShare = false;
                }
            };

            // Instantiate the bot media stream that drives media fan-out and capture.
            this.BotMediaStream = new BotMediaStream(this.Call.GetLocalMediaSession(), this, pipeline, teamsBot, exporter, this.GraphLogger,  this.botConfiguration);

            this.pipeline.PipelineExceptionNotHandled += (_, ex) =>
            {
                this.GraphLogger.Error($"PSI PIPELINE ERROR: {ex.Exception.Message}");
            };
            this.pipeline.RunAsync();
        }

        /// <summary>
        /// Factory method for creating the <see cref="ITeamsBot"/> component.
        /// </summary>
        /// <param name="pipeline">PSI pipeline instance used for processing.</param>
        /// <returns>ITeamsBot instance.</returns>
        private static ITeamsBot CreateTeamsBot(Pipeline pipeline)
        {
            return new ParticipantEngagementScaleBot(pipeline, TimeSpan.FromSeconds(1.0 / 15.0), 1920, 1080, true);
        }

        /// <inheritdoc/>
        protected override Task HeartbeatAsync(ElapsedEventArgs args)
        {
            return this.Call.KeepAliveAsync();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.pipeline.Dispose();
            this.Call.OnUpdated -= this.CallOnUpdated;
            this.Call.Participants.OnUpdated -= this.ParticipantsOnUpdated;

            foreach (var participant in this.Call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            this.BotMediaStream?.Dispose();
        }

        /// <summary>
        /// Event fired when the call has been updated.
        /// </summary>
        /// <param name="sender">The call.</param>
        /// <param name="e">The event args containing call changes.</param>
        private void CallOnUpdated(ICall sender, ResourceEventArgs<Call> e)
        {
            this.GraphLogger.Info($"Call status updated to {e.NewResource.State} - {e.NewResource.ResultInfo?.Message}");
            // Event - Recording update e.g established/updated/start/ended

            if (e.OldResource.State != e.NewResource.State && e.NewResource.State == CallState.Established)
            {
            }
        }

        /// <summary>
        /// Creates the participant update json.
        /// </summary>
        /// <param name="participantId">The participant identifier.</param>
        /// <param name="participantDisplayName">Display name of the participant.</param>
        /// <returns>Serialized payload.</returns>
        private string CreateParticipantUpdateJson(string participantId, string participantDisplayName = "")
        {
            if (participantDisplayName.Length == 0)
            {
                return "{" + string.Format("\"Id\": \"{0}\"", participantId) + "}";
            }

            return "{" + string.Format("\"Id\": \"{0}\", \"DisplayName\": \"{1}\"", participantId, participantDisplayName) + "}";
        }

        /// <summary>
        /// Updates the participant collection with the supplied participant state change.
        /// </summary>
        /// <param name="participants">The participants.</param>
        /// <param name="participant">The participant.</param>
        /// <param name="added">if set to <c>true</c> [added].</param>
        /// <param name="participantDisplayName">Display name of the participant.</param>
        /// <returns>Serialized payload describing the participant change.</returns>
        private string UpdateParticipant(List<IParticipant> participants, IParticipant participant, bool added, string participantDisplayName = "")
        {
            if (added)
            {
                participants.Add(participant);
                participant.OnUpdated += this.OnParticipantUpdated;
                this.SubscribeToParticipantVideo(participant, forceSubscribe: false);
            }
            else
            {
                participants.Remove(participant);
                participant.OnUpdated -= this.OnParticipantUpdated;
                this.UnsubscribeFromParticipantVideo(participant);
            }

            return CreateParticipantUpdateJson(participant.Id, participantDisplayName);
        }

        /// <summary>
        /// Syncs the internal participant list with additions or removals raised by the SDK.
        /// </summary>
        /// <param name="eventArgs">Participants to reconcile.</param>
        /// <param name="added">Indicates whether the participants were added or removed.</param>
        private void UpdateParticipants(ICollection<IParticipant> eventArgs, bool added = true)
        {
            foreach (var participant in eventArgs)
            {
                var participantDetails = participant.Resource.Info.Identity.User;

                if (participantDetails != null)
                {
                    UpdateParticipant(this.BotMediaStream.participants, participant, added, participantDetails.DisplayName);
                }
                else if (participant.Resource.Info.Identity.AdditionalData?.Count > 0)
                {
                    if (CheckParticipantIsUsable(participant))
                    {
                        UpdateParticipant(this.BotMediaStream.participants, participant, added);
                    }
                }
            }
        }

        /// <summary>
        /// Event fired when the participants collection has been updated.
        /// </summary>
        /// <param name="sender">Participants collection.</param>
        /// <param name="args">Event args containing added and removed participants.</param>
        public void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            UpdateParticipants(args.AddedResources);
            UpdateParticipants(args.RemovedResources, false);
        }

        /// <summary>
        /// Event fired when a participant is updated.
        /// </summary>
        /// <param name="sender">Participant object.</param>
        /// <param name="args">Event args containing the old values and the new values.</param>
        private void OnParticipantUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
        {
        }

        /// <summary>
        /// Listen for dominant speaker changes in the conference.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The dominant speaker changed event arguments.
        /// </param>
        private void OnDominantSpeakerChanged(object sender, DominantSpeakerChangedEventArgs e)
        {
            this.GraphLogger.Info($"[{this.Call.Id}:OnDominantSpeakerChanged(DominantSpeaker={e.CurrentDominantSpeaker})]");
            if (e.CurrentDominantSpeaker != DominantSpeakerNone)
            {
                var participant = GetParticipantFromMSI(this.Call, e.CurrentDominantSpeaker);
                var participantDetails = TryGetParticipantIdentity(participant);
                if (participantDetails != null)
                {
                    this.SubscribeToParticipantVideo(participant, forceSubscribe: true);
                }
            }
        }

        /// <summary>
        /// Gets the participant with the corresponding MSI.
        /// </summary>
        /// <param name="msi">media stream id.</param>
        /// <returns>
        /// The <see cref="IParticipant"/>.
        /// </returns>
        public static IParticipant GetParticipantFromMSI(ICall call, uint msi)
        {
            return call.Participants.SingleOrDefault(x => x.Resource.IsInLobby == false && x.Resource.MediaStreams.Any(y => y.SourceId == msi.ToString()));
        }

        /// <summary>
        /// Tries to get the identity information of the given participant.
        /// </summary>
        /// <param name="participant">The participant we wish to get an identity from.</param>
        /// <returns>The participant's identity info (or null if not found).</returns>
        public static Identity TryGetParticipantIdentity(IParticipant participant)
        {
            var identitySet = participant?.Resource?.Info?.Identity;
            var identity = identitySet?.User;

            if (identity == null &&
                identitySet != null &&
                identitySet.AdditionalData.Any(kvp => kvp.Value is Microsoft.Graph.Identity))
            {
                identity = identitySet.AdditionalData.Values.First(v => v is Microsoft.Graph.Identity) as Microsoft.Graph.Identity;
            }

            return identity;
        }

        /// <summary>
        /// Unsubscribe and free up the video socket for the specified participant.
        /// </summary>
        /// <param name="participant">Particant to unsubscribe the video.</param>
        private void UnsubscribeFromParticipantVideo(IParticipant participant)
        {
            var participantSendCapableVideoStream = participant.Resource.MediaStreams.Where(x => x.MediaType == Modality.Video &&
              (x.Direction == MediaDirection.SendReceive || x.Direction == MediaDirection.SendOnly)).FirstOrDefault();

            if (participantSendCapableVideoStream != null)
            {
                var msi = uint.Parse(participantSendCapableVideoStream.SourceId);
                lock (this.subscriptionLock)
                {
                    if (this.currentVideoSubscriptions.TryRemove(msi))
                    {
                        if (this.msiToSocketIdMapping.TryRemove(msi, out uint socketId))
                        {
                            this.BotMediaStream.Unsubscribe(MediaType.Video, socketId);
                            this.availableSocketIds.Add(socketId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the participant represents a usable media source.
        /// </summary>
        /// <param name="p">Participant to evaluate.</param>
        /// <returns>True when the participant exposes a non-application identity.</returns>
        private bool CheckParticipantIsUsable(IParticipant p)
        {
            foreach (var i in p.Resource.Info.Identity.AdditionalData)
                if (i.Key != "applicationInstance" && i.Value is Identity)
                    return true;

            return false;
        }

        /// <summary>
        /// Subscribes to participant video or screen sharing streams, applying LRU eviction when sockets are exhausted.
        /// </summary>
        /// <param name="participant">Participant sending the video or VBSS stream.</param>
        /// <param name="forceSubscribe">If forced, the least recently used video socket is released if no sockets are available.</param>
        private void SubscribeToParticipantVideo(IParticipant participant, bool forceSubscribe = true)
        {
            bool subscribeToVideo = false;
            uint socketId = uint.MaxValue;

            var participantSendCapableVideoStream = participant.Resource.MediaStreams.Where(x => x.MediaType == Modality.Video &&
               (x.Direction == MediaDirection.SendReceive || x.Direction == MediaDirection.SendOnly)).FirstOrDefault();
            if (participantSendCapableVideoStream != null)
            {
                bool updateMSICache = false;
                var msi = uint.Parse(participantSendCapableVideoStream.SourceId);
                lock (this.subscriptionLock)
                {
                    if (this.currentVideoSubscriptions.Count < this.Call.GetLocalMediaSession().VideoSockets.Count)
                    {
                        if (!this.msiToSocketIdMapping.ContainsKey(msi))
                        {
                            if (this.availableSocketIds.Any())
                            {
                                socketId = this.availableSocketIds.Last();
                                this.availableSocketIds.Remove((uint)socketId);
                                subscribeToVideo = true;
                            }
                        }

                        updateMSICache = true;
                        this.GraphLogger.Info($"[{this.Call.Id}:SubscribeToParticipant(socket {socketId} available, the number of remaining sockets is {this.availableSocketIds.Count}, subscribing to the participant {participant.Id})");
                    }
                    else if (forceSubscribe)
                    {
                        updateMSICache = true;
                        subscribeToVideo = true;
                    }

                    if (updateMSICache)
                    {
                        this.currentVideoSubscriptions.TryInsert(msi, out uint? dequeuedMSIValue);
                        if (dequeuedMSIValue != null)
                        {
                            this.msiToSocketIdMapping.TryRemove((uint)dequeuedMSIValue, out socketId);
                        }
                    }
                }

                if (subscribeToVideo && socketId != uint.MaxValue)
                {
                    this.msiToSocketIdMapping.AddOrUpdate(msi, socketId, (k, v) => socketId);

                    this.GraphLogger.Info($"[{this.Call.Id}:SubscribeToParticipant(subscribing to the participant {participant.Id} on socket {socketId})");
                    this.BotMediaStream.Subscribe(MediaType.Video, msi, VideoResolution.HD1080p, socketId);
                }
            }

            var vbssParticipant = participant.Resource.MediaStreams.SingleOrDefault(x => x.MediaType == Modality.VideoBasedScreenSharing
            && x.Direction == MediaDirection.SendOnly);
            if (vbssParticipant != null)
            {
                this.GraphLogger.Info($"[{this.Call.Id}:SubscribeToParticipant(subscribing to the VBSS sharer {participant.Id})");
                this.BotMediaStream.Subscribe(MediaType.Vbss, uint.Parse(vbssParticipant.SourceId), VideoResolution.HD1080p, socketId);
            }
        }
    }
}
