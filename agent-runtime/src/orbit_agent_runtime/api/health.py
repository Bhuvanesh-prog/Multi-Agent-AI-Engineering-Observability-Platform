"""/health/live: process is up. /health/ready: Postgres reachable."""

from typing import Annotated

import psycopg
from fastapi import APIRouter, Depends, Request
from fastapi.responses import JSONResponse
from psycopg_pool import AsyncConnectionPool, PoolTimeout

from orbit_agent_runtime.core.problems import problem

router = APIRouter(prefix="/health", tags=["health"])


def get_pool(request: Request) -> AsyncConnectionPool:
    pool: AsyncConnectionPool = request.app.state.db_pool
    return pool


@router.get("/live")
async def live() -> dict[str, str]:
    return {"status": "Healthy"}


@router.get("/ready", response_model=None)
async def ready(pool: Annotated[AsyncConnectionPool, Depends(get_pool)]) -> dict[str, str] | JSONResponse:
    try:
        async with pool.connection(timeout=2) as conn:
            await conn.execute("select 1")
    except (PoolTimeout, psycopg.Error):
        return problem(503, "Postgres is not reachable.")
    return {"status": "Healthy"}
