# RaceDay – Part 2: RESTful API Development

## Project Overview

RaceDay is a web-based event management system designed for the South African running, walking and cycling community.

Part 2 focuses on developing a RESTful API using C# and ASP.NET Core Web API. The API is intended to support event management, participant registration, categories, enrolments and results, building on the database design and endpoint plan from Part 1.

## Project Objectives

- Develop a RESTful API based on the approved Part 1 endpoint plan.
- Connect to Microsoft SQL Server using Entity Framework Core.
- Implement authentication and role-based authorisation for Organisers and Participants.
- Develop and validate API endpoints.
- Write and run automated tests.
- Configure GitHub Actions for automated builds and tests.

## User Roles

### Organiser

Organisers manage events and categories, view event enrolments and record participant results, subject to the implemented permissions.

### Participant

Participants can create an account, browse events, enrol in events, manage their profile and view their own enrolments and results, subject to the implemented functionality.

###

## Technologies

- C#
- ASP.NET Core Web API
- Entity Framework Core
- Microsoft SQL Server
- SQL Server Management Studio (SSMS)
- Visual Studio
- Swagger / OpenAPI
- GitHub and GitHub Actions

## Project Structure

- `Controllers/` – API request handling and endpoints.
- `Data/` – database context and data access.
- `DTOs/` – request and response data transfer objects.
- `Models/` – application data models.
- `Security/` – authentication and authorisation components.
- `Tests/` – automated tests.
- `Program.cs` – application configuration.
- `appsettings.json` – application configuration.
- `RaceDay.Api.csproj` – project dependencies and build settings.
- `.github/workflows/` – GitHub Actions workflows.

  THE CLASSES WE HAVE

## Prerequisites

- Visual Studio with the ASP.NET and web development workload.
- The .NET SDK version required by the project.
- Microsoft SQL Server.
- SQL Server Management Studio.
- Git, if working with a local clone.

## Setup and Configuration

1. Clone the repository or download the project source.
2. Open `RaceDay.sln` in Visual Studio.
3. Restore the NuGet packages.
4. Configure the SQL Server connection string for your local environment.
5. Configure the database schema and apply the required Entity Framework Core migrations, if applicable.
6. Build the solution.
7. Set the API project as the startup project and run it.
8. Open the Swagger URL provided by the running application to explore the available endpoints.

Do not commit database passwords, API keys or other secrets to the repository.

## Testing
<img width="983" height="796" alt="image" src="https://github.com/user-attachments/assets/4db32758-5285-45c8-ad65-a5d80f158b20" />


Automated tests are located in the `Tests/` directory. Run the tests using Visual Studio Test Explorer or the appropriate .NET test command.

The tests should cover successful requests, validation failures, missing resources and role-based access restrictions where applicable.

## Continuous Integration (CI/CD)

GitHub Actions workflows are stored in `.github/workflows/`.

**Build status:** Add a screenshot of a successful green workflow run here after verifying that the build and tests pass.

**Workflow results:** [ https://github.com/Mma-sechaba/RaceDayPart-2.git ] 

## API Documentation

The API should follow the approved Part 1 endpoint plan. Verify that the implemented HTTP methods, routes, request bodies, response codes and access restrictions match the plan.

Swagger / OpenAPI can be used to explore and test the running API where configured.

## Demonstration Video

**Part 2 video:** [Insert your unlisted YouTube video link after recording your demonstration.]

The demonstration should cover the running API, database integration, authentication, role restrictions, key endpoints, automated tests and CI/CD.

## Part 1 Reference Documents

The implementation should remain consistent with the approved Part 1:

- Entity Relationship Diagram (ERD).
- SQL Server database script.
- API endpoint plan.

## AI Assistance Disclosure

AI assistance was used to support documentation and development guidance. All suggestions must be reviewed, understood, tested and adapted by the student, who remains responsible for the submitted work.

Chatgpt was used.



