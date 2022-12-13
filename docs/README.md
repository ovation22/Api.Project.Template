# A .NET Project Template

Template repository for a .NET Web Api project.

## Getting Started

1. Clone this Repository
1. Install Project Template
1. Create Project with `dotnet new`

### Clone this Repository

Clone or download this repository.

### Install Project Template

Then run the following command to install this project template:

```powershell
dotnet new install ./
```

You should now see the `api-project` template installed successfully.

### Install using `dotnet new`

Run the following command to create the solution structure in a subfolder named `Your.ProjectName`:

```powershell
dotnet new api-project -o Your.ProjectName
```

Optionally, you may choose to included additional parameters to better fill out your new project:

```powershell
dotnet new api-project -o Your.ProjectName \
    --appInsights "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://eastus2-0.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus2.livediagnostics.monitor.azure.com/"
```

## Solution Structure

Included in the solution:

- [x] .NET 6 Web Api
- [x] Logging
- [ ] Event Hub Producer client
- [ ] Service Bus client
- [ ] Repository
- [ ] Http client
- [ ] Unit Tests
- [ ] Integration Tests
- [ ] Benchmark Tests
- [ ] Architectural Tests

### Core Project

The Core project is the center of the Clean Architecture design, and all other project dependencies should point toward it. As such, it has very few external dependencies. The Core project should include things like:

- Entities
- DTOs
- Interfaces
- Domain Services

### Infrastructure Project

Most of your application's dependencies on external resources should be implemented in classed defined in the Infrastructure project. These classed should implement interfaces defined in Core. The Infrastructure project should include things like:

- Http clients
- Repositories
- Event Hub Producer clients
- Service Bus clients

### Api Project

The entry point of the application is the Api project. The Api project should include things like:

- Controllers

### Test Projects

Test projects are organized based on the kind of test (unit, integration, benchmark, etc.).

#### Unit Tests

Unit tests provide a way to verify and validate functionality of individual methods/components/features. This project contains example tests using [xUnit](https://xunit.net/).

#### Integration Tests

[Integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests) provides a way to ensure that an application's components function correctly at each level.

#### Architectural Tests

Architectural rules can be enforced using [NetArchTest](https://github.com/BenMorris/NetArchTest), a fluent API for .NET Standard that can enforce architectural rules within unit tests.

#### Benchmark Tests

Benchmark testing is provided by [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet), a powerful .NET library for creating and executing benchmark tests.

```powershell
dotnet run --project ./tests/Api.Project.Template.Tests.Benchmark -c Release
```
