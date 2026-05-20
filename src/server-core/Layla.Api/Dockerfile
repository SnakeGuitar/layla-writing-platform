# -------- BUILD --------
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

# -------- PUBLISH --------
FROM build AS publish
RUN mkdir -p /src/certs \
    && dotnet dev-certs https -ep /src/certs/aspnetapp.pfx -p YourStrongPasswordAF --format Pfx
RUN dotnet publish "Layla.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# -------- RUNTIME --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER root
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE 7166
EXPOSE 5287
COPY --from=publish /app/publish .
COPY --from=publish /src/certs /app/certs_static
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs_static/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourStrongPasswordAF

ENTRYPOINT ["dotnet", "Layla.Api.dll"]
