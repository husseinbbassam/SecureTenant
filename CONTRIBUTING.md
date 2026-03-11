# Contributing to SecureTenant

Thank you for your interest in contributing to **SecureTenant**! This document provides guidelines and instructions to help you get started.

## Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md). Please read it before contributing.

## How Can I Contribute?

### Reporting Bugs

Before creating a bug report, please search the [existing issues](https://github.com/husseinbbassam/SecureTenant/issues) to avoid duplicates.

When creating a bug report, include:
- A clear and descriptive title
- Steps to reproduce the problem
- Expected behavior vs. actual behavior
- .NET SDK version and OS details
- Relevant log output or error messages

### Suggesting Enhancements

Enhancement suggestions are welcome! Please open an issue using the **Feature Request** template and describe:
- The problem you're trying to solve
- Your proposed solution
- Any alternatives you've considered

### Submitting Pull Requests

1. **Fork** the repository and create your branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Install dependencies** and verify the build:
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Write tests** for your changes. All new functionality should include relevant tests in the `tests/SecureTenant.Tests` project.

4. **Run the tests** and ensure they all pass:
   ```bash
   dotnet test
   ```

5. **Follow the coding style** — the project uses `.editorconfig` to enforce consistent formatting. Your IDE should pick this up automatically.

6. **Commit your changes** using a descriptive commit message:
   ```bash
   git commit -m "feat: add support for JWT key rotation"
   ```

7. **Push** your branch and open a Pull Request against `main`.

### Pull Request Checklist

- [ ] My code builds without errors or warnings
- [ ] I have added or updated tests for my changes
- [ ] All existing tests still pass
- [ ] I have updated documentation if needed (README, XML doc comments)
- [ ] I have followed the existing coding style

## Development Setup

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A code editor such as [Visual Studio](https://visualstudio.microsoft.com/), [VS Code](https://code.visualstudio.com/), or [JetBrains Rider](https://www.jetbrains.com/rider/)

### Getting Started

```bash
# Clone your fork
git clone https://github.com/<your-username>/SecureTenant.git
cd SecureTenant

# Restore packages
dotnet restore

# Build the solution
dotnet build

# Run the tests
dotnet test

# Run the authorization server locally
cd src/SecureTenant.Auth
dotnet run
```

The Auth server will be available at `http://localhost:5200`.

### Project Structure

```
SecureTenant/
├── src/
│   ├── SecureTenant.Core/           # Domain models, interfaces, and options
│   ├── SecureTenant.Infrastructure/ # EF Core data access, providers, services, and DI extensions
│   ├── SecureTenant.Auth/           # OpenIddict Authorization Server host
│   └── SecureTenant.ProtectedApi/   # Sample protected API
└── tests/
    └── SecureTenant.Tests/          # Integration tests
```

## Coding Conventions

- Use **C# 13** features where appropriate
- Follow Microsoft's [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Write XML doc comments (`///`) on all public types and members
- Prefer `async/await` over raw `Task`-based code
- Use `string.Empty` instead of `""`
- Prefer `is null` / `is not null` over `== null` / `!= null`

## Adding a New Tenant Resolution Strategy

SecureTenant supports custom tenant resolution by implementing `ITenantProvider`:

```csharp
public class ClaimsTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentTenantId()
    {
        return _httpContextAccessor.HttpContext?
            .User.FindFirstValue("tenant_id");
    }
}
```

Register your implementation in DI and it will be picked up by the rest of the system.

## Questions?

Feel free to open a [Discussion](https://github.com/husseinbbassam/SecureTenant/discussions) if you have questions that don't fit into a bug report or feature request.
