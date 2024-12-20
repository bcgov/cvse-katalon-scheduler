using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SQLite;
using KatalonScheduler.Data;
using KatalonScheduler.Configuration;
using KatalonScheduler.Services;
using KatalonScheduler.Services.Interfaces;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Options;

namespace KatalonScheduler;

internal class Program
{
   static void Main(string[] args)
   {
       try
       {
     
           var builder = WebApplication.CreateBuilder(args);

           // Set up the base directory where your app is running
           var baseDir = Directory.GetCurrentDirectory();
           Log.Information("Application base directory: {Path}", baseDir);

           // Configure logging
           var logPath = Path.Combine(baseDir, "Logs.db");
           Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
               .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
               .Enrich.FromLogContext()
               .WriteTo.SQLite(logPath)
               .WriteTo.Console()
               .CreateLogger();

            builder.Host.UseSerilog();

           Log.Information("Starting application...");
           Log.Information("Logs will be written to: {Path}", logPath);

           // Configuration
           builder.Configuration
               .SetBasePath(baseDir)
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables(); 

            // Helper function to safely truncate strings
            string SafeTruncate(string value, int maxLength = 4)
            {
                if (string.IsNullOrEmpty(value)) return "null";
                return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
            }

            // Log all values we're interested in
            Log.Information("Environment KATALON_API_KEY: {Value}", 
                SafeTruncate(Environment.GetEnvironmentVariable("KATALON_API_KEY")));
            Log.Information("Environment GIT_ACCESS_TOKEN: {Value}", 
                SafeTruncate(Environment.GetEnvironmentVariable("GIT_ACCESS_TOKEN")));
            Log.Information("Config KATALON_API_KEY: {Value}", 
                SafeTruncate(builder.Configuration["KATALON_API_KEY"]));
            Log.Information("Config GIT_ACCESS_TOKEN: {Value}", 
                SafeTruncate(builder.Configuration["GIT_ACCESS_TOKEN"]));

           // Connection strings setup
           var mainDbPath = Path.Combine(baseDir, "KatalonScheduler.db");
           var hangfireDbPath = Path.Combine(baseDir, "KatalonScheduler.Hangfire.db");

           var mainDbConnectionString = $"Data Source={mainDbPath};Mode=ReadWriteCreate;";
           var hangfireConnectionString = $"Data Source={hangfireDbPath};Mode=ReadWriteCreate;";

           // Database
           builder.Services.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlite(mainDbConnectionString));

           // Configure Options
           builder.Services.Configure<KatalonOptions>(options => {
               var apiKey = Environment.GetEnvironmentVariable("KATALON_API_KEY");
               Log.Information("Configuring KatalonOptions with API Key: {Value}", SafeTruncate(apiKey));
               
               options.ApiKey = apiKey;
               options.ScanIntervalMinutes = 15;
               options.ServerUrl = "https://testops.katalon.io";
           });

           builder.Services.Configure<GitOptions>(options => {
               var gitToken = Environment.GetEnvironmentVariable("GIT_ACCESS_TOKEN");
               Log.Information("Configuring GitOptions with Token: {Value}", SafeTruncate(gitToken));
               
               options.AccessToken = gitToken;
           });

           // Verify options configuration
           var sp = builder.Services.BuildServiceProvider();
           var katalonOptions = sp.GetService<IOptions<KatalonOptions>>()?.Value;
           var gitOptions = sp.GetService<IOptions<GitOptions>>()?.Value;
           
           Log.Information("Verified KatalonOptions.ApiKey: {Value}", SafeTruncate(katalonOptions?.ApiKey));
           Log.Information("Verified GitOptions.AccessToken: {Value}", SafeTruncate(gitOptions?.AccessToken));

           // Hangfire configuration
           builder.Services.AddHangfire((sp, config) => {
               var options = new SQLiteStorageOptions
               {
                   QueuePollInterval = TimeSpan.FromSeconds(1)
               };

               config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                   .UseSimpleAssemblyNameTypeSerializer()
                   .UseRecommendedSerializerSettings()
                   .UseSQLiteStorage(hangfireConnectionString, options);
           });

           builder.Services.AddHangfireServer(options => {
               options.WorkerCount = 1;
               options.Queues = new[] { "default" };
               options.ServerTimeout = TimeSpan.FromMinutes(5);
               options.ShutdownTimeout = TimeSpan.FromMinutes(5);
           });

           // Services
           builder.Services.AddScoped<IKatalonService, KatalonService>();
           builder.Services.AddScoped<ITestRunnerService, TestRunnerService>();
           builder.Services.AddHostedService<TestScannerService>();
           builder.Services.AddControllersWithViews();

           var app = builder.Build();

           Log.Information("Using database paths: Main DB: {MainDb}, Hangfire DB: {HangfireDb}, Logs: {LogsDb}",
               mainDbPath, hangfireDbPath, logPath);

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
           throw;
       }
       finally
       {
           Log.CloseAndFlush();
       }
   }
}