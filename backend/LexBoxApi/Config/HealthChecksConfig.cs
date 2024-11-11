namespace LexBoxApi.Config;

public class HealthChecksConfig
{
    public bool RequireFwHeadlessContainerVersionMatch { get; init; } = true;
    public bool RequireHealthyFwHeadlessContainer { get; init; } = true;
}
