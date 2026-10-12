from httpx import AsyncClient


async def test_live_process_up_returns_200(client: AsyncClient) -> None:
    response = await client.get("/health/live")

    assert response.status_code == 200
    assert response.json() == {"status": "Healthy"}


async def test_ready_database_reachable_returns_200(client: AsyncClient) -> None:
    response = await client.get("/health/ready")

    assert response.status_code == 200


async def test_ready_database_unreachable_returns_503_problem(client_without_db: AsyncClient) -> None:
    response = await client_without_db.get("/health/ready")

    assert response.status_code == 503
    assert response.headers["content-type"] == "application/problem+json"
    assert response.json()["status"] == 503


async def test_unknown_route_returns_404_problem(client: AsyncClient) -> None:
    response = await client.get("/nope")

    assert response.status_code == 404
    assert response.headers["content-type"] == "application/problem+json"
