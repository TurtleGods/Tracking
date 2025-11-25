# Multi-stage Dockerfile for Mayo.Platform.Tracklix.WebAPI
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10 AS build
WORKDIR /src
COPY ["src/Mayo.Platform.Tracklix.WebAPI/Mayo.Platform.Tracklix.WebAPI.csproj", "src/Mayo.Platform.Tracklix.WebAPI/"]
RUN dotnet restore "src/Mayo.Platform.Tracklix.WebAPI/Mayo.Platform.Tracklix.WebAPI.csproj"

COPY . .
WORKDIR "/src/src/Mayo.Platform.Tracklix.WebAPI"
RUN dotnet build "Mayo.Platform.Tracklix.WebAPI.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Mayo.Platform.Tracklix.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mayo.Platform.Tracklix.WebAPI.dll"]