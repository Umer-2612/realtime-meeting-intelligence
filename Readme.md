# Microsoft Teams Meeting Bot

> Modernized fork maintained by Umer — replaces the legacy public sample documentation and reflects the current project shape.

## Overview

The Microsoft Teams Meeting Bot is a .NET solution that joins Teams meetings as a participant, processes live audio/video streams with [Microsoft Psi](https://github.com/microsoft/psi), and can publish bot-generated media back into the meeting. The codebase is organized for rapid experimentation with real-time audio‑visual intelligence and includes tooling for offline replay and diagnostics.

Core scenarios include:

- Joining Teams meetings programmatically and maintaining call state.
- Consuming participant audio, video, and screen-share feeds for downstream analysis.
- Producing bot-driven audio/video/screen-share output (for example, engagement visualizations).
- Persisting media streams to Psi stores for offline inspection in Psi Studio.

## Solution Structure

| Project | Description |
|---------|-------------|
| `PsiBot` | ASP.NET Core service that hosts the Teams calling bot and wires Psi pipelines into the Graph Communications SDK. |
| `TeamsBot` | Contracts for pluggable Psi bot components (for example, `ITeamsBot`). |
| `TeamsBotSample` | Sample Psi visualizations such as the engagement “ball bot” and thumbnail scaling bot. |
| `TeamsBotTester` | GTK-based desktop harness for replaying recorded meetings against a bot implementation offline. |

Supporting assets in `/architecture.png`, `/pipeline.png`, and `/local_dev.png` visualize the end-to-end topology.

## Prerequisites

- .NET SDK 6.0 or later
- Visual Studio 2022 or Visual Studio Code with C# Dev Kit (Windows) / VS Code + OmniSharp (macOS/Linux)
- An Azure AD app registration with Microsoft Graph Calling permissions:
  - `Calls.AccessMedia.All`
  - `Calls.Initiate.All`
  - `Calls.JoinGroupCall.All`
  - `Calls.JoinGroupCallAsGuest.All`
- A publicly reachable HTTPS endpoint for call control callbacks. During development you can use [ngrok](https://ngrok.com/) or Azure App Service.
- TLS certificate for the public endpoint (self-signed is acceptable for local dev).

## Quick Start

1. **Clone & Restore**
   ```bash
   git clone https://github.com/<your-org>/microsoft-teams-meeting-bot.git
   cd microsoft-teams-meeting-bot
   dotnet restore
   ```

2. **Configure `appsettings.Development.json`** (create alongside `PsiBot.Service/appsettings.json`):
   ```json
   {
     "BotConfiguration": {
       "BotName": "MyTeamsBot",
       "AadAppId": "<CLIENT_ID>",
       "AadAppSecret": "<CLIENT_SECRET>",
       "ServiceCname": "{ngrok-subdomain}.ngrok.io",
       "MediaServiceFQDN": "local.mydomain.com",
       "ServiceDnsName": "",
       "CertificateThumbprint": "<LOCAL_CERT_THUMBPRINT>",
       "InstancePublicPort": 9441,
       "CallSignalingPort": 9441,
       "InstanceInternalPort": 8445,
       "PlaceCallEndpointUrl": "https://graph.microsoft.com/v1.0",
       "PsiStoreDirectory": "C:/PsiStores" // optional
     }
   }
   ```

   - `ServiceCname` should match the public HTTPS endpoint (ngrok domain during development).
   - `MediaServiceFQDN` must resolve to the TCP endpoint that exposes your media port (for ngrok TCP forwarding, map a subdomain via your DNS provider).
   - `CertificateThumbprint` references the X509 certificate in the LocalMachine/My store used by Kestrel.

3. **Run the Bot Service**
   ```bash
   dotnet run --project PsiBot/PsiBot.Service
   ```

4. **Manage Calls**
   Visit `https://{ServiceCname}/manage` to use the built-in management page for joining/leaving meetings and inspecting active call logs.

5. **Join a Meeting**
   Paste a Teams meeting join URL on the management page. The bot will join as the configured application identity (or as a guest if a display name is supplied in the payload).

## Development Workflow

- **Hot Reload / Watch**: `dotnet watch --project PsiBot/PsiBot.Service` for faster iterations during backend changes.
- **Local Certificates**: Use the PowerShell `New-SelfSignedCertificate` cmdlet to mint development certs and export them for Kestrel. Update `BotConfiguration` with the thumbprint.
- **Psi Studio**: Open generated Psi stores (`*.psi`) in Psi Studio to inspect pipelines, audio, and video offline.
- **Testing Custom Bots**: Implement `ITeamsBot` in a new project or reuse `TeamsBotSample` as a template. Wire it into `CallHandler.CreateTeamsBot` or exercise it via `TeamsBotTester` for offline debugging.

## Troubleshooting

| Symptom | Resolution |
|---------|-----------|
| Bot immediately disconnects | Ensure the Azure AD application has admin consent for the calling permissions and that `AadAppSecret` is valid. |
| Media playback is blank | Confirm that the bot has at least one available video socket (`BotConstants.NumberOfMultiviewSockets`) and that the meeting participant is not in the lobby. |
| TLS startup failure | Verify the certificate exists in LocalMachine\My, has a private key, and that the process has permission to read it. Update `CertificateThumbprint` if the cert was reissued. |
| Ngrok traffic blocked | Use ngrok’s TCP forwarding for media (port 8445+) and HTTPS forwarding for call signaling. Restart ngrok when the public URL changes and update `ServiceCname`. |

## Roadmap Ideas

- Pluggable analytics (for example, sentiment, transcription, speaker attribution).
- Containerized deployment for Azure Kubernetes Service.
- Continuous Integration setup with automated smoke tests for call control endpoints.

## Acknowledgements

This project originated from Microsoft’s public \\psi Teams bot sample. The implementation retains key architectural components (Graph Communications SDK integration, Psi pipelines, engagement visualizations) while modernizing documentation and configuration to reflect current ownership.

Contributions and issue reports are welcome — open a pull request or file a GitHub issue with details about desired enhancements or bug reports.
