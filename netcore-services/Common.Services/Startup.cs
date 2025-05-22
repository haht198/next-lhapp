using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Threading;
using Common.Services.Core;
using Common.Services.Model.Uploader;
using Common.Services.Services;
using Common.Services.Services.Implements;
using Common.Services.Static;
using Common.Services.Static.Logger;
using Common.Services.Static.Queue;
using Common.Services.Static.Queue.Core;
using Common.Services.Worker;

namespace Common.Services
{
    public class Startup
    {
        private readonly IHostBuilder _builder;
        private IServiceCollection _services;

        public Startup()
        {
            if (!ServiceSetting.IsValid())
            {
                Console.WriteLine("Service settings invalid");
                Environment.Exit(-1);
                return;
            }

            LoggerConfig.AddConsoleLog();
            if (ProgramArguments.Env != "debug" && !string.IsNullOrEmpty(ServiceSetting.Logging_ElasticSearch_ApiKey) && !string.IsNullOrEmpty(ServiceSetting.Logging_ElasticSearch_BulkInsertEndpoint))
            {
                LoggerConfig.AddElasticSearchLog($"kelvin-service-{ProgramArguments.Service.ToLower()}", ServiceSetting.Logging_ElasticSearch_BulkInsertEndpoint, ServiceSetting.Logging_ElasticSearch_ApiKey, "kelvin");
            }


            var assembly = Assembly.GetEntryAssembly();
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            var assemblyVersion = assembly.GetName().Version.ToString();
            var versionSuffix = version.Contains("-") ? version.Split('-')[1] : "release";
            Console.WriteLine($"Version Suffix: {versionSuffix}");
            Console.WriteLine($"Informational Version: {version}");
            Console.WriteLine($"File Version: {fileVersion}");
            Console.WriteLine($"Assembly Version: {assemblyVersion}");


            Logger.Info($"Service {ProgramArguments.Service} started - version: {version}");
            _builder = new HostBuilder()
                        .ConfigureServices((hostContext, services) =>
                        {
                            _services = services;
                            ConfigDependencies();
                            ConfigWorker();
                        });
        }

        public async Task Run()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty);
            await _builder.RunConsoleAsync();
        }

        public async Task Stop()
        {
            try
            {
                await _services.BuildServiceProvider().GetService<IHostedService>().StopAsync(new CancellationTokenSource().Token);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Stop service exception: {ex.Message}");
            }
        }

        private void ConfigDependencies()
        {

            // _services.AddSingleton<IUserService, UserService>();
        }

        private void ConfigWorker()
        {
            if (ProgramArguments.Service == ServiceIdentifier.UPLOADER)
            {
                _services.AddSingleton(new QueueSetting<UploadFileModel> { ProcessingThread = 2 });
                _services.AddSingleton<IQueue<UploadFileModel>, FIFO<UploadFileModel>>();
                _services.AddSingleton<IServiceWorker, UploaderWorker>();
                _services.AddHostedService<HostedService<IServiceWorker>>();
                _services.AddSingleton<IUploadService, UploadService>();
            }
            else
            {
                 Console.WriteLine($"Identifier {ProgramArguments.Service} Invalid!!!");
            }
        }
    }
}
