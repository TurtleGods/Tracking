# AGENTS.md

## Project Overview

This project, named "Tracklix", is a .NET 10.0 Web API designed for user behavior tracking and analysis. It provides endpoints for receiving and querying event data. The system is built using ASP.NET Core and currently uses an in-memory store for event data. The project also includes a suite of xUnit tests.

The name "Tracklix" is a tribute to a data project by Willis Xu.

## Building and Running

### Building the Project

To build the project, you can use the .NET CLI.

```bash
dotnet build
```

### Running the Project

To run the Web API, you can use the .NET CLI. The API will be available at `https://localhost:5001` and `http://localhost:5000`.

```bash
dotnet run --project src/Mayo.Platform.Tracklix.WebAPI/Mayo.Platform.Tracklix.WebAPI.csproj
```

### Running the Tests

To run the tests, you can use the .NET CLI.

```bash
dotnet test
```

## Development Conventions

*   **API Design:** The project follows a standard RESTful API design, with controllers for different resources.
*   **Dependency Injection:** The project makes extensive use of dependency injection to manage services.
*   **Error Handling:** A global error handling middleware is used to provide a consistent error handling experience.
*   **Testing:** The project includes a dedicated test project that uses xUnit for unit and integration testing.
*   **API Documentation:** Swagger is used to provide API documentation. You can access the Swagger UI at `/swagger` when the project is running in development mode.
*   **Data Storage:** The project currently uses an in-memory event store. For production use, this should be replaced with a persistent storage solution.
