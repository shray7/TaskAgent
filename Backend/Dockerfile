# Build stage (context: repo root; solution at root, Backend code under Backend/)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files (paths relative to repo root)
COPY TaskAgent.sln .
COPY Backend/src/TaskAgent.Api/TaskAgent.Api.csproj Backend/src/TaskAgent.Api/
COPY Backend/src/TaskAgent.Contracts/TaskAgent.Contracts.csproj Backend/src/TaskAgent.Contracts/
COPY Backend/src/TaskAgent.DataAccess/TaskAgent.DataAccess.csproj Backend/src/TaskAgent.DataAccess/

# Restore dependencies
RUN dotnet restore Backend/src/TaskAgent.Api/TaskAgent.Api.csproj

# Copy source code
COPY Backend/src/ Backend/src/

# Build and publish
RUN dotnet publish Backend/src/TaskAgent.Api/TaskAgent.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TaskAgent.Api.dll"]
