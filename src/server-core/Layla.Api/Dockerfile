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
RUN dotnet publish "Layla.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# -------- RUNTIME --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 7166
EXPOSE 5287
COPY --from=publish /app/publish .
COPY Certs/aspnetapp.pfx /app/certs/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourStrongPasswordAF

ENTRYPOINT ["dotnet", "Layla.Api.dll"]
