# -------- BUILD --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy projects and restore
COPY ["client-web/client-web.csproj", "client-web/"]
COPY ["client-shared/client-shared.csproj", "client-shared/"]
RUN dotnet restore "client-web/client-web.csproj"

# Install Node.js and pnpm
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && npm install -g pnpm \
    && rm -rf /var/lib/apt/lists/*

# Copy package files and install dependencies
COPY ["client-web/package.json", "client-web/pnpm-lock.yaml", "client-web/"]
WORKDIR /src/client-web
RUN pnpm install --frozen-lockfile

# Copy everything else
WORKDIR /src
COPY client-web/ client-web/
COPY client-shared/ client-shared/

# Build frontend and backend
WORKDIR /src/client-web
RUN mkdir -p /src/certs \
    && dotnet dev-certs https -ep /src/certs/aspnetapp.pfx -p YourStrongPasswordAF --format Pfx
ENV CI=true
RUN pnpm run build:frontend
RUN dotnet build -c Release -o /app/build

# -------- PUBLISH --------
FROM build AS publish
RUN mkdir -p /src/certs \
    && dotnet dev-certs https -ep /src/certs/aspnetapp.pfx -p YourStrongPasswordAF --format Pfx
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# -------- RUNTIME --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
USER root
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE 7126
EXPOSE 5233
COPY --from=publish /app/publish .
COPY --from=publish /src/certs /app/certs
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourStrongPasswordAF

ENTRYPOINT ["dotnet", "client-web.dll"]
