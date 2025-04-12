# Development Environment Setup

## Prerequisites

### Required Tools
- .NET 8.0 SDK
- Visual Studio Code (Recommended)
- Docker Desktop
- Git
- kubectl (included with Docker Desktop)
- Helm (for Kubernetes package management)

### Recommended Extensions
- C# for Visual Studio Code
- GitLens
- Docker
- YAML
- Markdown
- SonarLint
- Code Spell Checker

## Environment Variables

Create a `.env` file in the project root with the following variables:

```plaintext
# Database Configuration
DB_CONNECTION_STRING=Server=tcp:localhost,1433;Database=SmartPortfolioAnalyzer;User Id=sa;Password=YourStrong@Password123;

# Redis Configuration
REDIS_CONNECTION_STRING=localhost:6379,password=YourSecureRedisPassword123

# Application Settings
APP_ENVIRONMENT=Development
APP_LOG_LEVEL=Debug
```

## Development Guidelines

### Code Style
- Use 4-space indentation
- Maximum line length: 120 characters
- Use var for local variables when type is obvious
- Use async/await for asynchronous operations
- Use expression-bodied members where appropriate

### Naming Conventions
- Use PascalCase for class names
- Use camelCase for method and property names
- Use PascalCase for enum members
- Use UPPER_SNAKE_CASE for constants
- Use _camelCase for private fields

### Documentation
- All public and protected members must be documented
- Use XML documentation comments
- Include examples where appropriate
- Document exception conditions

### Testing
- Write unit tests for all business logic
- Use integration tests for database operations
- Use performance tests for critical paths

### Version Control
- Commit messages should follow conventional commits format
- Use feature branches for new development
- Follow Gitflow workflow for releases

## Build and Run

### Build Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run Application
```bash
dotnet run --project src/SmartPortfolioAnalyzer.Web/SmartPortfolioAnalyzer.Web.csproj
```

### Docker Commands
```bash
# Build containers
docker compose build

# Start containers
docker compose up

# Stop containers
docker compose down
```
