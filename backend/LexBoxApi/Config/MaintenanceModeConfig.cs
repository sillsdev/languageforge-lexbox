namespace LexBoxApi.Config;

public class MaintenanceModeConfig
{
    public bool ReadOnlyMode { get; init; } = false;
    public string? MaintenanceMessage { get; set; } = null;
}
