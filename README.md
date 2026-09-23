# WeatherStation

WeatherStation is an ASP.NET Core-based application for retrieving weather data from Netatmo, storing measurements in InfluxDB, and presenting data via Grafana.

The application consists of:

- A REST API built with ASP.NET Core.
- OAuth authentication against Netatmo.
- Automatic retrieval and storage of weather data.
- InfluxDB 3 Core as a time-series database.
- Grafana for visualization.
- InfluxDB 3 Explorer for administration and troubleshooting.
- NUnit-based unit tests.
- Docker Compose for local operation of the entire environment.

## Design principles

The underlying design follows a layered approach where the API layer communicates with the Application and Persistence layers through clear interfaces and services. Dependencies flow from the API layer toward the Application and Persistence layers. Controllers do not work directly with the InfluxDB client; instead, they use services and interfaces. This makes the logic testable and makes it possible to switch implementations for things like storage or external API communication.

The application is structured to be extensible with multiple data sources and storage solutions without needing to rebuild the existing integration. Data sources are responsible for retrieving and interpreting their own data, while shared models and abstractions are used between the integration and persistence layers.

```text
+---------------------------+      +--------------------------------------+
| Netatmo                   |      | Future data sources                  |
| - OAuth                   |      | - another weather station           |
| - data retrieval           |      | - external weather API              |
| - normalization           |      | - IoT devices                       |
+-------------+-------------+      | - MQTT                              |
              |                    | - home automation systems          |
              v                    | - manual measurement input          |
              +--------------------------------------------------------+
              | Application                                          |
              | - shared models                                      |
              | - application logic                                  |
              +---------------------+----------------------------------+
                                    |
                                    v
                     +---------------------------+
                     | Persistence abstractions  |
                     | - interfaces              |
                     | - dependency injection    |
                     | - storage adapters        |
                     +-------------+-------------+
                                   |
                    +------------------+------------------+
                    |                                     |
                    v                                     v
      +-----------------------------+       +-------------------------------+
      | InfluxDB 3 Core             |       | Elasticsearch                 |
      | - time-series data          |       | - searchable/structured data |
      +-----------------------------+       +-------------------------------+
                    |
                    v
      +----------------------------------------------+
      | Future storage solutions                     |
      | - another time-series database               |
      | - relational database                        |
      | - document database                          |
      | - cloud-based storage                        |
      +----------------------------------------------+
```

### Data sources

All Netatmo-specific code is grouped under Netatmo-related folders or named with the `Netatmo` prefix. This makes it clear which parts belong to Netatmo and creates a well-defined integration boundary for future data sources.

Examples of additional data sources include:

- Another weather station.
- An external weather API.
- IoT devices.
- MQTT.
- Home automation systems.
- Manually submitted measurements.

A new data source should be implemented behind an interface and its own services, in the same way as the Netatmo integration. It should be responsible for retrieving, interpreting, and normalizing its own data, while the rest of the application works with shared models and abstractions.

### Storage

The persistence layer is separate from the API and source integrations. The current implementation uses InfluxDB, but the structure makes it possible to add additional storage solutions.

Examples of additional storage solutions include:

- Elasticsearch.
- Another time-series database.
- Relational database.
- Document database.
- Cloud-based storage.

New storage implementations should be exposed through interfaces and registered via dependency injection. This way, a storage solution can be replaced or complemented without controllers, data sources, or application models needing to know about the concrete implementation.

### Architecture flow

```text
Data sources
    -> Retrieve and normalize data

Application
    -> Contains shared models and application logic

Persistence
    -> Stores data in one or more concrete storage solutions
```

The goal is to allow new data sources and new storage solutions to be added as separate implementations with as little impact as possible on the existing code.

## Implemented architecture

```text
                  +----------------+
                  |    Netatmo     |
                  |   OAuth + API  |
                  +--------+-------+
                           |
                           | HTTPS
                           v
+------------------+   +---+--------------------+
| Grafana          |-->| WeatherStation API     |
| :3000            |   | ASP.NET Core :8080     |
+--------+---------+   +---+--------------------+
         |                 |
         |                 | InfluxDB Client
         v                 v
  API status        +-----+----------------+
                    | InfluxDB 3 Core     |
                    | :8181               |
                    +---------------------+
```

The API has two main flows:

1. Retrieve data from Netatmo and return it over HTTP.
2. Retrieve data from Netatmo, transform it into InfluxDB points, and store it in InfluxDB.

## Project structure

```text
.
├── Dockerfile
├── docker-compose.yml
├── WeatherStation.sln
├── global.json
├── README.md
├── grafana/
│   ├── dashboards/
│   │   └── weather_readings.json
│   └── provisioning/
│       ├── dashboards/
│       └── datasources/
└── weatherStation/
    ├── API/
    ├── Application/
    ├── Persistence/
    └── WeatherStation.Tests/
```

### API

`weatherStation/API` contains the web application and application logic.

```text
API/
├── Auth/Netatmo/
│   ├── Interface/
│   ├── Options/
│   └── Services/
├── Background/
│   ├── Services/
│   └── Tasks/
├── Controllers/v1/
├── Exceptions/
├── Extensions/
├── Filters/
├── Logic/
├── Options/
├── Responses/
├── Services/
└── Program.cs
```

Responsibilities:

- Register ASP.NET Core and dependency injection.
- Configure Serilog.
- Expose REST endpoints.
- Handle Netatmo OAuth.
- Read and refresh Netatmo tokens.
- Retrieve home and station data from Netatmo.
- Start the periodic background process.
- Coordinate storage of weather measurements.

### Application

`weatherStation/Application` contains shared application models and response types.

Examples:

- `JsonHome`
- `JsonStationData`
- `ApiResponse<T>`
- `NetatmoError`
- Validation interfaces and shared models

### Persistence

`weatherStation/Persistence` is responsible for storing weather data in InfluxDB.

```text
Persistence/
├── Influx/
│   ├── InfluxClientFactory.cs
│   ├── InfluxOptions.cs
│   ├── InfluxWriteClient.cs
│   ├── InfluxWriteService.cs
│   └── Interface/
└── Models/
    ├── NetatmoIndoorPoint.cs
    ├── NetatmoOutdoorPoint.cs
    ├── NetatmoRainPoint.cs
    ├── NetatmoWindPoint.cs
    ├── NetatmoPointFactory.cs
    └── Interface/
```

`InfluxWriteService` converts the application's measurement points into InfluxDB `PointData` objects and writes them using `InfluxDB.Client`.

Different Netatmo module types map to different measurement point types:

| Netatmo type | Point class | Default table |
|---|---|---|
| `NAMain` | `NetatmoIndoorPoint` | `indoor` |
| `NAModule1` | `NetatmoOutdoorPoint` | `outdoor` |
| `NAModule2` | `NetatmoWindPoint` | `wind` |
| `NAModule3` | `NetatmoRainPoint` | `rain` |
| `NAModule4` | `NetatmoIndoorPoint` | `indoor` |

### WeatherStation.Tests

`weatherStation/WeatherStation.Tests` contains NUnit tests for:

- Controllers
- OAuth and token storage
- Options validation
- Netatmo deserialization
- Background services
- InfluxDB client and write services
- Mapping of Netatmo modules to InfluxDB points

## Netatmo OAuth

The application uses Netatmo's OAuth flow.

The flow is:

1. The client calls the login endpoint.
2. The API checks whether a valid token exists locally.
3. If no valid token exists, a Netatmo login link is returned.
4. The user authenticates with Netatmo.
5. Netatmo calls the API callback.
6. The API exchanges the authorization code for an access token and refresh token.
7. Token information is saved to a JSON file.
8. After the access token expires, the refresh token is used automatically.

The default token file location is:

```text
./data/netatmo.tokens.json
```

In Docker, the `/app/data` directory is mounted to the `api-data` volume, so the token file survives if the API container is restarted.

The token file should never be committed to Git or shared publicly.

## Background collection

On startup, a hosted background service is registered that periodically retrieves data from Netatmo.

The flow is:

1. Wait according to `Netatmo:Frequency`.
2. Check Netatmo authentication.
3. Refresh tokens if needed.
4. Retrieve home data to build a module-to-room mapping.
5. Retrieve station data for a Netatmo device.
6. Create InfluxDB points for the base station and its modules.
7. Write the points to the configured InfluxDB bucket.

Home data is cached in memory for a limited time to avoid unnecessary calls to Netatmo.

## REST API

The API version is specified in the URL using the `v1` format.

### Health check

```http
GET /api/v1/health
```

Returns:

```text
Healthy
```

### Netatmo status

```http
GET /api/v1/health/netatmo
```

Returns, among other things:

- Whether the user is authenticated.
- Whether login is required.
- Login URL.
- Token expiration time.
- Status code and message.

This endpoint is used by Grafana to display the Netatmo connection status.

### Start OAuth login

```http
GET /api/v1/netatmo/auth/login
```

If the user is not already authenticated, the API returns a redirect to Netatmo's OAuth page.

If a valid token already exists, the current authentication status is returned.

### OAuth callback

```http
GET /api/v1/netatmo/callback?code={authorizationCode}
```

The following alternative is also supported:

```http
GET /api/v1/netatmo/auth/callback?code={authorizationCode}
```

The endpoint receives the authorization code from Netatmo, retrieves the tokens, and stores them locally.

### Retrieve home data

```http
GET /api/v1/netatmo/homesdata
```

Optional filtering:

```http
GET /api/v1/netatmo/homesdata?gatewayTypes=NLG,OTH
```

This endpoint requires valid Netatmo authentication.

### Retrieve station data

```http
GET /api/v1/netatmo/moduledata/{moduleId}
```

Example:

```http
GET /api/v1/netatmo/moduledata/70:EE:50:12:34:56
```

The module ID must use MAC address format.

### Start manual ingest

```http
POST /api/v1/ingest
```

The endpoint retrieves current Netatmo data and writes it to InfluxDB.

Example response:

```json
{
  "writtenCount": 6
}
```

This endpoint requires valid Netatmo authentication.

### Swagger

When the API is running, Swagger UI is available at:

```text
http://localhost:8080/swagger
```

When using the HTTPS profile, this is normally:

```text
https://localhost:7130/swagger
```

## Configuration

The configuration is located in:

```text
weatherStation/API/appsettings.json
weatherStation/API/appsettings.Development.json
```

Sensitive values should not be placed in version-controlled files. Instead, use environment variables, user secrets, or a local development configuration.

### InfluxDB

```json
{
  "Influx": {
    "Url": "http://localhost:8181",
    "Token": "<influxdb-token>",
    "Org": "weatherstation",
    "Bucket": "readings",
    "Tables": {
      "NAMain": "indoor",
      "NAModule1": "outdoor",
      "NAModule2": "wind",
      "NAModule3": "rain",
      "NAModule4": "indoor"
    }
  }
}
```

When the API runs locally outside Docker, this is used:

```text
http://localhost:8181
```

When the API runs in Docker, the Docker Compose service DNS name is used:

```text
http://influxdb:8181
```

`Org` and `Bucket` are used by the InfluxDB client when writing weather data.

### Netatmo

```json
{
  "Netatmo": {
    "ClientId": "<netatmo-client-id>",
    "ClientSecret": "<netatmo-client-secret>",
    "ApiBaseUrl": "http://localhost:8080",
    "RedirectUri": "http://localhost:8080/api/v1/netatmo/callback",
    "Scopes": "read_station read_thermostat",
    "AuthorizeUrl": "https://api.netatmo.com/oauth2/authorize",
    "TokenUrl": "https://api.netatmo.com/oauth2/token",
    "TokenFilePath": "data/netatmo.tokens.json",
    "Frequency": "00:00:30"
  }
}
```

`RedirectUri` must be registered exactly the same way in Netatmo's developer console.

### WeatherApi

The application also has configuration for an external weather API:

```json
{
  "WeatherApi": {
    "BaseUrl": "<external-api-url>",
    "ApiKey": "<api-key>",
    "ReadingsPath": "readings",
    "ApiKeyHeaderName": "X-Api-Key"
  }
}
```

Values are validated at startup.

## Logging

The application uses Serilog.

The default configuration writes logs to the console:

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console"
    ],
    "WriteTo": [
      {
        "Name": "Console"
      }
    ]
  }
}
```

Logging to InfluxDB is not part of the active default solution. A future development step is to complement console logging with an InfluxDB 3 Core-native solution for structured logging.

Weather data sent to InfluxDB is handled separately by the `Persistence` project's `InfluxWriteService`.

## Docker Compose

The following services are defined in `docker-compose.yml`:

| Service | Port | Description |
|---|---:|---|
| `api` | `8080` | WeatherStation API |
| `influxdb` | `8181` | InfluxDB 3 Core |
| `influxdb3-explorer` | `8888` | Web interface for InfluxDB 3 |
| `grafana` | `3000` | Visualization and dashboards |

### Start the environment

```bash
docker compose up -d --build
```

### View logs

```bash
docker compose logs -f api
```

### Check services

```bash
docker compose ps
```

### Stop the environment

```bash
docker compose down
```

If Docker volumes are also removed, the stored data in those volumes is lost. InfluxDB data is stored according to the Compose configuration under:

```text
~/.influxdb3/data
```

InfluxDB Explorer UI data is stored separately under:

```text
.influxdb3-ui/
```

These two directories have different purposes. `.influxdb3-ui` contains UI information and is not the same as the actual InfluxDB data content.

## Grafana

Grafana runs at:

```text
http://localhost:3000
```

The default login can be configured with:

```bash
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=<password>
```

Grafana is configured automatically with two data sources:

1. `InfluxDB`
   - Used for weather measurements.
   - Points to `http://influxdb:8181`.

2. `WeatherStation API`
   - Used for Netatmo status.
   - Points to `http://api:8080`.

The dashboard is located at:

```text
grafana/dashboards/weather_readings.json
```

The dashboard includes, among other things:

- Time series for weather values from InfluxDB.
- Netatmo OAuth status.
- Login link when Netatmo is not authenticated.

## InfluxDB data

Measurement data is written to the configured bucket specified by:

```json
"Influx": {
  "Bucket": "readings"
}
```

InfluxDB points contain:

- Measurement.
- Field values from Netatmo.
- Tags such as device and room.
- UTC timestamp with nanosecond precision.

`InfluxDB.Client` uses the configuration fields `Token`, `Org`, and `Bucket` when writing. These names come from the client API; the project database is InfluxDB 3 Core.

InfluxDB 3 Core must be correctly configured and reachable before data can be written. Docker Compose starts InfluxDB 3 Core and configures storage volumes and networking, but it does not include a separate bootstrap for InfluxDB configuration or tokens.

## Local development

### Prerequisites

- .NET SDK `10.0.102`
- Docker Desktop if the full infrastructure is to run locally
- Netatmo developer application
- InfluxDB token
- Grafana, InfluxDB, and Netatmo configuration

The SDK version is specified in `global.json`.

### Build the solution

```bash
dotnet restore WeatherStation.sln
dotnet build WeatherStation.sln
```

### Start the API

From the repository root:

```bash
dotnet run --project weatherStation/API/API.csproj --launch-profile http
```

The API normally runs at:

```text
http://localhost:8080
```

### Run tests

```bash
dotnet test WeatherStation.sln --configuration Release
```

## Planned improvements

- Netatmo tokens are stored as a local JSON file and should be replaced with more secure persistent secret management in a production environment.
- The background service depends on Netatmo OAuth configuration being correct.
- Logging and measurements should eventually receive a more unified InfluxDB 3 Core-native integration.
- InfluxDB 3 Core configuration and tokens should be able to be initialized and managed more automatically together with Docker Compose.
- The API runs with HTTP in the Docker configuration. TLS should be handled by a reverse proxy or ingress in production.
