using CV.AuditLog.Abstractions;
using CV.AuditLog.Models;
using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;

namespace CV.AuditLog
{
    /// <summary>
    /// Logs application errors to the Serilog file sink (Error level, full stack trace)
    /// and to the persistent IErrorRepository (dbo.ErrorLog).
    /// Fail-safe: Log() never throws, regardless of sink or repository failures.
    /// </summary>
    public class ErrorLogger
    {
        private readonly IErrorRepository      _repository;
        private readonly IAuditContextProvider _contextProvider;

        public ErrorLogger(IErrorRepository repository, IAuditContextProvider contextProvider)
        {
            _repository      = repository      ?? throw new ArgumentNullException(nameof(repository));
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        }

        /// <summary>
        /// Records an exception to the error file and the error database table.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="module">Module or controller name (e.g. "Tickets").</param>
        /// <param name="action">
        /// Action or method name. When called from inside a method, leave this empty and
        /// the compiler fills it in automatically via [CallerMemberName].
        /// </param>
        /// <param name="additionalData">Optional extra context serialized as JSON (e.g. route params).</param>
        public void Log(
            Exception exception,
            string    module,
            [CallerMemberName] string action         = "",
            object                    additionalData = null)
        {
            if (exception == null) return;

            try
            {
                var entry = new ErrorLogEntry
                {
                    Module         = module    ?? string.Empty,
                    Action         = action    ?? string.Empty,
                    UserId         = SafeGet(_contextProvider.GetUserId),
                    UserName       = SafeGet(_contextProvider.GetUserName),
                    IpAddress      = SafeGet(_contextProvider.GetIpAddress),
                    Timestamp      = DateTime.UtcNow,
                    ExceptionType  = exception.GetType().FullName,
                    Message        = exception.Message,
                    StackTrace     = exception.StackTrace ?? string.Empty,
                    AdditionalData = Serialize(additionalData),
                };

                // ── File (structured, Serilog captures stack trace automatically) ──
                AuditLogSetup.FileLogger
                    .ForContext("Module",    entry.Module)
                    .ForContext("Action",    entry.Action)
                    .ForContext("UserId",    entry.UserId)
                    .ForContext("UserName",  entry.UserName)
                    .Error(exception,
                        "[Error] {Module}.{Action} | User: {UserName} | {ExceptionType}: {Message}",
                        entry.Module, entry.Action, entry.UserName,
                        entry.ExceptionType, entry.Message);

                // ── Database ──────────────────────────────────────────────────────
                _repository.Add(entry);
            }
            catch
            {
                // Fail-safe: error logging must never interrupt the caller.
            }
        }

        private static string Serialize(object value)
        {
            if (value == null) return string.Empty;
            try   { return JsonConvert.SerializeObject(value); }
            catch { return string.Empty; }
        }

        private static string SafeGet(Func<string> getter)
        {
            try   { return getter() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
