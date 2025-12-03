FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Tracking.LoadTester/Tracking.LoadTester.csproj Tracking.LoadTester/
RUN dotnet restore Tracking.LoadTester/Tracking.LoadTester.csproj

COPY . .
RUN dotnet publish Tracking.LoadTester/Tracking.LoadTester.csproj -c Release -o /app/publish -r linux-x64 --self-contained true /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["./Tracking.LoadTester"]
