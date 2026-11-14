# RaceDay

RaceDay is an event-management system for running, walking, and cycling
communities. It supports event organisers who manage events, categories,
enrolments, and results, and participants who discover events, enrol, and
manage their profiles.

This repository brings together the RaceDay portfolio work: the planning and
database design from Part 1, the API and web application, and the later API
features and automated verification covered by Parts 2 and 3.

## Contents

- [Project structure](#project-structure)
- [Part 1: Planning and database design](#part-1-planning-and-database-design)
- [Part 2: API and web application](#part-2-api-and-web-application)
- [Part 3: Extended functionality and verification](#part-3-extended-functionality-and-verification)
- [Technology stack](#technology-stack)
- [Run locally](#run-locally)
- [Run with Docker Compose](#run-with-docker-compose)
- [Database](#database)
- [Image storage configuration](#image-storage-configuration)
- [Tests and CI](#tests-and-ci)
- [Portfolio resources](#portfolio-resources)

## Project structure

```text
RaceDay/
├── Controllers/                 # REST API endpoints
├── Data/
│   └── RaceDayDbContext.cs      # EF Core database context and relationships
├── DTOs/                        # API request models
├── Models/                      # Database entities
├── Security/                    # Password hashing and session authentication
├── Services/                    # Image validation and blob storage
├── Tests/                       # API integration and unit tests
├── RaceDay.Web/                 # Server-rendered ASP.NET Core MVC application
│   ├── Controllers/
│   ├── Services/                # Typed client for the API
│   ├── Views/
│   └── wwwroot/
├── docs/
│   ├── API-Endpoint-Plan.md     # Endpoint plan and access rules
│   ├── database.sql             # SQL Server schema and example rows
│   ├── ERD.png
│   ├── ERD.svg
│   └── Part2-Implementation-Notes.md
├── .github/workflows/           # Part 1 validation and .NET CI workflows
├── Program.cs                   # API startup and database configuration
├── RaceDay.Api.csproj
├── RaceDay.Web/RaceDay.Web.csproj
├── Tests/RaceDay.Api.Tests.csproj
├── RaceDay.sln
├── RaceDay.Api.http             # Example API requests
├── docker-compose.yml
└── README.md
```

## Part 1: Planning and database design

Part 1 defines the system before implementation. Its deliverables and
requirements include:

- The entity-relationship diagram, identifying entities, attributes, keys,
  relationships, and cardinality.
- A REST API endpoint plan describing methods, routes, purpose, access, request
  bodies, and expected responses.
- A SQL Server database script with tables, keys, constraints, and sample rows.
- GitHub Actions validation for the required Part 1 documentation and files.

The design covers these resources: event organisers, participants, events,
categories, weather records, registrations, and results. Its principal
relationships are:

- One organiser can manage many events.
- One event can have many categories and weather records.
- Participants, events, and categories are related through registrations.
- A registration can have zero or one result.

See the [ERD](docs/ERD.svg), [endpoint plan](docs/API-Endpoint-Plan.md), and
[SQL Server script](docs/database.sql). The script is for a clean SQL Server
database; it drops and recreates tables, so do not run it against data you need
to keep.

## Part 2: API and web application

Part 2 implements the planned system as two ASP.NET Core applications:

- **RaceDay.Api** provides authenticated REST endpoints for account
  registration and login, events, organisers, categories, participants,
  weather, registrations/enrolments, results, profiles, and image uploads.
- **RaceDay.Web** provides the organiser and participant web experience. It
  communicates with the API through a typed HTTP client rather than accessing
  the database directly.

The application supports event browsing and filtering, organiser event and
category management, enrolment management, participant registration and
profile management, and result entry and history. The API also supports the
existing registration routes and enrolment route aliases.

Authentication uses server-side session cookies, and passwords are stored as
PBKDF2-SHA256 hashes. The web application applies antiforgery protection to
requests that change data.

## Part 3: Extended functionality and verification

The repository's Part 3 API tests cover the extended business rules and
behaviours, including:

- Event type and distance validation, search and browse filters, and
  organiser-specific event listings.
- Category age, price, capacity, and enrolment-count validation.
- Enrolment capacity, cancellation, past-event restrictions, and status
  management.
- Result status validation and participant result history.
- Duplicate account handling and role validation.
- Event banner and profile-picture uploads, including access and file-size
  checks.

The implementation also includes event banner URLs, participant and organiser
profile picture URLs, category prices and optional capacity, and result
statuses such as `Finished`, `DNF`, and `DQ`. Cancelled enrolments do not count
toward capacity. Weather is stored locally; live forecast retrieval is not
provided.

## Technology stack

- .NET 10 and ASP.NET Core
- Entity Framework Core 10
- SQLite for local development; SQL Server for Docker Compose deployments
- ASP.NET Core MVC for the web application
- xUnit for automated tests
- Azure Blob Storage for uploaded images (optional)
- Docker Compose and GitHub Actions

## Run locally

### Prerequisites

- .NET 10 SDK
- No database server is needed when using the default local SQLite settings.
- Azure Blob Storage is only needed if you want to use image uploads against
  real blob storage.

### Build, run, and test

From the repository root:

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet test RaceDay.sln
```

Start the API:

```powershell
dotnet run --project RaceDay.Api.csproj
```

The API listens at `http://localhost:5184` by default, and Swagger is available
at `http://localhost:5184/swagger`.

In another terminal, start the web application:

```powershell
dotnet run --project RaceDay.Web
```

Open the address printed by the web application's startup output. By default,
the web application calls the API at `http://localhost:5184/`; configure
`RaceDayApi:BaseUrl` if the API is running elsewhere.

## Run with Docker Compose

1. Copy `.env.example` to `.env`.
2. Set a strong local `RACEDAY_DB_PASSWORD`. Optionally set the Azure Blob
   Storage values if image uploads are required.
3. Start the stack:

   ```powershell
   docker compose up --build
   ```

4. Open the web application at `http://localhost:8080`. The API is available at
   `http://localhost:5000` and Swagger at `http://localhost:5000/swagger`.
5. Stop the stack with `docker compose down`.

Compose runs SQL Server with a persistent Docker volume and waits for its
health check before starting the API, then waits for API health before starting
the web application. The `.env` file is ignored by Git; never commit production
credentials.

## Database

Local development defaults to SQLite using `Data Source=raceday.db`. The API
creates the database and schema on first startup when
`Database:EnsureCreatedOnStartup` is enabled. No separate database server is
required. The local database file is in the repository root.

Docker Compose configures the API to use SQL Server instead. The provider and
connection string can be changed through configuration:

- `Database:Provider`: `Sqlite` or `SqlServer`
- `ConnectionStrings:RaceDayDb`: database connection string
- `Database:EnsureCreatedOnStartup`: create a database schema on startup

`EnsureCreated` is intended for initial database creation and is not a
replacement for migrations when evolving an existing schema. Back up
important data before changing database configuration or schema. The
`docs/database.sql` script is for SQL Server and is destructive to existing
tables; it is not the script used for the default SQLite database.

## Image storage configuration

Image uploads use Azure Blob Storage when configured. Set
`AzureBlobStorage:ConnectionString` and `AzureBlobStorage:ContainerName` (or
the corresponding environment variables). Uploads accept JPG, PNG, WEBP, and
GIF images up to 5 MB. Automated API tests use fake blob storage and do not
need Azure credentials.

## Tests and CI

Run all solution tests with:

```powershell
dotnet test RaceDay.sln
```

The tests include API integration tests and password service unit tests. The
integration test host uses an in-memory database and fake blob storage.
GitHub Actions workflows validate the Part 1 documentation and build/test the
.NET solution; the CI workflow also builds both Docker images.

## Portfolio resources

- [Part 1 ERD (SVG)](docs/ERD.svg) and [Part 1 ERD (PNG)](docs/ERD.png)
- [API endpoint plan](docs/API-Endpoint-Plan.md)
- [SQL Server database script](docs/database.sql)
- [Part 2 implementation notes](docs/Part2-Implementation-Notes.md)
- [Example API requests](RaceDay.Api.http)
- [Part 1 project video](https://youtu.be/3arvoiBoRyE)

**Author:** Mmasechab Kgolane Nkadimeng
