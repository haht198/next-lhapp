using Common.Services.Static.Queue.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common.Services.Static.Queue
{
    public class FIFO<TItem> : IQueue<TItem>
        where TItem : IQueueItem
    {
        public event ProcessItem<TItem> ProcessItem;

        private readonly QueueSetting<TItem> setting;
        private List<QueueItemProcessing> items;

        public FIFO(QueueSetting<TItem> _setting = null)
        {
            items = new List<QueueItemProcessing>();
            if (_setting != null)
            {
                setting = _setting;
            }
            else
            {
                setting = new QueueSetting<TItem>()
                {
                    ProcessingThread = 3
                };
            }
        }

        public void Push(TItem item)
        {
            lock (items)
            {
                if (!items.Any(t => t.Item.ItemId == item.ItemId))
                {
                    items.Add(new QueueItemProcessing(item));
                }
            }
            DoProcessQueue();
        }

        public void Remove(string itemId)
        {
            lock (items)
            {
                var removeItem = items.FirstOrDefault(t => t.Item.ItemId == itemId);
                if (removeItem != null)
                {
                    removeItem.CancelTokenSource.Cancel();
                    items.Remove(removeItem);
                }
            }
        }

        public void RemoveAll()
        {
            lock (items)
            {
                foreach (var item in items)
                {
                    item.CancelTokenSource.Cancel();
                }
                items = new List<QueueItemProcessing>();
            }
        }

        public IEnumerable<string> getAllItemIds()
        {
            return this.items.Select(t => t.Item.ItemId);
        }

        public IEnumerable<string> getAllInProcessItemIds()
        {
            return this.items.Where(rt => rt.Status == QueueItemStatusEnum.Processing).Select(t => t.Item.ItemId);
        }

        private void DoProcessQueue()
        {
            if (items.Count == 0 || items.Count(t => t.Status == QueueItemStatusEnum.Processing) >= setting.ProcessingThread)
            {
                return;
            }
            foreach (var processItem in items.Where(rt => rt.Status == QueueItemStatusEnum.Waiting).Skip(0).Take(setting.ProcessingThread - items.Count(t => t.Status == QueueItemStatusEnum.Processing)))
            {
                processItem.Status = QueueItemStatusEnum.Processing;
                ThreadPool.QueueUserWorkItem((@input) =>
                {
                    var item = (QueueItemProcessing)@input;
                    var cancelTokenSource = new CancellationTokenSource();
                    if (ProcessItem != null)
                    {
                        ProcessItem.Invoke((TItem)item.Item, item.CancelTokenSource.Token);
                    }
                    lock (items)
                    {
                        items.Remove(processItem);
                    }
                    if (items.Count > 0)
                    {
                        DoProcessQueue();
                    }
                }, processItem);
            }
        }
    }
}
