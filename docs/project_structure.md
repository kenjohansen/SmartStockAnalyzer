# Project Structure

## Solution Structure
```
SmartPortfolioAnalyzer
├── src/
│   ├── SmartPortfolioAnalyzer.Core/           # Core domain models and interfaces
│   ├── SmartPortfolioAnalyzer.Infrastructure/ # Database, caching, and external services
│   ├── SmartPortfolioAnalyzer.Services/       # Business services
│   │   ├── Portfolio/                        # Portfolio management service
│   │   └── Analysis/                         # Analysis and AI service
│   ├── SmartPortfolioAnalyzer.Web/           # Blazor WebAssembly frontend
│   └── SmartPortfolioAnalyzer.Tests/         # Test projects
│       ├── Core/                             # Core domain tests
│       └── Services/                         # Service layer tests
└── tests/                                    # Integration tests
```

## Project Dependencies

### Core
- Infrastructure (depends on Core)
- Services.Portfolio (depends on Core)
- Services.Analysis (depends on Core)
- Web (depends on Core, Services.Portfolio, Services.Analysis)
- Tests.Core (depends on Core)
- Tests.Services (depends on Services.Portfolio, Services.Analysis)

## Technology Stack
- .NET 8.0
- Blazor WebAssembly
- Entity Framework Core
- ML.NET
- Math.NET Numerics
- Redis
- SQL Server
