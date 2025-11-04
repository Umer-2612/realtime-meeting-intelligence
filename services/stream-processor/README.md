# Stream Processor Service

Lightweight FastAPI microservice that consumes media events produced by the Teams and Zoom bots. Extend this component with analytics pipelines (speech-to-text, sentiment, summarisation) and integrate with AWS managed services such as Kinesis, Lambda, or SageMaker endpoints.

## Local Development

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn stream_processor.app:app --reload
```

## Docker Build

```bash
docker build -t stream-processor:dev .
```

## Kubernetes Probes

- `GET /healthz`: liveness/readiness probe.
- `POST /events`: basic ingestion endpoint. Replace body schema with your media envelope.
