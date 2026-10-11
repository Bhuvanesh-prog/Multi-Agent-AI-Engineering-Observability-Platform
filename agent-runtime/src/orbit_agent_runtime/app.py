"""FastAPI application: lifespan-managed resources and routers."""

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI
from psycopg_pool import AsyncConnectionPool

from orbit_agent_runtime.api import health
from orbit_agent_runtime.core.problems import add_problem_handlers
from orbit_agent_runtime.core.settings import Settings, get_settings


def create_app(settings: Settings | None = None) -> FastAPI:
    settings = settings or get_settings()

    @asynccontextmanager
    async def lifespan(app: FastAPI) -> AsyncIterator[None]:
        pool = AsyncConnectionPool(
            settings.database_url.get_secret_value(),
            min_size=1,
            max_size=5,
            open=False,
            kwargs={"autocommit": True},
        )
        # Don't block startup on the DB: readiness reports it instead.
        await pool.open(wait=False)
        app.state.db_pool = pool
        try:
            yield
        finally:
            await pool.close()

    app = FastAPI(title="Orbit agent runtime", version="0.1.0", lifespan=lifespan)
    add_problem_handlers(app)
    app.include_router(health.router)
    return app


def __getattr__(name: str) -> FastAPI:
    # `uvicorn orbit_agent_runtime.app:app` builds the app lazily so importing this module needs no env.
    if name == "app":
        return create_app()
    raise AttributeError(name)
