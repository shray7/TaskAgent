# Build stage (context: repo root; solution at root, Backend code under Backend/)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and Backend (needed for solution restore; tests are not built)
COPY TaskAgent.sln .
COPY Backend/ Backend/

# Restore solution so all package references (including transitive) are resolved
RUN dotnet restore TaskAgent.sln

# Build and publish API only
RUN dotnet publish Backend/src/TaskAgent.Api/TaskAgent.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for healthcheck (aspnet image does not include it)
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TaskAgent.Api.dll"]
