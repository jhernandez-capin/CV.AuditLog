using CV.AuditLog.Abstractions;
using CV.AuditLog.Enums;
using CV.AuditLog.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CV.AuditLog
{
    public class AuditLogger : IAuditLogger
    {
        private readonly IAuditRepository      _repository;
        private readonly IAuditContextProvider _contextProvider;

        public AuditLogger(IAuditRepository repository, IAuditContextProvider contextProvider)
        {
            _repository      = repository      ?? throw new ArgumentNullException(nameof(repository));
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        }

        public void Log(
            string entityName,
            string entityId,
            string action,
            object oldValues  = null,
            object newValues  = null,
            string moduleName = null)
        {
            try
            {
                var entry = new AuditLogEntry
                {
                    EntityName        = entityName  ?? string.Empty,
                    EntityId          = entityId    ?? string.Empty,
                    Action            = action      ?? string.Empty,
                    ModuleName        = moduleName  ?? string.Empty,
                    UserId            = SafeGet(_contextProvider.GetUserId),
                    UserName          = SafeGet(_contextProvider.GetUserName),
                    IpAddress         = SafeGet(_contextProvider.GetIpAddress),
                    Timestamp         = DateTime.UtcNow,
                    OldValues         = Serialize(oldValues),
                    NewValues         = Serialize(newValues),
                    ChangedProperties = SerializeChangedProperties(oldValues, newValues),
                };

                // ── File log (all events) ─────────────────────────────────────
                WriteToFile(entry);

                // ── Database (Create / Update / Delete only) ──────────────────
                if (IsMutationAction(entry.Action))
                    _repository.Add(entry);
            }
            catch (Exception ex)
            {
                // Fail-safe: audit errors must never interrupt the caller's operation.
                Trace.TraceWarning("[CV.AuditLog] Log failed for entity '{0}' id '{1}': {2}", entityName, entityId, ex.Message);
            }
        }

        public IEnumerable<AuditLogEntry> GetLogs(
            string    entityName = null,
            string    entityId   = null,
            string    moduleName = null,
            DateTime? fromDate   = null,
            DateTime? toDate     = null)
        {
            return _repository.GetLogs(entityName, entityId, moduleName, fromDate, toDate)
                   ?? Enumerable.Empty<AuditLogEntry>();
        }

        public IEnumerable<AuditLogEntry> GetUserActivity(
            string    userId,
            DateTime? fromDate = null,
            DateTime? toDate   = null)
        {
            return _repository.GetUserActivity(userId, fromDate, toDate)
                   ?? Enumerable.Empty<AuditLogEntry>();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static void WriteToFile(AuditLogEntry entry)
        {
            AuditLogSetup.FileLogger
                .ForContext("EntityName", entry.EntityName)
                .ForContext("EntityId",   entry.EntityId)
                .ForContext("Action",     entry.Action)
                .ForContext("ModuleName", entry.ModuleName)
                .ForContext("UserId",     entry.UserId)
                .ForContext("UserName",   entry.UserName)
                .ForContext("IpAddress",  entry.IpAddress)
                .Information(
                    "[Audit] {Action} | {EntityName}#{EntityId} | User: {UserName} | Module: {ModuleName}",
                    entry.Action, entry.EntityName, entry.EntityId, entry.UserName, entry.ModuleName);
        }

        private static bool IsMutationAction(string action) =>
            string.Equals(action, AuditAction.Create, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(action, AuditAction.Update, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(action, AuditAction.Delete, StringComparison.OrdinalIgnoreCase);

        private static string Serialize(object value)
        {
            if (value == null) return string.Empty;
            try   { return JsonConvert.SerializeObject(value); }
            catch { return string.Empty; }
        }

        private static string SerializeChangedProperties(object oldValues, object newValues)
        {
            var changed = GetChangedProperties(oldValues, newValues);
            return JsonConvert.SerializeObject(changed);
        }

        private static List<string> GetChangedProperties(object oldValues, object newValues)
        {
            var result = new List<string>();

            if (oldValues == null || newValues == null)
                return result;

            PropertyInfo[] oldProps;
            PropertyInfo[] newProps;

            try
            {
                oldProps = oldValues.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                newProps = newValues.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            }
            catch
            {
                return result;
            }

            var newPropMap = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            foreach (var p in newProps)
                newPropMap[p.Name] = p;

            foreach (var oldProp in oldProps)
            {
                if (!newPropMap.TryGetValue(oldProp.Name, out var newProp))
                    continue;

                try
                {
                    var oldVal = oldProp.GetValue(oldValues);
                    var newVal = newProp.GetValue(newValues);

                    if (!Equals(oldVal, newVal))
                        result.Add(oldProp.Name);
                }
                catch { }
            }

            return result;
        }

        private static string SafeGet(Func<string> getter)
        {
            try   { return getter() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
