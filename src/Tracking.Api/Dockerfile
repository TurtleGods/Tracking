FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Tracking.sln .
COPY Tracking.Api/Tracking.Api.csproj Tracking.Api/
RUN dotnet restore Tracking.Api/Tracking.Api.csproj

COPY . .
RUN dotnet publish Tracking.Api/Tracking.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Tracking.Api.dll"]
