FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /workspace

# Copy solution + project files first to maximize restore layer cache
COPY src/Tracking.sln ./src/
COPY src/Tracking.Api/Tracking.Api.csproj ./src/Tracking.Api/
COPY Test/Tracking.Api.Tests/Tracking.Api.Tests.csproj ./Test/Tracking.Api.Tests/
RUN dotnet restore ./src/Tracking.sln

# Copy full sources
COPY src ./src
COPY Test ./Test
RUN dotnet publish ./src/Tracking.Api/Tracking.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Tracking.Api.dll"]
