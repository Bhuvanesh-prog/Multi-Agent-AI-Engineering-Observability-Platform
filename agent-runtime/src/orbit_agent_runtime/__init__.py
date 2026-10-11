"""Orbit agent runtime."""

import asyncio
import sys

import uvicorn

from orbit_agent_runtime.core.settings import get_settings


def main() -> None:
    """Run the HTTP surface (health, later the graph API) with uvicorn."""
    settings = get_settings()
    config = uvicorn.Config(
        "orbit_agent_runtime.app:app",
        host=settings.host,
        port=settings.port,
        log_level="info",
    )
    server = uvicorn.Server(config)
    # psycopg's async mode can't run on Windows' default Proactor loop; Linux (prod) is unaffected.
    loop_factory = asyncio.SelectorEventLoop if sys.platform == "win32" else None
    asyncio.run(server.serve(), loop_factory=loop_factory)
