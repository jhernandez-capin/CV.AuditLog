using System;

namespace CV.AuditLog.Models
{
    public class AuditLogEntry
    {
        public int      Id                 { get; set; }
        public string   EntityName         { get; set; } = string.Empty;
        public string   EntityId           { get; set; } = string.Empty;
        public string   Action             { get; set; } = string.Empty;
        public string   ModuleName         { get; set; } = string.Empty;
        public string   UserId             { get; set; } = string.Empty;
        public string   UserName           { get; set; } = string.Empty;
        public string   IpAddress          { get; set; } = string.Empty;
        public DateTime Timestamp          { get; set; }
        public string   OldValues          { get; set; } = string.Empty;
        public string   NewValues          { get; set; } = string.Empty;
        public string   ChangedProperties  { get; set; } = string.Empty;
    }
}
