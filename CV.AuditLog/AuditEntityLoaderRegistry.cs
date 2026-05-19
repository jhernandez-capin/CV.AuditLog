using System;
using System.Collections.Generic;

namespace CV.AuditLog
{
    /// <summary>
    /// Optional static registry that maps entity names to loader functions.
    /// Allows the AuditActionFilter to load the "before" state of an entity
    /// when an UPDATE or DELETE is detected.
    ///
    /// Register loaders at app startup (e.g. Global.asax Application_Start):
    ///   AuditEntityLoaderRegistry.Register("Tickets", id => ticketRepo.GetById(id));
    /// </summary>
    public static class AuditEntityLoaderRegistry
    {
        private static readonly Dictionary<string, Func<string, object>> _loaders =
            new Dictionary<string, Func<string, object>>(StringComparer.OrdinalIgnoreCase);

        public static void Register(string entityName, Func<string, object> loader)
        {
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentNullException(nameof(entityName));
            if (loader == null)                         throw new ArgumentNullException(nameof(loader));

            _loaders[entityName] = loader;
        }

        /// <summary>
        /// Returns the old entity state for a given entityName + entityId, or null if no loader is registered
        /// or the loader throws.
        /// </summary>
        public static object Load(string entityName, string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(entityId))
                return null;

            if (!_loaders.TryGetValue(entityName, out var loader))
                return null;

            try   { return loader(entityId); }
            catch { return null; }
        }

        public static void Clear() => _loaders.Clear();
    }
}
