# -------- BUILD --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN mkdir -p /src/certs \
    && dotnet dev-certs https -ep /src/certs/aspnetapp.pfx -p YourStrongPasswordAF
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && npm install -g pnpm \
    && rm -rf /var/lib/apt/lists/*
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
ENV CI=true
RUN pnpm run build:frontend
RUN dotnet build -c Release -o /app/build

# -------- PUBLISH --------
FROM build AS publish
RUN mkdir -p /src/certs \
    && dotnet dev-certs https -ep /src/certs/aspnetapp.pfx -p YourStrongPasswordAF
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# -------- RUNTIME --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 7126
EXPOSE 5233
COPY --from=publish /app/publish .
COPY --from=publish /src/certs /app/certs
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourStrongPasswordAF

ENTRYPOINT ["dotnet", "client-web.dll"]
