using Microsoft.Extensions.Options;
using KatalonScheduler.Configuration;
using KatalonScheduler.Services.Interfaces;

namespace KatalonScheduler.Services;

public class TestScannerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KatalonOptions _options;
    private readonly ILogger<TestScannerService> _logger;

    public TestScannerService(
        IServiceScopeFactory scopeFactory,
        IOptions<KatalonOptions> options,
        ILogger<TestScannerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var katalonService = scope.ServiceProvider.GetRequiredService<IKatalonService>();
                await katalonService.ScanForProjectsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for Katalon projects");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(_options.ScanIntervalMinutes), 
                stoppingToken);
        }
    }
}