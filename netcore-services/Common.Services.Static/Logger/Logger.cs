using Serilog;
using System;
using Serilog.Exceptions;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;

namespace Common.Services.Static.Logger
{
    public static class LoggerConfig
    {
        public static void AddConsoleLog()
        {
            Logger.AddConsoleLog();
        }
        public static void AddElasticSearchLog(string loggerIdentifier, string esBulkInsertEndpoint, string esAPIKey, string esIndexPrefix = "")
        {
            Logger.AddElasticSearchLog(loggerIdentifier, esBulkInsertEndpoint, esAPIKey, esIndexPrefix);
        }
    }
    public static class Logger
    {
        private static ILogger serilog;
        private static ElasticSearchLogInstance eslog;
        private static bool usingSentry = false;
        private static LoggerInstance defaultInstance;

        internal static void AddConsoleLog()
        {
            serilog = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .Enrich.With<LoggerUtcTimestampEnricher>()
                            .Enrich.WithExceptionDetails()
                            .WriteTo.Console()
                            .CreateLogger();
            defaultInstance = new LoggerInstance(serilog, eslog, usingSentry);
        }
        internal static void AddElasticSearchLog(string loggerIdentifier, string esBulkInsertEndpoint, string esAPIKey, string esIndexPrefix = "")
        {
            eslog = new ElasticSearchLogInstance(loggerIdentifier, esBulkInsertEndpoint, esAPIKey, esIndexPrefix);
            defaultInstance = new LoggerInstance(serilog, eslog, usingSentry);
        }

        public static LoggerInstance Tracing(params string[] traceIds)
        {
            return defaultInstance.Tracing(traceIds);
        }
        public static void Debug(string message, params dynamic[] meta)
        {
            defaultInstance.Debug(message, meta);
        }
        public static void Debug(Exception ex, string message, params dynamic[] meta)
        {
            defaultInstance.Debug(ex, message, meta);
        }
        public static void Info(string message, params dynamic[] meta)
        {
            defaultInstance.Info(message, meta);
        }
        public static void Info(Exception ex, string message, params dynamic[] meta)
        {
            defaultInstance.Info(ex, message, meta);
        }
        public static void Warning(string message, params dynamic[] meta)
        {
            defaultInstance.Warning(message, meta);
        }
        public static void Warning(Exception ex, string message, params dynamic[] meta)
        {
            defaultInstance.Warning(ex, message, meta);
        }
        public static void Error(string message, params dynamic[] meta)
        {
            defaultInstance.Error(message, meta);
        }
        public static void Error(Exception ex, string message, params dynamic[] meta)
        {
            defaultInstance.Error(ex, message, meta);
        }
    }

    public class LoggerInstance
    {
        private readonly ILogger serilog;
        private readonly ElasticSearchLogInstance eslog;
        private readonly bool usingSentry = false;

        private readonly string[] traceIds;

        internal LoggerInstance(ILogger serilog, ElasticSearchLogInstance eslog, bool usingSentry, params string[] traceIds)
        {
            this.serilog = serilog;
            this.eslog = eslog;
            this.usingSentry = usingSentry;
            this.traceIds = traceIds;
        }

        public LoggerInstance Tracing(params string[] newTraceIds)
        {
            var allTraceIds = new List<string>();
            if (traceIds != null && traceIds.Length > 0)
            {
                allTraceIds.AddRange(traceIds);
            }
            if (newTraceIds != null && newTraceIds.Length > 0)
            {
                allTraceIds.AddRange(newTraceIds);
            }
            return new LoggerInstance(serilog, eslog, usingSentry, allTraceIds.ToArray());
        }
        public void Debug(string message, params dynamic[] meta)
        {
            if (serilog != null)
            {
                serilog.Debug(GetLogMessage("Debug", message, meta));
            }
            if (eslog != null)
            {
                eslog.Append("Debug", message, traceIds, meta);
            }
        }
        public void Debug(Exception ex, string message, params dynamic[] meta)
        {
            if (serilog != null)
            {
                serilog.Debug(GetLogMessage("Debug", message, meta, ex));
            }
            if (eslog != null)
            {
                eslog.Append("Debug", message, traceIds, meta, new { ExceptionMessage = ex.Message, ExceptionStacktrace = ex.StackTrace });
            }
        }


        public void Info(string message, params dynamic[] meta)
        {
            if (serilog != null)
            {
                serilog.Information(GetLogMessage("Info", message, meta));
            }
            if (eslog != null)
            {
                eslog.Append("Info", message, traceIds, meta);
            }

        }
        public void Info(Exception ex, string message, params dynamic[] meta)
        {
            if (serilog != null)
            {
                serilog.Information(GetLogMessage("Info", message, meta, ex));
            }
            if (eslog != null)
            {
                eslog.Append("Info", message, traceIds, meta, new { ExceptionMessage = ex.Message, ExceptionStacktrace = ex.StackTrace });
            }
        }


        public void Warning(string message, params dynamic[] meta)
        {
            if (serilog != null)
            {
                serilog.Warning(GetLogMessage("Warning", message, meta));
            }
            if (eslog != null)
            {
                eslog.Append("Warning", message, traceIds, meta);
            }
        }
        public void Warning(Exception ex, string message, params dynamic[] meta)
        {
            if (serilog != null)
            {
                serilog.Warning(GetLogMessage("Warning", message, meta, ex));
            }
            if (eslog != null)
            {
                eslog.Append("Warning", message, traceIds, meta, new { ExceptionMessage = ex.Message, ExceptionStacktrace = ex.StackTrace });
            }
        }

        public void Error(string message, params dynamic[] meta)
        {
            if (serilog != null)
            {
                serilog.Error(GetLogMessage("Error", message, meta));
            }
            if (eslog != null)
            {
                eslog.Append("Error", message, traceIds, meta);
            }
        }
        public void Error(Exception ex, string message, params dynamic[] meta)
        {
            if (serilog != null)
            {
                serilog.Error(GetLogMessage("Error", message, meta, ex));
            }
            if (eslog != null)
            {
                eslog.Append("Error", message, traceIds, meta, new { ExceptionMessage = ex.Message, ExceptionStacktrace = ex.StackTrace });
            }
        }

        private string GetLogMessage(string level, string message, dynamic[] meta = null, Exception ex = null)
        {
            var logMsg = FormatMessage(level, message);
            //if (meta != null && meta.Length > 0)
            //{
            //    logMsg += Environment.NewLine;
            //    logMsg += ParseMetaObject(meta);
            //}
            if (ex != null)
            {
                logMsg += Environment.NewLine;
                logMsg += ParseException(ex);
            }
            logMsg += Environment.NewLine;
            return logMsg;
        }
        private string FormatMessage(string level, string message)
        {
            return $"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")}] [{level.ToUpper()}] {message.Trim()}";
        }
        private string ParseMetaObject(object[] metas)
        {
            if (metas == null || metas.Length == 0)
            {
                return string.Empty;
            }
            var message = string.Empty;
            for (var i = 0; i < metas.Length; i++)
            {
                var metaString = string.Empty;
                try
                {
                    metaString = Newtonsoft.Json.JsonConvert.SerializeObject(metas[i]);
                }
                catch (Exception) { }
                message += metaString;
                if (i < metas.Length - 1)
                {
                    message += Environment.NewLine;
                }
            }
            return message.Trim();
        }
        private string ParseException(Exception ex)
        {
            var rootEx = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
            if (ex.InnerException == null)
            {
                return rootEx;
            }
            return $"{rootEx}{Environment.NewLine}Inner Exception{Environment.NewLine}{ex.InnerException.Message}{Environment.NewLine}{ex.InnerException.StackTrace}";
        }
    }
    class LoggerUtcTimestampEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory pf)
        {
            logEvent.AddPropertyIfAbsent(pf.CreateProperty("UtcTimestamp", logEvent.Timestamp.UtcDateTime));
        }
    }

    public enum LogLever
    {
        Info,
        Debug,
        Warn,
        Error
    }
}
