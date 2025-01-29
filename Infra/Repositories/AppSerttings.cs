using Microsoft.Extensions.Configuration;

namespace Infra.Repositories;

public class AppSettings
{
    public string StorePassword { get; set; } = string.Empty;
}

public class AppSettingsService
{
    private readonly IConfiguration _configuration;

    public AppSettingsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetStorePassword()
    {
        return _configuration["StorePassword"]!;
    }
}