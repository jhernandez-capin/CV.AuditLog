namespace CV.AuditLog.Abstractions
{
    /// <summary>
    /// Provides user and request context for audit logging.
    /// Each consuming project implements this interface using its own stack
    /// (System.Web.HttpContext in ASP.NET MVC, IHttpContextAccessor in ASP.NET Core, etc.).
    /// </summary>
    public interface IAuditContextProvider
    {
        string GetUserId();
        string GetUserName();
        string GetIpAddress();
    }
}
