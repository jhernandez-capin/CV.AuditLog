using System;

namespace CV.AuditLog.Attributes
{
    /// <summary>
    /// Marks a controller so that all its successful write actions (POST/PUT/DELETE)
    /// are automatically recorded in the audit log.
    /// Apply at class level; inherited by derived controllers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class AuditableAttribute : Attribute
    {
        /// <summary>
        /// Optional module name written to the ModuleName column of the audit log.
        /// If null, the controller name is used instead.
        /// </summary>
        public string ModuleName { get; set; }

        public AuditableAttribute(string moduleName = null)
        {
            ModuleName = moduleName;
        }
    }
}
