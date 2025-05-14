using System.Threading.Tasks;

namespace Common.Services.Core
{
    public abstract class ServiceWorkerInSchedule : IServiceWorker
    {
        public abstract int ScheduleTimeInMilliSeconds { get; }
        public abstract Task DoWork();
    }
}
