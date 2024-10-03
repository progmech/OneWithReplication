// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using OneWithReplication;
using OneWithReplication.Services;
using OneWithReplication.Settings;
using Serilog;

IConfigurationRoot? config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json").Build();

var dbOptions = config.GetSection(DatabaseSettings.DatabaseOptionsName).Get<List<DatabaseSettings>>() ?? [];
var backupOptions = Basis.GetBackupSettings(dbOptions);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs\\log-.txt", rollingInterval: RollingInterval.Month)
    .CreateLogger();

var replicator = new Replicator(
    backupOptions,
    new EmailService(backupOptions.EmailSettings),
    new CloudService(backupOptions));
replicator.Replicate();

Log.CloseAndFlush();