using System.Collections.Generic;

namespace Common.Services.Static.Queue.Core
{
    public interface IQueue<TItem>
        where TItem : IQueueItem
    {
        event ProcessItem<TItem> ProcessItem;
        void Push(TItem item);
        void RemoveAll();
        void Remove(string itemId);
        IEnumerable<string> getAllItemIds();
        IEnumerable<string> getAllInProcessItemIds();
    }
}
