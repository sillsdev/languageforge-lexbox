namespace FwLiteWeb;

public class FwLiteWebConfig
{
    public bool CorsAllowAny { get; init; } = false;
    public bool OpenBrowser { get; init; } = true;
    public string? LogFileName { get; init; } = null;
}
