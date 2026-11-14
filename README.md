RaceDay

RaceDay is a race management system built for IIE PROG6212 (Programming 2B).

Part 1: API planning (endpoint plan, database script, ERD) in docs/
Part 2: ASP.NET Core Web API with automated tests and CI
Part 3: ASP.NET Core MVC web application that consumes the Part 2 API, stores files in Azure Blob Storage, and runs in Docker


Table of Contents
Solution Structure
Technology Stack
Prerequisites
Configuration
Running Locally
Running with Docker
Running the Tests
Continuous Integration
Azure Blob Storage
Documentation
Troubleshooting
Author
Solution Structure
RaceDayPart-2/
├── .github/workflows/
│   ├── part1-validation.yml     # Checks the Part 1 documents exist
│   └── dotnet-ci.yml            # Build, test and Docker image build
├── docs/
│   ├── API-Endpoint-Plan.md     # Method, Route, Purpose, Access
│   ├── database.sql             # CREATE TABLE and INSERT INTO statements
│   └── ERD.png                  # Entity relationship diagram
├── RaceDay.Web/                 # Part 3: MVC web application
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── Services/                # API client and Azure Blob service
│   └── Dockerfile               # Web image
├── Tests/
│   └── RaceDay.Api.Tests.csproj # API unit tests
├── Dockerfile                   # API image
├── docker-compose.yml           # Runs API and web app together
├── RaceDay.sln
└── README.md

The API project sits at the repository root and is built by the root Dockerfile. Adjust folder names above if your project differs slightly.



Technology Stack
Area	Technology
Framework	.NET 10, ASP.NET Core
Web API	ASP.NET Core Web API (Part 2)
Web UI	ASP.NET Core MVC with Razor views (Part 3)
File storage	Azure Blob Storage
Containers	Docker, Docker Compose
Testing	xUnit (API tests)
CI	GitHub Actions
Prerequisites
.NET 10 SDK
Visual Studio 2022 or later (or VS Code)
Docker Desktop (for the container steps)
An Azure Storage account (for Blob Storage features)
Git
Configuration

Secrets are never committed to the repository. Provide them through environment variables or .NET user secrets.

Setting	Used by	Description
ApiBaseUrl	RaceDay.Web	Base URL of the running API, for example https://localhost:5001/
AzureBlob__ConnectionString	RaceDay.Web	Azure Blob Storage connection string
AzureBlob__ContainerName	RaceDay.Web	Blob container used for uploads
Using user secrets (recommended for local development)
bash
cd RaceDay.Web
dotnet user-secrets init
dotnet user-secrets set "AzureBlob:ConnectionString" "<your-connection-string>"
dotnet user-secrets set "AzureBlob:ContainerName" "<your-container-name>"
dotnet user-secrets set "ApiBaseUrl" "https://localhost:5001/"
Using environment variables

In .NET configuration, a double underscore (__) represents a colon (:), so AzureBlob__ConnectionString maps to AzureBlob:ConnectionString.

Running Locally
Clone the repository:
bash
git clone https://github.com/Mma-sechaba/RaceDayPart-2.git
cd RaceDayPart-2
Restore and build:
bash
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
Set your configuration (see Configuration).
Start the API first:
bash
dotnet run --project <path-to-api-project>
In a second terminal, start the web app:
bash
dotnet run --project RaceDay.Web/RaceDay.Web.csproj
Open the URL printed in the web app's terminal output in your browser.

In Visual Studio: right-click the solution, choose Configure Startup Projects, select Multiple startup projects, and set both the API and RaceDay.Web to Start. Then press F5.

Running with Docker
Build the images
bash
# API image (root Dockerfile)
docker build --file Dockerfile --tag raceday-api:local .

# Web image
docker build --file RaceDay.Web/Dockerfile --tag raceday-web:local RaceDay.Web
Run the containers

Pass your configuration as environment variables. Never bake secrets into an image.

bash
docker run -d --name raceday-api -p 5000:8080 raceday-api:local

docker run -d --name raceday-web -p 5001:8080 \
  -e ApiBaseUrl="http://host.docker.internal:5000/" \
  -e AzureBlob__ConnectionString="<your-connection-string>" \
  -e AzureBlob__ContainerName="<your-container-name>" \
  raceday-web:local

Then browse to http://localhost:5001.

Using Docker Compose

If docker-compose.yml is present in the root:

bash
docker compose up --build

Stop and remove everything with:

bash
docker compose down
Running the Tests
bash
dotnet test RaceDay.sln --configuration Release

Or run the API test project directly:

bash
dotnet test Tests/RaceDay.Api.Tests.csproj --configuration Release
Continuous Integration

Two GitHub Actions workflows run on every push and pull request to main or master:

Workflow	File	What it does
Part 1 Validation	.github/workflows/part1-validation.yml	Confirms README.md, docs/API-Endpoint-Plan.md, docs/database.sql and an ERD file exist, and that the endpoint plan and SQL script contain the required content
.NET CI	.github/workflows/dotnet-ci.yml	Builds the solution and the MVC app, runs the API tests, and builds both Docker images

A green tick on the latest commit means every check passed.

Azure Blob Storage

The web application uses Azure Blob Storage to store uploaded files.

Create a Storage account in the Azure portal.
Create a Blob container (for example raceday-files).
Copy the connection string from Access keys in the storage account.
Supply it through user secrets or an environment variable as described in Configuration.

Keep the container access level private unless your brief requires otherwise. Do not commit connection strings or account keys.

Documentation

All Part 1 planning documents are in the docs/ folder:

API-Endpoint-Plan.md lists every endpoint with its method, route, purpose and access level
database.sql contains the database schema and sample data
ERD.png shows the entity relationship diagram
Troubleshooting
Problem	Likely cause and fix
MSB1003: Specify a project or solution file	Run the command from the repository root, where RaceDay.sln lives
Web app shows connection errors	The API is not running, or ApiBaseUrl is wrong
Blob upload fails	Check the connection string, container name and that the container exists
Docker build cannot find a file	Check the build context path: the root Dockerfile uses ., the web image uses RaceDay.Web
--no-build test step fails in CI	Make sure the test project is in the solution: dotnet sln add Tests/RaceDay.Api.Tests.csproj
Part 1 Validation fails	Confirm docs/ and README.md are at the repository root, not inside a nested folder
Author

Mma-sechaba IIE PROG6212, Programming 2B, Portfolio of Evidence
































