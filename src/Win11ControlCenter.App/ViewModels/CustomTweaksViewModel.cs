using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.SystemEngine.Handlers;

namespace Win11ControlCenter.App.ViewModels;

public partial class CustomTweaksViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly IOperationExecutor _operationExecutor;
    private readonly string _storageFile;

    private string _newName = string.Empty;
    private string _newDescription = string.Empty;
    private string _newCategory = "Custom";
    private string _newHive = "HKCU";
    private string _newKeyPath = @"Software\MyCustomTweak";
    private string _newValueName = "Setting";
    private string _newValueType = "DWord";
    private string _newRecommendedValue = "1";
    private string _newDefaultValue = "0";
    private string _validationError = string.Empty;

    public ObservableCollection<OptimizationTweak> CustomTweaks { get; } = new();
    public ObservableCollection<string> AvailableHives { get; } = new() { "HKCU", "HKLM" };
    public ObservableCollection<string> AvailableTypes { get; } = new() { "DWord", "String", "QWord" };

    public string NewName
    {
        get => _newName;
        set => SetProperty(ref _newName, value);
    }

    public string NewDescription
    {
        get => _newDescription;
        set => SetProperty(ref _newDescription, value);
    }

    public string NewCategory
    {
        get => _newCategory;
        set => SetProperty(ref _newCategory, value);
    }

    public string NewHive
    {
        get => _newHive;
        set => SetProperty(ref _newHive, value);
    }

    public string NewKeyPath
    {
        get => _newKeyPath;
        set => SetProperty(ref _newKeyPath, value);
    }

    public string NewValueName
    {
        get => _newValueName;
        set => SetProperty(ref _newValueName, value);
    }

    public string NewValueType
    {
        get => _newValueType;
        set => SetProperty(ref _newValueType, value);
    }

    public string NewRecommendedValue
    {
        get => _newRecommendedValue;
        set => SetProperty(ref _newRecommendedValue, value);
    }

    public string NewDefaultValue
    {
        get => _newDefaultValue;
        set => SetProperty(ref _newDefaultValue, value);
    }

    public string ValidationError
    {
        get => _validationError;
        set => SetProperty(ref _validationError, value);
    }

    public IAsyncRelayCommand AddTweakCommand { get; }
    public IAsyncRelayCommand<OptimizationTweak> DeleteTweakCommand { get; }
    public IAsyncRelayCommand<OptimizationTweak> ToggleTweakCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public CustomTweaksViewModel(ILoggingService logger, IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _operationExecutor = operationExecutor;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "Win11ControlCenter");
        Directory.CreateDirectory(dir);
        _storageFile = Path.Combine(dir, "custom_tweaks.json");

        AddTweakCommand = new AsyncRelayCommand(AddTweakAsync);
        DeleteTweakCommand = new AsyncRelayCommand<OptimizationTweak>(DeleteTweakAsync);
        ToggleTweakCommand = new AsyncRelayCommand<OptimizationTweak>(ToggleTweakAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public override async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        CustomTweaks.Clear();
        var list = LoadCustomTweaksFromDisk();

        foreach (var tweak in list)
        {
            var current = RegistryOperationHandler.ReadCurrentValue($"{tweak.RegistryHive}\\{tweak.KeyPath}", tweak.ValueName);
            tweak.CurrentValue = current;
            if (current != null && tweak.RecommendedValue != null)
            {
                tweak.IsApplied = current.ToString() == tweak.RecommendedValue.ToString();
            }
            else
            {
                tweak.IsApplied = false;
            }

            CustomTweaks.Add(tweak);
        }

        await Task.CompletedTask;
    }

    private async Task AddTweakAsync()
    {
        ValidationError = string.Empty;

        if (string.IsNullOrWhiteSpace(NewName))
        {
            ValidationError = "Tweak name cannot be empty.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewKeyPath))
        {
            ValidationError = "Registry key path cannot be empty.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewValueName))
        {
            ValidationError = "Value name cannot be empty.";
            return;
        }

        var fullTarget = $"{NewHive}\\{NewKeyPath.Trim('\\')}";
        var testItem = new OperationItem
        {
            Name = NewName,
            OperationType = OperationType.Registry,
            Target = fullTarget,
            RecommendedValue = NewRecommendedValue
        };

        // STEP 2: Safety Validation
        var (isValid, reason) = SafetyValidator.ValidateOperation(testItem);
        if (!isValid)
        {
            ValidationError = $"Safety Block: {reason}";
            _logger.LogWarning($"Rejected custom tweak '{NewName}': {reason}", "CustomTweaks");
            return;
        }

        object recommendedParsed = ParseValue(NewRecommendedValue, NewValueType);
        object defaultParsed = ParseValue(NewDefaultValue, NewValueType);

        var tweak = new OptimizationTweak
        {
            Id = "custom-" + Guid.NewGuid().ToString("N")[..8],
            Name = NewName,
            Description = string.IsNullOrWhiteSpace(NewDescription) ? "Custom user-defined registry optimization" : NewDescription,
            Category = string.IsNullOrWhiteSpace(NewCategory) ? "Custom" : NewCategory,
            RegistryHive = NewHive,
            KeyPath = NewKeyPath.Trim('\\'),
            ValueName = NewValueName,
            ValueType = NewValueType,
            RecommendedValue = recommendedParsed,
            DefaultValue = defaultParsed,
            RiskLevel = OperationRiskLevel.Safe
        };

        CustomTweaks.Add(tweak);
        SaveCustomTweaksToDisk();
        _logger.LogInfo($"Added custom registry tweak: {tweak.Name} -> {fullTarget}", "CustomTweaks");

        // Clear input form
        NewName = string.Empty;
        NewDescription = string.Empty;

        await RefreshAsync();
    }

    private async Task DeleteTweakAsync(OptimizationTweak? tweak)
    {
        if (tweak == null) return;

        CustomTweaks.Remove(tweak);
        SaveCustomTweaksToDisk();
        _logger.LogInfo($"Deleted custom tweak: {tweak.Name}", "CustomTweaks");
        await Task.CompletedTask;
    }

    private async Task ToggleTweakAsync(OptimizationTweak? tweak)
    {
        if (tweak == null) return;

        var targetVal = tweak.IsApplied ? tweak.DefaultValue : tweak.RecommendedValue;

        var item = new OperationItem
        {
            Name = tweak.Name,
            Description = tweak.Description,
            Category = "Custom Tweak",
            RiskLevel = tweak.RiskLevel,
            OperationType = OperationType.Registry,
            Reversibility = ReversibilityTier.FullyReversible,
            Target = $"{tweak.RegistryHive}\\{tweak.KeyPath}",
            OriginalValue = tweak.CurrentValue,
            RecommendedValue = targetVal,
            ValueType = tweak.ValueType,
            RequiresAdmin = tweak.RequiresAdmin,
            RollbackSupported = true,
            Metadata = new Dictionary<string, string>
            {
                { "ValueName", tweak.ValueName }
            }
        };

        var plan = await _operationExecutor.GeneratePlanAsync(new[] { item }, "Custom Tweak Apply", isDryRun: false);
        await _operationExecutor.ExecutePlanAsync(plan);

        await RefreshAsync();
    }

    private List<OptimizationTweak> LoadCustomTweaksFromDisk()
    {
        try
        {
            if (File.Exists(_storageFile))
            {
                var json = File.ReadAllText(_storageFile);
                return JsonSerializer.Deserialize<List<OptimizationTweak>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed loading custom tweaks", ex, "CustomTweaks");
        }
        return new();
    }

    private void SaveCustomTweaksToDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(CustomTweaks.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storageFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed saving custom tweaks", ex, "CustomTweaks");
        }
    }

    private static object ParseValue(string value, string type)
    {
        return type.ToLowerInvariant() switch
        {
            "dword" => int.TryParse(value, out var d) ? d : 0,
            "qword" => long.TryParse(value, out var q) ? q : 0L,
            _ => value
        };
    }
}
