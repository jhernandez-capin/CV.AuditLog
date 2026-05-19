using CV.AuditLog.Configuration;
using Serilog;
using System;

namespace CV.AuditLog
{
    /// <summary>
    /// One-time setup for CV.AuditLog. Call <see cref="Configure"/> at application startup
    /// (Global.asax Application_Start for .NET Framework; Program.cs / Startup.cs for .NET Core).
    /// If Configure is never called the file sink remains silent — no exceptions, no output.
    /// </summary>
    public static class AuditLogSetup
    {
        private static ILogger _fileLogger = Serilog.Core.Logger.None;

        /// <summary>The Serilog ILogger used by all AuditLogger instances for file output.</summary>
        internal static ILogger FileLogger => _fileLogger;

        /// <summary>
        /// Configures the internal Serilog file logger.
        /// Can be called more than once (e.g., during tests); each call replaces the previous logger.
        /// </summary>
        /// <param name="configure">Optional delegate to override default <see cref="AuditLogOptions"/>.</param>
        public static void Configure(Action<AuditLogOptions> configure = null)
        {
            var options = new AuditLogOptions();
            configure?.Invoke(options);

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(options.MinimumLevel)
                .Enrich.WithProperty("Library", "CV.AuditLog")
                .WriteTo.File(
                    path:                    options.LogFilePath,
                    rollingInterval:         options.RollingInterval,
                    fileSizeLimitBytes:      options.FileSizeLimitBytes,
                    rollOnFileSizeLimit:     options.RollOnFileSizeLimit,
                    retainedFileCountLimit:  options.RetainedFileCountLimit,
                    outputTemplate:          options.OutputTemplate,
                    shared:                  false
                );

            _fileLogger = loggerConfig.CreateLogger();
        }

        /// <summary>Resets the file logger to the silent no-op state. Useful in tests.</summary>
        public static void Reset() => _fileLogger = Serilog.Core.Logger.None;
    }
}
