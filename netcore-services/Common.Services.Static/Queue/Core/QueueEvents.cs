using System.Threading;

namespace Common.Services.Static.Queue.Core
{
    public delegate void ProcessItem<TItem>(TItem item, CancellationToken cancelToken) where TItem : IQueueItem;
    public delegate void ProcessItemDone<TItem>(TItem item) where TItem : IQueueItem;
}
