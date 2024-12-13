using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SQLite;
using KatalonScheduler.Data;
using KatalonScheduler.Configuration;
using KatalonScheduler.Services;
using KatalonScheduler.Services.Interfaces;
using Serilog;
using Serilog.Events;

namespace KatalonScheduler;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs.db");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.SQLite(logPath)
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Application Starting Up at {Path}", logPath);

            builder.Host.UseSerilog();


            // Configuration
            builder.Services.Configure<KatalonOptions>(
                builder.Configuration.GetSection(KatalonOptions.Section));

            // Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


            // Hangfire
            builder.Services.AddHangfire((sp, config) => {
                var options = new SQLiteStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(1)
                };

                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSQLiteStorage("Filename=KatalonScheduler.Hangfire.db;", options);
            });
            builder.Services.AddHangfireServer();

            // Services
            builder.Services.AddScoped<IKatalonService, KatalonService>();
            builder.Services.AddScoped<ITestRunnerService, TestRunnerService>();
            builder.Services.AddHostedService<TestScannerService>();

            // MVC
            builder.Services.AddControllersWithViews();

            builder.Services.Configure<GitOptions>(builder.Configuration.GetSection("Git"));

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.UseHangfireDashboard();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

