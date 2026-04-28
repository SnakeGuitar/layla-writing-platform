# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["Layla.Api/Layla.Api.csproj", "Layla.Api/"]
COPY ["Layla.Core/Layla.Core.csproj", "Layla.Core/"]
COPY ["Layla.Infrastructure/Layla.Infrastructure.csproj", "Layla.Infrastructure/"]
RUN dotnet restore "Layla.Api/Layla.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Layla.Api"
RUN dotnet build "Layla.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "Layla.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5288
EXPOSE 5287
COPY --from=publish --chown=app:app /app/publish .

# TLS certificate is expected to be mounted at runtime (e.g. via docker secret
# or volume) and its password injected through env vars by the orchestrator.
# Path defaults to /run/secrets/aspnetapp.pfx; override via compose/k8s if needed.
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/run/secrets/aspnetapp.pfx

# Drop privileges: the aspnet base image ships with a non-root "app" user.
USER app

ENTRYPOINT ["dotnet", "Layla.Api.dll"]
