using System;

namespace CV.AuditLog.Models
{
    public class ErrorLogEntry
    {
        public long     Id             { get; set; }
        public string   Module         { get; set; }
        public string   Action         { get; set; }
        public string   UserId         { get; set; }
        public string   UserName       { get; set; }
        public string   IpAddress      { get; set; }
        public DateTime Timestamp      { get; set; }
        public string   ExceptionType  { get; set; }
        public string   Message        { get; set; }
        public string   StackTrace     { get; set; }
        public string   AdditionalData { get; set; }
    }
}
