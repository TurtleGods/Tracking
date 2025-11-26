FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Tracking.LoadTester/Tracking.LoadTester.csproj Tracking.LoadTester/
RUN dotnet restore Tracking.LoadTester/Tracking.LoadTester.csproj

COPY . .
RUN dotnet publish Tracking.LoadTester/Tracking.LoadTester.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Tracking.LoadTester.dll"]
