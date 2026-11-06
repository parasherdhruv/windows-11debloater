# Win11 Control Center

A modern, modular, and safety-first Windows 11 debloater, optimization dashboard, and system configuration center built in C# and .NET 10 LTS with WPF and MVVM.

![Platform](https://img.shields.io/badge/Platform-Windows%2011%20x64-blue)
![Framework](https://img.shields.io/badge/.NET-10.0%20LTS-purple)
![Architecture](https://img.shields.io/badge/Architecture-WPF%20%7C%20MVVM-green)
![License](https://img.shields.io/badge/License-MIT-orange)

---

## Key Features

1. **Intelligent Dashboard**: Real-time hardware specifications, Windows edition/build, pending reboot status, and genuine CPU/RAM utilization.
2. **Safe App Debloater**: Differentiates current user profile installs from provisioned packages. Hardcoded protected package shields prevent breaking Windows Shell, Store, or authentication.
3. **Startup Manager**: Non-destructive disabling of startup applications from Run keys, Startup folders, and `StartupApproved`.
4. **Windows Services Manager**: Comprehensive service viewer with strict preservation rules for core Windows networking, audio, RPC, and security services.
5. **Privacy & Telemetry**: Manages documented Windows privacy settings (Advertising ID, Diagnostic Data, Activity History) using verified registry/group policy mechanisms.
6. **Windows & Gaming Optimization**: Safe visual effects tuning, Explorer preferences, Power plans, and Game Mode alignment.
7. **Safe System Cleaner**: Disposable cache audit (Temp, Recycle Bin, Thumbnails) with pre-deletion byte calculations and a strict exclusion list.
8. **10-Step Safe Execution Engine**: Discover $\to$ Analyze $\to$ Plan $\to$ Preview $\to$ Backup $\to$ Approve $\to$ Execute $\to$ Verify $\to$ Log $\to$ Restore Ready.
9. **Restore Center**: Integrated System Restore checkpoints, automated registry snapshots (.reg & JSON), and service/startup state rollback.
10. **Activity Logging**: Full local audit trail with zero external telemetry.

---

## Architecture

- **`Win11ControlCenter.Core`**: Domain models, interfaces, safety validation rules, and constants.
- **`Win11ControlCenter.Infrastructure`**: Storage (JSON/SQLite), structured audit logging, backup management.
- **`Win11ControlCenter.SystemEngine`**: Direct Windows API interop (Registry, Service Control Manager, DISM, Performance Counters) and Safe Execution Pipeline.
- **`Win11ControlCenter.PowerShell`**: Isolated Windows PowerShell 5.1 hosted runspaces with strict allow-listed command dispatching.
- **`Win11ControlCenter.App`**: WPF modern presentation layer with dark/light Fluent styling, MVVM with `CommunityToolkit.Mvvm`, and Dependency Injection.

---

## Building and Running

### Prerequisites
- Windows 11 x64 (Build 22000 or later).
- .NET 10 SDK (included or installed via `dotnet-install.ps1`).

### Build Solution
```powershell
dotnet build Win11ControlCenter.sln -c Release
```

### Run Unit Tests
```powershell
dotnet test tests/Win11ControlCenter.UnitTests
dotnet test tests/Win11ControlCenter.SafetyTests
```

### Launch Application
```powershell
dotnet run --project src/Win11ControlCenter.App
```

