# Contributing to Win11 Control Center

Thank you for your interest in contributing to **Win11 Control Center**! 

Win11 Control Center is a safety-first, modular Windows 11 debloater, optimizer, and system governance suite built in C# and .NET 10 LTS. Because this application interacts directly with Windows system internals, the Windows Registry, the Service Control Manager, and the AppX/MSIX package repository, **all contributions must strictly adhere to our safety architecture and engineering principles**.

---

## Table of Contents

- [Core Principles & Safety Rules](#core-principles--safety-rules)
- [Absolute Boundaries & Forbidden Changes](#absolute-boundaries--forbidden-changes)
- [Development Environment Setup](#development-environment-setup)
- [Solution Architecture](#solution-architecture)
- [Coding Standards & Conventions](#coding-standards--conventions)
- [How to Add a New Feature or Tweak](#how-to-add-a-new-feature-or-tweak)
- [Testing Guidelines](#testing-guidelines)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Security Vulnerability Reporting](#security-vulnerability-reporting)

---

## Core Principles & Safety Rules

1. **Safety Before Performance**: A faster PC is useless if it is unstable, unbootable, or vulnerable. Any feature that risks system stability, disables Windows Update, or turns off core security protections will be rejected immediately.
2. **Reversibility by Default**: Every mutable action must capture initial state before execution and support automated rollback. If an action is irreversible (such as emptying the Recycle Bin), it must be clearly classified as such and require explicit user consent.
3. **Transparency Before Automation**: Never apply changes silently. Every operation must generate an `OperationPlan` that details exact registry paths, service names, or package IDs before execution, and support dry-run simulation.
4. **Zero Arbitrary Script Execution**: Never evaluate untrusted strings, concatenate user input into PowerShell commands, or run arbitrary scripts. All operations must route through strongly-typed, allow-listed internal handlers.
5. **No Telemetry / Phoning Home**: Win11 Control Center is 100% local. No background analytics, telemetry, or remote tracking network calls are permitted.
6. **Strict Architectural Separation**: System mutations and Windows APIs belong exclusively in `Win11ControlCenter.SystemEngine`. ViewModels must never invoke Win32 APIs, registry keys, or cmdlets directly.

---

## Absolute Boundaries & Forbidden Changes

The following anti-patterns are strictly forbidden:

- **Disabling Windows Update (`wuauserv`)**: Leaving Windows unpatched invites security vulnerabilities and breaks Windows Store servicing.
- **Disabling Windows Defender / SmartScreen**: Security degradation is never acceptable as a "performance optimization."
- **Disabling Memory Compression or Pagefile**: Causes out-of-memory errors and blue-screen crashes under memory pressure.
- **Disabling Core Isolation / VBS / Secure Boot / TPM**: Compromises hardware-enforced platform security.
- **Purging WinSxS / Component Store manually**: Corrupts component store manifests and causes future cumulative update failures.
- **Modifying Hosts File with Giant Blocklists**: Degrades DNS resolution performance and breaks modern networking.
- **Targeting Protected Core Packages or Services**: Modifying or removing packages in `ProtectedPackages.cs` or services in `CoreWindowsServices.cs` is strictly prevented by both code and automated tests.

---

## Development Environment Setup

### Prerequisites
- **Operating System**: Windows 11 x64 (Build 22000 or higher).
- **.NET SDK**: [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or higher.
- **IDE**:
  - Visual Studio 2022 (v17.12+) with *.NET Desktop Development* workload.
  - JetBrains Rider 2024.3+.
  - Visual Studio Code with the *C# Dev Kit* extension.
- **PowerShell**: Windows PowerShell 5.1 (built into Windows 11) or PowerShell 7+.
- **Inno Setup** (Optional, for building the installer): Inno Setup 6.x.

### Clone and Build

```powershell
# Clone the repository
git clone https://github.com/parasherdhruv/windows-11debloater.git
cd windows-11debloater

# Configure .NET path if using portable installation
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"

# Build solution
dotnet build Win11ControlCenter.slnx

# Run the WPF application
dotnet run --project src/Win11ControlCenter.App
```

---

## Solution Architecture

The solution uses a modular 7-project structure:

```
Win11ControlCenter.slnx
├── src/
│   ├── Win11ControlCenter.Core             # Domain models, contracts, enums, rules & safety validators
│   ├── Win11ControlCenter.Infrastructure   # Storage (JSON), backups, settings & structured logging
│   ├── Win11ControlCenter.SystemEngine     # System APIs, Registry, Service Control Manager, DISM, Handlers
│   ├── Win11ControlCenter.PowerShell       # Isolated PowerShell runspace interop with parameter validation
│   └── Win11ControlCenter.App              # WPF presentation layer, MVVM (CommunityToolkit), themes & views
└── tests/
    ├── Win11ControlCenter.SafetyTests      # 91 unit & theory tests enforcing safety boundaries
    └── Win11ControlCenter.UnitTests        # 54+ unit tests, STA XAML tests, and Full App Automation suite
```

### Dependency Flow
```
Win11ControlCenter.App ──> Win11ControlCenter.Core
                       ──> Win11ControlCenter.Infrastructure
                       ──> Win11ControlCenter.SystemEngine
                       ──> Win11ControlCenter.PowerShell

Win11ControlCenter.SystemEngine ──> Win11ControlCenter.Core
                                ──> Win11ControlCenter.PowerShell

Win11ControlCenter.Infrastructure ──> Win11ControlCenter.Core
Win11ControlCenter.PowerShell ──> Win11ControlCenter.Core
```

---

## Coding Standards & Conventions

- **Language & Runtime**: C# 13 on .NET 10 LTS.
- **Nullable Reference Types**: Enabled across all projects (`<Nullable>enable</Nullable>`). Zero compiler warnings with nullable references.
- **Pattern Matching & Modern C#**: Use pattern matching, file-scoped namespaces, record types for immutable data, and collection expressions (`[...]`).
- **MVVM Pattern**: ViewModels inherit from `ViewModelBase` (derived from `ObservableObject` in `CommunityToolkit.Mvvm`).
  - Use `[ObservableProperty]` and `[RelayCommand]` source generators.
  - Never place direct UI dispatcher calls or system interop in ViewModels.
- **Asynchronous Execution**:
  - All I/O, registry scans, and system operations must be asynchronous (`Task<T>`) and accept a `CancellationToken`.
  - Always respect cancellation tokens to ensure clean UI responsiveness.
- **Dependency Injection**: Register all services and ViewModels in `App.xaml.cs` via `Microsoft.Extensions.DependencyInjection`.

---

## How to Add a New Feature or Tweak

When introducing a new tweak, optimization, or management capability:

1. **Define the Domain Model**: Add strongly-typed representations in `Win11ControlCenter.Core/Models/`.
2. **Check Safety Rules**: Ensure target registry keys or services are evaluated against `SafetyValidator.cs`. If the target could harm the OS, add appropriate shields.
3. **Implement the System Handler**: Place the execution logic in `Win11ControlCenter.SystemEngine/Handlers/`. Ensure it:
   - Captures pre-apply state for rollback.
   - Generates emergency `.reg` commands or snapshot records.
   - Verifies postcondition state.
   - Supports dry-run simulation mode.
4. **Expose Through Service Layer**: Add interface methods in `CoreServicesInterfaces.cs` and implement in the respective service.
5. **Connect to ViewModel**: Wire the command into the appropriate ViewModel with dry-run support and user feedback.
6. **Add Automated Safety Tests**: Write safety tests in `Win11ControlCenter.SafetyTests` proving that invalid or malicious inputs are blocked.
7. **Add Unit/Automation Tests**: Verify normal execution, dry-run mode, and rollback in `Win11ControlCenter.UnitTests`.

---

## Testing Guidelines

Every pull request must maintain a **100% test pass rate** across all 145 automated tests.

### Running Tests Locally

Run the complete test suite via dotnet CLI:
```powershell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"; $env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
dotnet test
```

Or run the automated safety & unit test runner:
```powershell
powershell -ExecutionPolicy Bypass -File run-automation-tests.ps1
```

Or run the live 15-feature end-to-end automation walkthrough:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-full-automation.ps1
```

### Testing Rules
- **Mock Destructive APIs**: Never write unit tests that delete real files or modify live system services on the developer's workstation. Use mocked interfaces or disposable sandbox paths.
- **Validate Safety Boundaries**: Any new package, registry key, or service touched must have an explicit test in `Win11ControlCenter.SafetyTests`.
- **STA UI Instantiation**: Ensure any new XAML view is added to `XamlViewInstantiationTests.cs` to verify that XAML parsing and data templates compile without runtime errors.

---

## Submitting a Pull Request

1. **Fork the Repository**: Create your own fork and work on a feature branch (e.g., `feature/winget-update-check` or `fix/service-display-name`).
2. **Keep Commits Clean**: Use clear, imperative commit messages (e.g., `feat(debloater): add safe recommendations tab`, `fix(services): protect wuauserv from accidental disable`).
3. **Verify All Tests Pass**: Ensure `dotnet test` reports zero failures.
4. **Update Documentation**: Update relevant documentation files in `docs/` and `README.md` if your PR modifies behavior or adds new options.
5. **Open a Pull Request**: Provide a detailed description of your changes, including rationale, screenshots (for UI changes), and automated test results.

---

## Security Vulnerability Reporting

If you discover a security vulnerability or a safety bypass in Win11 Control Center:
- **Do not open a public issue.**
- Submit a private report via GitHub Security Advisories or contact the maintainers directly.
- We take safety vulnerabilities seriously and will address verified bypasses promptly.
