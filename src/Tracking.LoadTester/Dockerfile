FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace

# Copy project file for restore caching
COPY src/Tracking.LoadTester/Tracking.LoadTester.csproj ./src/Tracking.LoadTester/
RUN dotnet restore ./src/Tracking.LoadTester/Tracking.LoadTester.csproj

COPY src ./src
RUN dotnet publish ./src/Tracking.LoadTester/Tracking.LoadTester.csproj -c Release -o /app/publish -r linux-x64 --self-contained true /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["./Tracking.LoadTester"]
