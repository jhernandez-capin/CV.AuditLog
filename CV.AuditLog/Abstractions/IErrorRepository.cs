using CV.AuditLog.Models;

namespace CV.AuditLog.Abstractions
{
    public interface IErrorRepository
    {
        void Add(ErrorLogEntry entry);
    }
}
