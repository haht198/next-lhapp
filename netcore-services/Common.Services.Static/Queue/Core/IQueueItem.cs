using System.Threading;

namespace Common.Services.Static.Queue.Core
{
    public interface IQueueItem
    {
        string ItemId { get; set; }
    }
    internal enum QueueItemStatusEnum
    {
        Waiting,
        Processing
    }
    internal class QueueItemProcessing
    {
        internal IQueueItem Item { get; set; }
        internal QueueItemStatusEnum Status { get; set; }
        internal CancellationTokenSource CancelTokenSource { get; set; }

        public QueueItemProcessing(IQueueItem item)
        {
            this.Item = item;
            CancelTokenSource = new CancellationTokenSource();
            Status = QueueItemStatusEnum.Waiting;
        }
    }
}
