# Realtime Meeting Monorepo

![High level architecture](architecture.png)

This repository packages a cross-platform meeting intelligence stack that spans:

- **Microsoft Teams meeting bot** running on Windows nodes (.NET Framework).
- **Zoom meeting automation** compiled from the Zoom Meeting SDK (C++).
- **Streaming analytics services** that handle real-time audio and video enrichment.
- **Shared infrastructure** deployed to AWS EKS with coordinated Windows and Linux workloads.

The repo is structured to support a Kubernetes-first deployment while keeping local development ergonomic.

## Solution Topology

![Domain model](app_services_domain.png)

At a glance:

- The **Teams bot** uses `TeamsCallLifecycleService`, `TeamsCallSessionCoordinator`, and `TeamsMediaStreamRouter` to manage call lifetimes, participant media subscriptions, and PSI pipelines.
- The **Zoom bot** centres on `MeetingSdkDemo`, refreshed to use `ZoomSdkRenderer`, `ZoomSdkAudioRawData`, `ZoomSdkVideoSource`, and `ZoomSdkVirtualAudioMicEvent` for raw media capture/publish workflows.
- The **stream processor** (Python) consumes fan-out data for analytics and feeds back decisions via REST/webhook surfaces.

## Repository Layout

```
apps/
  teams-bot/         # Windows-hosted .NET bot (Teams Graph SDK + Microsoft.Psi)
  zoom-bot/          # C++ Zoom Meeting SDK demo with raw media hooks
  common/            # Reserved for future shared assets/schemas
services/
  stream-processor/  # Python-based analytics workers
infrastructure/
  k8s/               # Base and overlay manifests for AWS EKS
scripts/             # CI/CD and local helper scripts
docs/                # Reference docs, run-books, and ADRs
```

## Developer Workflow

![Local development workflow](local_dev.png)

### Teams Bot (Windows)

1. Open `apps/teams-bot/PsiBot.sln` in Visual Studio 2022.
2. Restore NuGet packages and ensure the `TeamsCallLifecycleService` singleton initializes at startup (startup wiring already configured in `Startup.cs`).
3. Use ngrok or Azure Relay to expose the local bot endpoint when testing incoming calls (the tunnelling diagram below applies to both bots).

Key runtime components:

- `TeamsCallLifecycleService` – orchestrates call creation, teardown, and DI exposure.
- `TeamsCallSessionCoordinator` – per-call coordinator managing heartbeats and participant video subscriptions.
- `TeamsMediaStreamRouter` / `ParticipantMediaSource` – bridge Microsoft Graph media sockets with Microsoft.Psi pipelines.

### Zoom Bot (Linux/Windows)

![Secure tunnelling options](ngrok.png)

1. Install Zoom Meeting SDK dependencies and CMake toolchain.
2. Configure `apps/zoom-bot/src/demo/config.txt` using the new keys:
   - `meetingNumber`, `meetingPassword`, `token`
   - `enableVideoRawDataCapture`, `enableAudioRawDataCapture`
   - `enableVideoRawDataPublishing`, `enableAudioRawDataPublishing`
3. Build the demo:
   ```bash
   cd apps/zoom-bot/src/demo
   mkdir -p build && cd build
   cmake ..
   cmake --build .
   ```
4. Launch `MeetingSdkDemo` and monitor stdout for raw media subscription or publishing events.

### Streaming Services

Refer to `services/stream-processor/README.md` for Python environment setup and run commands. The processor expects JSON descriptors produced by the Teams/Zoom bots when fan-out is enabled.

## Build & Deployment

![CI/CD pipeline](pipeline.png)

1. **Container builds**
   - `apps/teams-bot/docker/Dockerfile.windows` (Windows container, requires Windows builder)
   - `apps/zoom-bot/docker/Dockerfile` (Ubuntu-based Zoom SDK build)
   - `services/stream-processor/Dockerfile`
2. **Push images** to Amazon ECR (or your registry of choice).
3. **Deploy** via Kustomize overlays:
   ```bash
   kubectl apply -k infrastructure/k8s/overlays/dev
   ```
4. **Observe** call routing with CloudWatch logs or Application Insights (depending on your telemetry wiring).

## Configuration Highlights

- Teams bot app settings (under `apps/teams-bot/src/PsiBot/PsiBot.Service/appsettings.json`) should provide AAD credentials, media platform settings, and PSI export directories. The service now relies on the renamed classes noted above.
- Zoom config uses the new camelCase flags (see `config.txt` example) to enable/disable raw capture and publishing flows.
- `.gitignore` has been expanded to cover nested `bin/`, `obj/`, TestResults, and Zoom SDK build artefacts—run `git status` before commits to ensure only source changes are staged.

## CI/CD Guidance

- Use split runners (Windows + Linux) in GitHub Actions or AWS CodeBuild to build respective images.
- Leverage scripts under `scripts/` for repeatable packaging and deployment tasks.
- Incorporate automated tests for each bot and add end-to-end smoke tests against a staging EKS cluster.

## Next Steps

- Populate `apps/common` with shared contracts, schemas, and reusable processing primitives.
- Add managed secrets integration (AWS Secrets Manager, Parameter Store, or HashiCorp Vault).
- Extend analytics pipelines with additional PSI components and ML models.
- Formalise monitoring dashboards for both Teams and Zoom call flows.
