import asyncio
import sys
from collections.abc import AsyncIterator, Callable, Iterator, Mapping

import pytest
from asgi_lifespan import LifespanManager
from httpx import ASGITransport, AsyncClient
from pydantic import SecretStr
from testcontainers.community.postgres import PostgresContainer

from orbit_agent_runtime.app import create_app
from orbit_agent_runtime.core.settings import Settings

PG_IMAGE = "pgvector/pgvector:0.8.7-pg18"


def pytest_asyncio_loop_factories(
    config: pytest.Config, item: pytest.Item
) -> Mapping[str, Callable[[], asyncio.AbstractEventLoop]]:
    # psycopg async needs a selector loop on Windows; CI (Linux) uses the default.
    if sys.platform == "win32":
        return {"selector": asyncio.SelectorEventLoop}
    return {"default": asyncio.new_event_loop}


@pytest.fixture(scope="session")
def postgres_url() -> Iterator[str]:
    with PostgresContainer(PG_IMAGE, driver=None) as pg:
        yield pg.get_connection_url()


async def _client(settings: Settings) -> AsyncIterator[AsyncClient]:
    app = create_app(settings)
    async with LifespanManager(app) as manager:
        transport = ASGITransport(app=manager.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            yield client


@pytest.fixture
async def client(postgres_url: str) -> AsyncIterator[AsyncClient]:
    async for c in _client(Settings(database_url=SecretStr(postgres_url))):
        yield c


@pytest.fixture
async def client_without_db() -> AsyncIterator[AsyncClient]:
    url = "postgresql://x:x@127.0.0.1:1/orbit?connect_timeout=1"
    async for c in _client(Settings(database_url=SecretStr(url))):
        yield c
