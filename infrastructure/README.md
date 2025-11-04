# Kubernetes Manifests

The manifests under `infrastructure/k8s` provide a baseline for deploying the realtime meeting platform to AWS EKS. The structure follows a Kustomize layout with a reusable base and environment-specific overlays (`dev`, `prod`).

## Usage

```bash
kustomize build infrastructure/k8s/overlays/dev | kubectl apply -f -
```

Update container image references to point at your Amazon ECR repositories and replace `config/secrets-placeholder.yaml` with a secure Secret manifest managed outside of source control.
