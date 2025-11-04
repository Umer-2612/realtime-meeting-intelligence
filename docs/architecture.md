# Architecture Overview

## High-Level Components

- **Teams Bot (`apps/teams-bot`)**  
  .NET Framework 4.8 application that integrates with Microsoft Graph Communications SDK and Microsoft Bot Framework. Runs inside a Windows container due to the Windows-only media SDK dependencies.

- **Zoom Bot (`apps/zoom-bot`)**  
  Native C++ implementation built from the Zoom Meeting SDK. The reference container targets Ubuntu and leverages CMake to compile and automate SDK packaging.

- **Stream Processor (`services/stream-processor`)**  
  Python microservice that ingests WebRTC or media artifacts emitted by both bots. Designed for async pipelines (AWS Kinesis/SQS) and GPU-enabled nodes when required.

## Deployment Topology

```
                          +-------------------------+
                          |     AWS Route 53 /     |
                          |     Application LB     |
                          +-----------+-------------+
                                      |
                        +-------------+------------------+
                        |                                 |
               +--------v-------+               +---------v--------+
               |  Linux Node    |               |  Windows Node    |
               |  group (EKS)   |               |  group (EKS)     |
               +--------+-------+               +---------+--------+
                        |                                 |
         +--------------v--------------+       +----------v-----------+
         | Zoom Bot Deployment        |       | Teams Bot Deployment |
         | (linux.tolerations/labels) |       | (windows.taints)      |
         +--------------+-------------+       +----------+-----------+
                        |                                 |
                        |                                 |
               +--------v----------------------+----------v-------------+
               |        Stream Processor Service (Linux)                |
               +--------------------------------------------------------+
```

- **Ingress**: AWS Load Balancer Controller or NGINX ingress routes traffic to appropriate services.
- **Messaging**: Use centralized queues/topics for coordinating media processing (e.g., SQS, Kafka).
- **Storage**: S3 buckets or EFS for persistent media artifacts (not provided in manifests; add overlays per environment).

## Kubernetes Strategy

- Base manifests reside in `infrastructure/k8s/base`.
- Environment overlays (`dev`, `prod`) apply image tags, replicas, scaling, and secret references.
- Windows workloads specify:
  ```yaml
  nodeSelector:
    kubernetes.io/os: windows
  tolerations:
    - key: "os"
      operator: "Equal"
      value: "windows"
      effect: "NoSchedule"
  ```
- Linux services omit Windows-specific selectors and default to standard Linux nodes.

## Observability

Recommended stack:

- **Logging**: Fluent Bit DaemonSet + CloudWatch Logs.
- **Metrics**: Prometheus Operator with custom exporters for bot metrics.
- **Tracing**: OpenTelemetry SDKs integrated into each service, exported to AWS X-Ray or Grafana Tempo.

## Security Considerations

- Manage application secrets via AWS Secrets Manager and Surface them as projected volumes.
- Enforce network policies to restrict intra-cluster access.
- Enable TLS termination at the load balancer; optionally use mTLS inside the cluster.

## Next Enhancements

- Introduce Helm charts or Terraform modules for infrastructure automation.
- Create an integration test harness that spins up disposable clusters for PR validation.
- Add Git submodules or package feeds for shared AI models and analytics pipelines.

