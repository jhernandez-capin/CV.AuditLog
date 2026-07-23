using CV.AuditLog.Models;
using System;
using System.Collections.Generic;

namespace CV.AuditLog.Abstractions
{
    /// <summary>
    /// Main public interface for recording and querying audit logs.
    /// Inject or instantiate this in controllers and services to track entity changes.
    /// </summary>
    public interface IAuditLogger
    {
        /// <summary>
        /// Records an audit entry for the given entity action.
        /// This method never throws — failures are silently swallowed to avoid
        /// disrupting the caller's operation.
        /// </summary>
        void Log(
            string entityName,
            string entityId,
            string action,
            object oldValues  = null,
            object newValues  = null,
            string moduleName = null,
            string description = null);

        IEnumerable<AuditLogEntry> GetLogs(
            string    entityName = null,
            string    entityId   = null,
            string    moduleName = null,
            DateTime? fromDate   = null,
            DateTime? toDate     = null);

        IEnumerable<AuditLogEntry> GetUserActivity(
            string    userId,
            DateTime? fromDate = null,
            DateTime? toDate   = null);
    }
}
