# ==============================================================================
# Customer Service — build multi-etapa
# El contexto de build es la RAÍZ del repositorio (ver docker-compose.yml),
# para que Directory.Build.props y Directory.Packages.props sean visibles.
# ==============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los archivos de proyecto: si el código cambia pero las
# dependencias no, Docker reutiliza la capa del restore.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/shared-contracts/Devsu.Shared.Contracts/*.csproj              src/shared-contracts/Devsu.Shared.Contracts/
COPY src/shared-contracts/Devsu.Shared.Messaging/*.csproj              src/shared-contracts/Devsu.Shared.Messaging/
COPY src/Services/Customer/Devsu.Customer.Domain/*.csproj            src/Services/Customer/Devsu.Customer.Domain/
COPY src/Services/Customer/Devsu.Customer.Application/*.csproj       src/Services/Customer/Devsu.Customer.Application/
COPY src/Services/Customer/Devsu.Customer.Infrastructure/*.csproj    src/Services/Customer/Devsu.Customer.Infrastructure/
COPY src/Services/Customer/Devsu.Customer.Api/*.csproj               src/Services/Customer/Devsu.Customer.Api/

RUN dotnet restore src/Services/Customer/Devsu.Customer.Api/Devsu.Customer.Api.csproj

COPY src/ src/
RUN dotnet publish src/Services/Customer/Devsu.Customer.Api/Devsu.Customer.Api.csproj \
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
# La imagen aspnet:10.0 (Debian) ya incluye ICU.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

# Usuario sin privilegios: la imagen aspnet ya trae 'app' (UID 1654).
USER app

ENTRYPOINT ["dotnet", "Devsu.Customer.Api.dll"]
