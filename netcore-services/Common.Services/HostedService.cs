using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.Services.Static;
using Common.Services.Core;
using Common.Services.Static.Logger;

namespace Common.Services
{
    public class HostedService<T> : IHostedService
        where T : class, IServiceWorker
    {
        private readonly T handler;

        public HostedService(T _handler)
        {
            this.handler = _handler;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                var connectResult = SocketIntegration.Init(ProgramArguments.Service);
                if (!connectResult)
                {
                    Logger.Error($"[SOCKET] - Cannot connect to main socket at {ProgramArguments.SocketServerHost}:{ProgramArguments.SocketServerPort}");
                    return;
                }
                // Static.Logger.Logger.Info($"[SOCKET] - Connected to main socket at {ProgramArguments.SocketServerHost}:{ProgramArguments.SocketServerPort}");

                await handler.DoWork();
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Task.Delay(10000).Wait(); //delay to flush cache
            return Task.CompletedTask;
        }
    }
}
