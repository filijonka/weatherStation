FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.19 AS base
WORKDIR /app

ENV ASPNETCORE_URLS="http://*:8080"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN addgroup -S weather --gid 2000 && adduser -S -u 1000 -G weather weather
RUN apk add --no-cache icu-libs

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.19 AS build
WORKDIR /src

# Copy solution and project files first to leverage build caching
COPY ["WeatherStation.sln", "."]
COPY ["weatherStation/API/API.csproj", "weatherStation/API/"]
COPY ["weatherStation/Persistence/Persistence.csproj", "weatherStation/Persistence/"]

RUN dotnet restore "weatherStation/API/API.csproj"
COPY ["weatherStation/", "weatherStation/"]
RUN dotnet build "weatherStation/API/API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "weatherStation/API/API.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app

COPY --from=publish --chown=weather:weather /app/publish ./
USER weather
ENTRYPOINT ["dotnet", "API.dll"]
