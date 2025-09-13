# sp2023-mis421-mockinterviews/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8001

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["sp2023-mis421-mockinterviews.csproj", "./"]
RUN dotnet restore "sp2023-mis421-mockinterviews.csproj"
COPY . .
RUN dotnet build "sp2023-mis421-mockinterviews.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sp2023-mis421-mockinterviews.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set port to 8001
ENV ASPNETCORE_URLS=http://+:8001

# Add curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8001/health || exit 1

ENTRYPOINT ["dotnet", "sp2023-mis421-mockinterviews.dll"]