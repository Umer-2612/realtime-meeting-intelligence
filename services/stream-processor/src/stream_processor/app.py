"""Lightweight FastAPI service that processes media events from Teams and Zoom bots."""
from fastapi import FastAPI

app = FastAPI(title="Realtime Stream Processor")


@app.get("/healthz")
def healthz() -> dict[str, str]:
    """Kubernetes liveness/readiness probe endpoint."""
    return {"status": "ok"}


@app.post("/events")
def ingest_event(event: dict) -> dict[str, str]:
    """Accepts an event payload, triggers downstream processing, and echoes acceptance."""
    # TODO: integrate with analytics pipeline
    return {"status": "accepted"}
