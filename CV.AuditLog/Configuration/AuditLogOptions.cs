using Serilog;
using Serilog.Events;

namespace CV.AuditLog.Configuration
{
    /// <summary>
    /// Options for the CV.AuditLog file sink. Pass to <see cref="AuditLogSetup.Configure"/>.
    /// </summary>
    public class AuditLogOptions
    {
        /// <summary>
        /// Full or relative path including the file name base.
        /// Serilog appends the date/counter suffix automatically.
        /// Default: "logs/audit-.log"
        /// </summary>
        public string LogFilePath { get; set; } = "logs/audit-.log";

        /// <summary>
        /// Create a new file every Day (default) or every Hour.
        /// </summary>
        public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;

        /// <summary>
        /// Maximum bytes per file before rolling over. Default: 10 MB.
        /// Set to null to disable size-based rolling.
        /// </summary>
        public long? FileSizeLimitBytes { get; set; } = 10L * 1024 * 1024;

        /// <summary>
        /// When true, a new file is created when <see cref="FileSizeLimitBytes"/> is reached
        /// within the same rolling interval. Default: true.
        /// </summary>
        public bool RollOnFileSizeLimit { get; set; } = true;

        /// <summary>
        /// Number of log files to keep. Older files are deleted automatically. Default: 31.
        /// Set to null to keep all files.
        /// </summary>
        public int? RetainedFileCountLimit { get; set; } = 31;

        /// <summary>
        /// Minimum log event level written to the file. Default: Information.
        /// </summary>
        public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

        /// <summary>
        /// Path for the error-only log file (Error level and above).
        /// Serilog writes to this file in addition to <see cref="LogFilePath"/>.
        /// Set to null or empty to disable the dedicated error file.
        /// Default: "logs/errors-.log"
        /// </summary>
        public string ErrorLogFilePath { get; set; } = "logs/errors-.log";

        /// <summary>
        /// Serilog output template for each log line.
        /// </summary>
        public string OutputTemplate { get; set; } =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    }
}
