# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY GfcEngine.sln ./
COPY src/GfcEngine.Domain/GfcEngine.Domain.csproj src/GfcEngine.Domain/
COPY src/GfcEngine.Application/GfcEngine.Application.csproj src/GfcEngine.Application/
COPY src/GfcEngine.Infrastructure/GfcEngine.Infrastructure.csproj src/GfcEngine.Infrastructure/
COPY src/GfcEngine.API/GfcEngine.API.csproj src/GfcEngine.API/
COPY tests/GfcEngine.Tests/GfcEngine.Tests.csproj tests/GfcEngine.Tests/

# Restore dependencies
RUN dotnet restore GfcEngine.sln

# Copy source code and build
COPY . .
RUN dotnet build GfcEngine.sln -c Release --no-restore

# Publish API
RUN dotnet publish src/GfcEngine.API/GfcEngine.API.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GfcEngine.API.dll"]

