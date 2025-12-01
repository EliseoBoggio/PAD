namespace Muni.Infrastructure;

public sealed class DnrpaOptions
{
    public string BaseUrl { get; set; } = "";
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 15;
}


