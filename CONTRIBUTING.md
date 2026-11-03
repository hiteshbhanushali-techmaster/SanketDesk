# Contributing to SanketDesk

Thank you for your interest in contributing to **SanketDesk**!

SanketDesk is a high-performance, pure Windows Forms desktop notification component suite and studio for .NET Framework 4.6.1 and .NET 8.0 Windows.

---

## Code of Conduct

We are committed to providing a welcoming, inclusive, and harassment-free experience for everyone. Please be respectful and constructive in all communications and reviews.

---

## Ground Rules & Non-Negotiables

Before submitting a Pull Request, ensure your proposed changes adhere to the project's core tenets:

1. **Pure Windows Forms Only:**
   - Zero WPF, MAUI, Avalonia, Electron, WebViews, or JavaScript dependencies.
2. **Zero External Runtime DLLs:**
   - Standalone libraries (`SanketDesk.Net8.dll`, `SanketDesk.NetFramework.dll`) must not introduce mandatory third-party runtime dependencies.
3. **Dual Runtime Support:**
   - Every core feature must compile and operate reliably under both `.NET 8.0 Windows` (`net8.0-windows`) and `.NET Framework 4.6.1` (`net461`).
4. **UI Thread Safety:**
   - Never access Windows Forms controls directly from worker threads without routing through `IUiDispatcher` / `ActiveToastManager`.
5. **No Per-Notification Timers:**
   - All animations, toast durations, and scheduling must use the shared coordinator clock. Active timers must stop completely (0% CPU) when idle.
6. **Pass All Tests:**
   - Any pull request must maintain 100% passing tests across all target frameworks.

---

## Development & Testing Workflow

### Prerequisites
- Windows 10/11 or Windows Server 2019+
- .NET 8.0 SDK (with Windows Desktop workload)
- Visual Studio 2022 (version 17.8+) or Visual Studio Code with C# Dev Kit
- MSBuild / .NET Framework 4.6.1+ Developer Pack

### Building the Solution
```powershell
# Restore dependencies
dotnet restore Notifier.sln

# Build Release configuration
dotnet build Notifier.sln -c Release
```

### Running the Test Suite
```powershell
# Execute automated tests
dotnet test tests/Notifier.Tests/Notifier.Tests.csproj -c Release
```

### Running the PropertyGrid Configurator Studio
```powershell
dotnet run --project samples/ConfiguratorSample/ConfiguratorSample.csproj -c Release -f net8.0-windows
```

---

## Submitting Pull Requests

1. Fork the repository on GitHub.
2. Create a topic branch: `git checkout -b feature/my-new-feature`.
3. Commit your changes with clear, descriptive commit messages.
4. Verify all tests pass locally: `dotnet test Notifier.sln -c Release`.
5. Push your branch to your fork: `git push origin feature/my-new-feature`.
6. Open a Pull Request against `main`. Provide a concise description of your changes and why they are necessary.
