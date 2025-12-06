# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file and project files
COPY ["Maliev.QuotationService.Api/Maliev.QuotationService.Api.csproj", "Maliev.QuotationService.Api/"]
COPY ["Maliev.QuotationService.Data/Maliev.QuotationService.Data.csproj", "Maliev.QuotationService.Data/"]
COPY ["nuget.config", "./"]

# Restore packages with BuildKit secrets for NuGet authentication
RUN --mount=type=secret,id=nuget_username \
  --mount=type=secret,id=nuget_password \
  export NUGET_USERNAME=$(cat /run/secrets/nuget_username) && \
  export NUGET_PASSWORD=$(cat /run/secrets/nuget_password) && \
  dotnet restore "Maliev.QuotationService.Api/Maliev.QuotationService.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/Maliev.QuotationService.Api"
RUN dotnet build "Maliev.QuotationService.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Maliev.QuotationService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Set ownership to built-in app user BEFORE switching user
COPY --from=publish /app/publish .
RUN chown -R app:app /app

# Switch to non-root app user
USER app

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/quotation/liveness || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Maliev.QuotationService.Api.dll"]
