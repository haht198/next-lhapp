using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Common.Services.Static;

namespace Common.Services.Logger
{
    class ElasticSearchLogEvent
    {
        public long TimeStamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public IEnumerable<object> Meta { get; set; }
        public IEnumerable<string> TraceIds { get; set; }
    }
    class ElasticSearchLogInstance
    {
        private readonly string applicationName;
        private readonly string esBulkInsertEndpoint;
        private readonly string esAPIKey;
        private readonly string esIndexPrefix;
        private ConcurrentQueue<ElasticSearchLogEvent> eventQueue = new ConcurrentQueue<ElasticSearchLogEvent>();
        private Thread consumingQueueThread = null;
        private bool IsConsumingThreadRunning => consumingQueueThread != null && consumingQueueThread.IsAlive;

        internal ElasticSearchLogInstance(string applicationName, string esBulkInsertEndpoint, string esAPIKey, string esIndexPrefix = "")
        {
            this.applicationName = applicationName;
            this.esBulkInsertEndpoint = esBulkInsertEndpoint;
            this.esAPIKey = esAPIKey;
            if (string.IsNullOrEmpty(esIndexPrefix))
            {
                this.esIndexPrefix = applicationName;
            }
            else
            {
                this.esIndexPrefix = esIndexPrefix;
            }
        }

        public void Append(string level, string message, IEnumerable<string> traceIds, params object[] meta)
        {
            eventQueue.Enqueue(new ElasticSearchLogEvent()
            {
                TimeStamp = Utils.GetTime(DateTime.UtcNow),
                Level = level,
                Message = message,
                TraceIds = traceIds,
                Meta = meta,
            });

            if (eventQueue.Count > 0 && !IsConsumingThreadRunning)
            {
                StartConsumeInNewThread();
            }
        }

        private void StartConsumeInNewThread()
        {
            if (IsConsumingThreadRunning)
            {
                return;
            }
            consumingQueueThread = new Thread(new ThreadStart(() =>
            {
                while (eventQueue.Count > 0)
                {
                    Consume();
                    Task.Delay(1000).Wait();
                }
            }))
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            consumingQueueThread.Start();
        }

        public void Consume()
        {
            if (eventQueue.Count == 0)
            {
                return;
            }
            try
            {
                Console.WriteLine($"Write log to Elastic Search (Remaining: {eventQueue.Count})");
                var esLogStringData = "";
                var numberOfEventToProcess = Math.Min(15, eventQueue.Count);
                for (int i = 0; i < numberOfEventToProcess; i++)
                {
                    var result = eventQueue.TryDequeue(out var processEvent);
                    if (!result || processEvent == null) continue;

                    var logMetaString = processEvent.Meta != null && processEvent.Meta.Count() > 0 ? Newtonsoft.Json.JsonConvert.SerializeObject(processEvent.Meta) : "";
                    if (logMetaString.Length > 15000)
                    {
                        logMetaString = logMetaString.Substring(0, 15000);
                    }
                    var logMessage = processEvent.Message;
                    if (logMessage.Length > 512)
                    {
                        logMessage = logMessage.Substring(0, 512);
                    }
                    var traceIdsString = processEvent.TraceIds != null && processEvent.TraceIds.Count() > 0 ? string.Join("||", processEvent.TraceIds) : "";
                    if (traceIdsString.Length > 1024)
                    {
                        traceIdsString = traceIdsString.Substring(0, 1024);
                    }

                    var logData = new Dictionary<string, dynamic>();
                    var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win32" : "darwin";
                    logData["timestamp"] = Utils.GetDateFromUnixTime(processEvent.TimeStamp).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff");
                    logData["level"] = processEvent.Level.ToLower();
                    logData["message"] = logMessage;
                    logData["meta"] = logMetaString;
                    logData["applicationName"] = applicationName;
                    logData["userId"] = UserSetting.UserId;
                    logData["userEmail"] = UserSetting.UserEmail;
                    logData["photoStudioId"] = UserSetting.StudioId;
                    logData["photoStudioName"] = UserSetting.StudioName;
                    logData["instanceId"] = ProgramArguments.InstanceId;
                    logData["appVersion"] = ProgramArguments.AppVersion;
                    logData["os_hostName"] = Environment.MachineName;
                    logData["os_username"] = Environment.UserName;
                    logData["os_platform"] = platform;
                    logData["os_release"] = Environment.OSVersion.Version.ToString();
                    logData["traceId"] = traceIdsString;
                    logData["userAgent"] = $"KELVIN-{ProgramArguments.Env.ToUpper()}/{ProgramArguments.AppVersion} {platform} | {ProgramArguments.InstanceId}";


                    esLogStringData += (
                    "{\"index\": {\"_index\": \"" + $"{esIndexPrefix}-{DateTime.UtcNow:yyyy-MM-dd}" + "\", \"_id\" : \"" + processEvent.TimeStamp.ToString() + "\"}}" +
                    "\n" +
                    Newtonsoft.Json.JsonConvert.SerializeObject(logData) +
                    "\n"
                    );
                }
                CustomHttpClient.Create(esBulkInsertEndpoint)
                                .AddHeader("x-api-key", esAPIKey)
                                .AddHeader("Content-Type", "application/json")
                                .PostRawStringAsync<dynamic>(esLogStringData)
                                .Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Write log to Elastic Search error", ex);
            }
        }
    }
}
