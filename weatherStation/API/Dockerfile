FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ./weatherStation/API/API.csproj ./weatherStation/API/
RUN dotnet restore ./weatherStation/API/API.csproj

COPY ./weatherStation/API ./weatherStation/API
WORKDIR /src/weatherStation/API
RUN dotnet publish API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WeatherStation.Api.dll"]
