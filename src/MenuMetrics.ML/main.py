"""Optional ML microservice — Python FastAPI.
If running on http://localhost:8001, the .NET ForecastService will use it.
If not, the .NET app falls back to a built-in seasonal-naive model.
"""
from fastapi import FastAPI
from pydantic import BaseModel
from typing import List
from datetime import datetime, timedelta
import statistics

app = FastAPI(title="MenuMetrics ML")

class Point(BaseModel):
    ds: datetime
    y: float

class ForecastReq(BaseModel):
    horizon: int = 14
    history: List[Point]

@app.get("/health")
def health(): return {"ok": True}

@app.post("/forecast")
def forecast(req: ForecastReq):
    if not req.history:
        return []
    by_dow = {}
    for p in req.history:
        by_dow.setdefault(p.ds.weekday(), []).append(p.y)
    avg = statistics.mean([p.y for p in req.history])
    out = []
    last = max(p.ds for p in req.history)
    for i in range(1, req.horizon + 1):
        d = last + timedelta(days=i)
        seasonal = statistics.mean(by_dow.get(d.weekday(), [avg]))
        trend = 0.005 * avg * i
        pred = max(0.0, avg + (seasonal - avg) + trend)
        out.append({
            "Date": d.isoformat(),
            "Predicted": round(pred, 1),
            "Lower": round(pred * 0.85, 1),
            "Upper": round(pred * 1.15, 1),
            "Base": round(avg, 1),
            "Seasonality": round(seasonal - avg, 1),
            "Trend": round(trend, 1),
        })
    return out
