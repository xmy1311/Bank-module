# ==============================================================================
# Account Service — build multi-etapa
# El contexto de build es la RAÍZ del repositorio (ver docker-compose.yml).
# ==============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/shared-contracts/Devsu.Shared.Contracts/*.csproj             src/shared-contracts/Devsu.Shared.Contracts/
COPY src/shared-contracts/Devsu.Shared.Messaging/*.csproj             src/shared-contracts/Devsu.Shared.Messaging/
COPY src/Services/Account/Devsu.Account.Domain/*.csproj             src/Services/Account/Devsu.Account.Domain/
COPY src/Services/Account/Devsu.Account.Application/*.csproj        src/Services/Account/Devsu.Account.Application/
COPY src/Services/Account/Devsu.Account.Infrastructure/*.csproj     src/Services/Account/Devsu.Account.Infrastructure/
COPY src/Services/Account/Devsu.Account.Api/*.csproj                src/Services/Account/Devsu.Account.Api/

RUN dotnet restore src/Services/Account/Devsu.Account.Api/Devsu.Account.Api.csproj

COPY src/ src/
RUN dotnet publish src/Services/Account/Devsu.Account.Api/Devsu.Account.Api.csproj \
        -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl para el HEALTHCHECK de docker-compose: la imagen aspnet no lo incluye.
# Se instala como root, ANTES de bajar a un usuario sin privilegios.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT queda deliberadamente SIN definir:
# Microsoft.Data.SqlClient falla con "Globalization Invariant Mode is not supported".
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

USER app

ENTRYPOINT ["dotnet", "Devsu.Account.Api.dll"]
