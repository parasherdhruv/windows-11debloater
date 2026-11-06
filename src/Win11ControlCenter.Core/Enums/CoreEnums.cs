namespace Win11ControlCenter.Core.Enums;

public enum OperationRiskLevel
{
    None = 0,
    Safe = 1,
    Moderate = 2,
    Advanced = 3,
    Critical = 4
}

public enum OperationType
{
    Registry = 1,
    Service = 2,
    PackageRemoval = 3,
    Startup = 4,
    FileCleanup = 5,
    PowerShell = 6,
    PowerPlan = 7,
    SystemRestore = 8
}

public enum ExecutionStatus
{
    Pending = 0,
    Planning = 1,
    Approved = 2,
    InProgress = 3,
    Succeeded = 4,
    Failed = 5,
    Skipped = 6,
    RolledBack = 7
}

public enum ReversibilityTier
{
    FullyReversible = 1,
    ReversibleWithBackup = 2,
    ReinstallRequired = 3,
    Irreversible = 4
}

public enum PackageCategory
{
    SafeConsumer = 1,
    OptionalConsumer = 2,
    AdvancedCandidate = 3,
    ProtectedInfrastructure = 4,
    Unknown = 5
}

public enum ServiceCategory
{
    CoreWindows = 1,
    Security = 2,
    Networking = 3,
    Storage = 4,
    Audio = 5,
    Bluetooth = 6,
    Printing = 7,
    Gaming = 8,
    Telemetry = 9,
    ThirdParty = 10,
    OEM = 11
}

public enum StartupLocationType
{
    RegistryCurrentUser = 1,
    RegistryLocalMachine = 2,
    UserStartupFolder = 3,
    CommonStartupFolder = 4
}

