# syntax=docker/dockerfile:1
# Multi-stage build for the Library API. Run from the repository root:
#   docker build -f src/Library.Api/Dockerfile -t library-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY LibraryManagementSystem.slnx ./
COPY src/Library.Domain/Library.Domain.csproj              src/Library.Domain/
COPY src/Library.Application/Library.Application.csproj    src/Library.Application/
COPY src/Library.Infrastructure/Library.Infrastructure.csproj src/Library.Infrastructure/
COPY src/Library.Api/Library.Api.csproj                    src/Library.Api/
RUN dotnet restore src/Library.Api/Library.Api.csproj

COPY src/ src/
RUN dotnet publish src/Library.Api/Library.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql probes for Kerberos/GSSAPI at startup; without this it logs
# "Cannot load library libgssapi_krb5.so.2" before falling back to password auth.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# The aspnet image ships a non-root 'app' user (UID 1654); run as it.
COPY --from=build --chown=app:app /app ./
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# The /health endpoint is the readiness probe; container orchestrators
# (compose, k8s) should poll it over HTTP rather than shelling in.

ENTRYPOINT ["dotnet", "Library.Api.dll"]
