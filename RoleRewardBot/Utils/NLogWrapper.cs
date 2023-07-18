using System;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using NLog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;

namespace RoleRewardBot.Utils
{
    public class DSharpPlusNLogAdapter : ILoggerFactory
    {
        private readonly Logger _logger = LogManager.GetLogger("DSharpPlus");
        private readonly LogLevel _minLevel;

        public DSharpPlusNLogAdapter(LogLevel minLevel)
        {
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DSharpPlusNLogAdapterLogger(_logger, _minLevel);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // This is a wrapper straight to NLog, so we don't need to add any providers.
        }

        public void Dispose()
        {
        }
    }

    public class DSharpPlusNLogAdapterLogger : ILogger
    {
        private readonly Logger _logger;
        private readonly LogLevel _minLevel;
        public DSharpPlusNLogAdapterLogger(Logger logger, LogLevel minLevel)
        {
            _logger = logger;
            _minLevel = minLevel;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                var message = formatter(state, exception);
                _logger.Log(ConvertLogLevel(logLevel), message);
            }
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null; // Since NLog does not support scopes, returning null is acceptable.
        }
        
        private LogLevel ConvertLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    return LogLevel.Fatal;
                case Microsoft.Extensions.Logging.LogLevel.Error:
                    return LogLevel.Error;
                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    return LogLevel.Warn;
                case Microsoft.Extensions.Logging.LogLevel.Information:
                    return LogLevel.Info;
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    return LogLevel.Debug;
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                    return LogLevel.Trace;
                default:
                    return LogLevel.Info;
            }
        }
    }
}