# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the entire source code first so that all referenced projects are available
COPY . .

# Restore dependencies
RUN dotnet restore "src/SS.AuthService.API/SS.AuthService.API.csproj"

WORKDIR "/src/src/SS.AuthService.API"

# Build and publish
RUN dotnet publish "SS.AuthService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Use the built-in non-root 'app' user (available since .NET 8)
USER app

# Copy published artifacts from build stage
COPY --from=build /app/publish .

# Expose the default ASP.NET port
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SS.AuthService.API.dll"]
