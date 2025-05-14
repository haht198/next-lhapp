using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Common.Services.Core;
using Common.Services.Static;
using Common.Services.Static.Logger;
namespace Common.Services
{
    public class HostedServiceScheduler<T> : IHostedService
        where T : ServiceWorkerInSchedule
    {
        private readonly T handler;
        private Timer _timer;
        private object _locker = new object();

        public HostedServiceScheduler(T _handler)
        {
            this.handler = _handler;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() =>
            {
                var connectResult = SocketIntegration.Init(ProgramArguments.Service);
                if (!connectResult)
                {
                    Logger.Error(
                        $"[SOCKET] - Cannot connect to main socket at {ProgramArguments.SocketServerHost}:{ProgramArguments.SocketServerPort}");
                    return;
                }
                Logger.Info($"[SOCKET] - Connected to main socket at {ProgramArguments.SocketServerHost}:{ProgramArguments.SocketServerPort}");
                _timer = new Timer(TimerInterval, null, 100, this.handler.ScheduleTimeInMilliSeconds);
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Task.Delay(10000).Wait(); //delay to flush cache
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void TimerInterval(object state)
        {
            var hasLock = false;
            Monitor.TryEnter(_locker, ref hasLock);
            if (!hasLock)
            {
                return;
            }
            handler.DoWork().Wait();
            Monitor.Exit(_locker);
        }
    }
}
