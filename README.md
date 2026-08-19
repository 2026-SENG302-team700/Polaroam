# Polaroam

Polaroam is a travel journal application for creating trips and posts, uploading and searching images, and viewing geotagged memories on a map.

## Authors

SENG302 Team 700: Sam Ladbrook, Shaun Souness, Shivam Pandya, Vlad Nistor, Shawn Neal, and jsa162.

## Dependencies

This project requires `dotnet` 10.0 or later, [click here to download the lastest release from Microsoft](https://dotnet.microsoft.com/en-us/download) as well as, `node` v24 (`npm` v11) or later [click here to download the latest node version](https://nodejs.org/en/download). You can also use your package manager (e.g., `apt`, `brew`, `chocolatey`).

## Technologies

This project makes use of several technologies that you will have to work with. Specifically, you will be creating a web application using C# ASP.Net for the _"back-end"_ (as a REST API) and Sveltekit for the _"front-end"_ (as a single-page-app).

For many of you, this will be the first time using C# and or Sveltekit, so we have included some helpful links below to get you started.

### Front-end

- [Sveltekit](https://kit.svelte.dev/docs) - Used for the front-end framework
- [Using Sveltekit as an SPA](https://khromov.se/the-missing-guide-to-understanding-adapter-static-in-sveltekit/) - An interesting article about using Sveltekit as a single page application
- [Bootstrap 5.3](https://getbootstrap.com/docs/5.3/getting-started/introduction) - Used within the Sveltekit front-end for styling
- [Bootstrap Icons 1.13.1](https://getbootstrap.com/docs/5.3/getting-started/introduction/) - Used within the Sveltekit front-end for icons

### Back-end

- [C# Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [C# vs Java](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/tips-for-java-developers)
- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/overview?view=aspnetcore-10.0) - Used for the back-end web framework
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - Used for database access
- [SQLite Documentation](https://sqlite.org/docs.html) - Used for development databases
- [PostgreSQL 18 Documentation](https://www.postgresql.org/docs/18/index.html) - Used for production databases (from Sprint 3 onwards)

For those of you who prefer more traditional tutorial style guides, we have included some links below:

- [Learn C#](https://learn.microsoft.com/en-us/collections/yz26f8y64n7k07) - Microsoft's official C# learning resources
- [Learn C# Video Series](https://www.youtube.com/playlist?list=PLdo4fOcmZ0oULFjxrOagaERVAMbmG20Xe) - A series of short youtube vidoes covering a number of C# development topics

## Integrated Development Environment (IDE)

Within SENG301 you are recommended to use Visual Studio Code as your IDE, Lab 1 includes more information about how to set up VS Code for C# projects. On top of the recommended plugins from SENG301, you should install the official [Sveltekit plugin](https://marketplace.visualstudio.com/items?itemName=svelte.svelte-vscode).

For those of you who have experience with IntelliJ IDEA, [Rider](https://www.jetbrains.com/rider/) by the same company is also a popular IDE for C#. You should install the [Svelte plugin](https://plugins.jetbrains.com/plugin/12375-svelte). Rider will also propose you to install other plugins for you, if needed.

## Quickstart Guide

### Building and running the project

#### 1 - Installing the dependencies

To install the C# dependencies, run the following command from the `SENG302.Api` project directory

```bash
dotnet restore
```

To install the Svelte dependencies, run the following command from the `SENG302.App` project directory

```bash
npm install
```

#### 2 - Setting environment variables (SMTP)

Local secrets can be placed in `SENG302.Api/appsettings.Local.json`. Obtain the development values from the pinned Team 700 Mattermost message; do not commit this file or secret values.

#### 3 - Running the project

To run the API, run the following command from the `SENG302.Api` project directory

```bash
dotnet run
```

The REST API should now be available on port 5000.

To run the app (UI), run the following commands from the `SENG302.App` project directory

```bash
npm run dev
```

The app should now be available on port 5173 and visible in your browser at <http://localhost:5173>.

### Building

From the repository root:

```bash
dotnet build SENG302.slnx
cd SENG302.App
npm run build
```

## Tests

Run all backend test suites from the repository root:

```bash
dotnet test SENG302.Api.Tests/SENG302.Api.Tests.csproj
```

Run an individual suite by replacing `Unit` with `Integration`, `Acceptance`, or `E2E`:

```bash
dotnet test SENG302.Api.Tests/SENG302.Api.Tests.csproj --filter "FullyQualifiedName~Unit"
```

Before running E2E tests for the first time, build the test project and install Playwright Chromium:

```bash
dotnet build SENG302.Api.Tests/SENG302.Api.Tests.csproj
pwsh SENG302.Api.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

Generate test coverage with:

```bash
dotnet test SENG302.Api.Tests/SENG302.Api.Tests.csproj --collect:"XPlat Code Coverage"
```

Check the frontend with:

```bash
cd SENG302.App
npm run check
```

## Local test accounts

The development database is seeded with these verified accounts:

| Account | Email | Password |
| --- | --- | --- |
| Admin | `admin@outstanding.com` | `Team700!` |
| John (sample trips and posts) | `john@example.com` | `P4$$word` |
| Jane (blank account) | `jane@example.com` | `P4$$word` |

## Configuration

Do not commit real credentials. Development values may be supplied through `appsettings.Local.json`; deployment values are supplied as environment variables.

| Setting | Purpose |
| --- | --- |
| `EmailSettings:Host`, `EmailSettings:Port`, `EmailSettings:FromEmail`, `EmailSettings:Password` | Local SMTP configuration |
| `EMAIL_HOST`, `EMAIL`, `EMAIL_PASS` | Deployed SMTP configuration; port defaults to 587 |
| `LLM_API_KEY` | API key used for AI image tagging |
| `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASS` | PostgreSQL connection settings required in staging and production |
| `ASPNETCORE_ENVIRONMENT` | Selects `Development`, `Staging`, or `Production` configuration |
| `PathBase` | Optional URL base path used when deployed below a sub-path |
