# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY TaskAgent.sln .
COPY src/TaskAgent.Api/TaskAgent.Api.csproj src/TaskAgent.Api/
COPY src/TaskAgent.Contracts/TaskAgent.Contracts.csproj src/TaskAgent.Contracts/
COPY src/TaskAgent.DataAccess/TaskAgent.DataAccess.csproj src/TaskAgent.DataAccess/

# Restore dependencies
RUN dotnet restore src/TaskAgent.Api/TaskAgent.Api.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/TaskAgent.Api/TaskAgent.Api.csproj -c Release -o /app/publish --no-restore

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
