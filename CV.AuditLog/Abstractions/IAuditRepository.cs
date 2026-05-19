using CV.AuditLog.Models;
using System;
using System.Collections.Generic;

namespace CV.AuditLog.Abstractions
{
    /// <summary>
    /// Persistence contract for audit log entries.
    /// Each consuming project implements this interface using its own data access strategy
    /// (Dapper, EF6, ADO.NET, etc.).
    /// </summary>
    public interface IAuditRepository
    {
        void Add(AuditLogEntry entry);

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
