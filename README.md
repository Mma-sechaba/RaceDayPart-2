# RaceDay – Part 2: RESTful API Development

## 1. Project Overview

RaceDay is a web-based event management system designed for the South African running, walking and cycling community.

Part 2 focuses on developing a RESTful API using ASP.NET Core Web API and C#. The API provides the backend functionality required to manage events, participants, categories, registrations and results. It builds on the database design and API endpoint plan established in Part 1.

## 2. Project Objectives

The objectives of Part 2 are to:

- Develop a RESTful API following the approved Part 1 endpoint plan.
- Connect the API to SQL Server using Entity Framework Core and the Code-First approach.
- Implement authentication and role-based access for Organisers and Participants.
- Provide endpoints for managing event-related information.
- Validate API requests and handle errors appropriately.
- Create unit tests for the API functionality.
- Configure GitHub Actions to build and test the project automatically.

## 3. User Roles

### Organiser

The Organiser is responsible for managing RaceDay events and related information. Organiser functionality is intended to include creating and updating events, managing categories, viewing registrations and recording participant results.

### Participant

The Participant uses the system to create an account, browse events, enrol in events, manage profile information and view personal registrations and results.

Access to protected operations must be restricted according to the user's role.

## 4. Technologies

- C#
- ASP.NET Core Web API
- Entity Framework Core
- Microsoft SQL Server
- SQL Server Management Studio (SSMS)
- Visual Studio
- Swagger / OpenAPI
- Git and GitHub
- GitHub Actions
- Unit testing framework: [confirm the framework used by the project]

## 5. Project Structure

The solution is organised into the following areas:

- `Controllers/` – API endpoints and request handling.
- `Data/` – database context and data access configuration.
- `DTOs/` – data transfer objects for API requests and responses.
- `Models/` – application entities and domain models.
- `Security/` – authentication and authorisation functionality.
- `Tests/` – automated tests.
- `Properties/` – project launch and development settings.
- `Program.cs` – application configuration and service registration.
- `appsettings.json` – application configuration; sensitive settings must be supplied securely.
- `RaceDay.Api.csproj` – API project dependencies and build settings.
- `.github/workflows/` – automated GitHub Actions workflows.

## 6. Prerequisites

Before running the project, install:

1. Visual Studio with the ASP.NET and web development workload.
2. The .NET SDK version required by the project.
3. Microsoft SQL Server.
4. SQL Server Management Studio.
5. Git.

## 7. Database Configuration

1. Start SQL Server and open SSMS.
2. Create or configure the RaceDay database according to the approved Part 1 database design.
3. Configure the API's database connection string using local development configuration or user secrets.
4. Ensure that the configured SQL Server instance and database name match the connection settings.
5. Apply the required Entity Framework Core migrations, if migrations are used by the project.
6. Verify that the database schema is consistent with the approved Part 1 ERD and SQL script.

Do not commit passwords or other secrets to GitHub.

## 8. Running the API

1. Clone the repository and open `RaceDay.sln` in Visual Studio.
2. Restore the NuGet packages.
3. Configure the database connection.
4. Build the solution.
5. Set the API project as the startup project.
6. Run the project using the configured HTTPS launch profile.
7. Open the Swagger URL shown in the application output to explore and test the available endpoints.

## 9. Testing

Run the automated tests from Visual Studio Test Explorer or with the appropriate `dotnet test` command.

Tests should verify the expected behaviour of the API, including successful requests, invalid input, missing resources and access restrictions where applicable.

Test results and build errors should be reviewed before committing and pushing changes.

## 10. Continuous Integration

The project uses GitHub Actions to automate build and test checks.

Workflow location: `.github/workflows/`

**CI build status:** [Add the actual successful workflow badge or screenshot after the workflow passes.]

GitHub Actions results: [Insert the repository's Actions page link.]

## 11. API Documentation

Swagger / OpenAPI is used to document and test the API endpoints.

The implemented routes should be checked against the approved Part 1 API endpoint plan. Document the HTTP methods, request requirements, response codes and role restrictions for each endpoint.

## 12. Demonstration Video

Part 2 demonstration: [Insert the unlisted YouTube video link after recording the demonstration.]

The video should demonstrate the running API, database integration, authentication and role restrictions, key endpoints, automated tests and CI/CD workflow.

## 13. Version Control

GitHub is used to maintain the project source code and document development progress. Commits should represent meaningful implementation milestones, such as database integration, endpoint development, validation, authentication, testing, documentation and CI improvements.

## 14. AI Assistance Disclosure

AI tools were used for assistance with planning, documentation and/or development. All generated suggestions were reviewed and adapted by the student, who is responsible for understanding, testing and verifying the submitted work.

## 15. Part 1 Reference Documents

The Part 2 implementation should remain consistent with the approved Part 1 documents:

- Entity Relationship Diagram (ERD).
- SQL Server database script.
- API endpoint plan.

These documents define the intended database structure, resources, routes and access requirements for the RaceDay system.
