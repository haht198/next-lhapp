namespace Common.Services.Static.Queue.Core
{
    public class QueueSetting<TItem>
        where TItem : IQueueItem
    {
        public int ProcessingThread { get; set; }
    }
}
