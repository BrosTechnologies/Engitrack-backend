# Use the official .NET 9.0 SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files for better layer caching
COPY Engitrack.sln .
COPY src/Api/Engitrack.Api.csproj src/Api/
COPY src/BuildingBlocks/Engitrack.BuildingBlocks.csproj src/BuildingBlocks/
COPY src/Projects/Engitrack.Projects.Domain/Engitrack.Projects.Domain.csproj src/Projects/Engitrack.Projects.Domain/
COPY src/Projects/Engitrack.Projects.Application/Engitrack.Projects.Application.csproj src/Projects/Engitrack.Projects.Application/
COPY src/Projects/Engitrack.Projects.Infrastructure/Engitrack.Projects.Infrastructure.csproj src/Projects/Engitrack.Projects.Infrastructure/
COPY src/Inventory/Engitrack.Inventory.Domain/Engitrack.Inventory.Domain.csproj src/Inventory/Engitrack.Inventory.Domain/
COPY src/Inventory/Engitrack.Inventory.Application/Engitrack.Inventory.Application.csproj src/Inventory/Engitrack.Inventory.Application/
COPY src/Inventory/Engitrack.Inventory.Infrastructure/Engitrack.Inventory.Infrastructure.csproj src/Inventory/Engitrack.Inventory.Infrastructure/
COPY src/Workers/Engitrack.Workers.Domain/Engitrack.Workers.Domain.csproj src/Workers/Engitrack.Workers.Domain/
COPY src/Workers/Engitrack.Workers.Application/Engitrack.Workers.Application.csproj src/Workers/Engitrack.Workers.Application/
COPY src/Workers/Engitrack.Workers.Infrastructure/Engitrack.Workers.Infrastructure.csproj src/Workers/Engitrack.Workers.Infrastructure/
COPY src/Incidents/Engitrack.Incidents.Domain/Engitrack.Incidents.Domain.csproj src/Incidents/Engitrack.Incidents.Domain/
COPY src/Incidents/Engitrack.Incidents.Application/Engitrack.Incidents.Application.csproj src/Incidents/Engitrack.Incidents.Application/
COPY src/Incidents/Engitrack.Incidents.Infrastructure/Engitrack.Incidents.Infrastructure.csproj src/Incidents/Engitrack.Incidents.Infrastructure/
COPY src/Machinery/Engitrack.Machinery.Domain/Engitrack.Machinery.Domain.csproj src/Machinery/Engitrack.Machinery.Domain/
COPY src/Machinery/Engitrack.Machinery.Application/Engitrack.Machinery.Application.csproj src/Machinery/Engitrack.Machinery.Application/
COPY src/Machinery/Engitrack.Machinery.Infrastructure/Engitrack.Machinery.Infrastructure.csproj src/Machinery/Engitrack.Machinery.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY src/ src/

# Build and publish the application
RUN dotnet publish src/Api/Engitrack.Api.csproj -c Release -o out --no-restore

# Use the official .NET 9.0 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy the published application from the build stage
COPY --from=build /app/out .

# Change ownership of the app directory
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose the port that the application will run on
EXPOSE $PORT

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Engitrack.Api.dll"]