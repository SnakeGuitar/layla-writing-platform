# -------- BUILD --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && npm install -g pnpm \
    && rm -rf /var/lib/apt/lists/*

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .
ENV CI=true
RUN pnpm run build:frontend

# Publish
RUN dotnet publish "client-web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -------- RUNTIME --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

EXPOSE 7126
EXPOSE 5233

COPY --from=build --chown=app:app /app/publish .

# TLS certificate is expected to be mounted at runtime (e.g. via docker secret
# or volume) and its password injected through env vars by the orchestrator.
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/run/secrets/aspnetapp.pfx

# Drop privileges: the aspnet base image ships with a non-root "app" user.
USER app

ENTRYPOINT ["dotnet", "client-web.dll"]
