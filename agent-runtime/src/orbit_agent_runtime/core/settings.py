"""Runtime settings from ORBIT_* environment variables (.env locally, Key Vault-backed env in Azure)."""

from functools import lru_cache

from pydantic import SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_prefix="ORBIT_",
        env_file=(".env", "../.env"),
        extra="ignore",
    )

    environment: str = "Development"
    database_url: SecretStr
    host: str = "127.0.0.1"
    port: int = 8081


@lru_cache
def get_settings() -> Settings:
    """Fail fast: raises a validation error at startup when required settings are missing."""
    return Settings()  # type: ignore[call-arg]  # values come from the environment
