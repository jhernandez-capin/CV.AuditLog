using CV.AuditLog.Abstractions;
using CV.AuditLog.Enums;
using CV.AuditLog.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace CV.AuditLog.Tests
{
    public class AuditLoggerTests
    {
        // ── Fakes ─────────────────────────────────────────────────────────────

        private class FakeRepository : IAuditRepository
        {
            public List<AuditLogEntry> Entries { get; } = new();

            public void Add(AuditLogEntry entry) => Entries.Add(entry);

            public IEnumerable<AuditLogEntry> GetLogs(
                string entityName, string entityId, string moduleName,
                DateTime? fromDate, DateTime? toDate) => Entries;

            public IEnumerable<AuditLogEntry> GetUserActivity(
                string userId, DateTime? fromDate, DateTime? toDate) => Entries;
        }

        private class FakeContextProvider : IAuditContextProvider
        {
            public string GetUserId()    => "42";
            public string GetUserName()  => "test.user";
            public string GetIpAddress() => "127.0.0.1";
        }

        private class ThrowingContextProvider : IAuditContextProvider
        {
            public string GetUserId()    => throw new InvalidOperationException("no context");
            public string GetUserName()  => throw new InvalidOperationException("no context");
            public string GetIpAddress() => throw new InvalidOperationException("no context");
        }

        private class ThrowingRepository : IAuditRepository
        {
            public void Add(AuditLogEntry entry) => throw new Exception("DB down");
            public IEnumerable<AuditLogEntry> GetLogs(string e, string id, string m, DateTime? f, DateTime? t) => new List<AuditLogEntry>();
            public IEnumerable<AuditLogEntry> GetUserActivity(string u, DateTime? f, DateTime? t) => new List<AuditLogEntry>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Fact]
        public void Log_CreatesEntry_WithCorrectFields()
        {
            var repo    = new FakeRepository();
            var logger  = new AuditLogger(repo, new FakeContextProvider());

            logger.Log("Ticket", "3027", AuditAction.Update, moduleName: "Tickets");

            Assert.Single(repo.Entries);
            var entry = repo.Entries[0];
            Assert.Equal("Ticket",         entry.EntityName);
            Assert.Equal("3027",           entry.EntityId);
            Assert.Equal(AuditAction.Update, entry.Action);
            Assert.Equal("Tickets",        entry.ModuleName);
            Assert.Equal("42",             entry.UserId);
            Assert.Equal("test.user",      entry.UserName);
            Assert.Equal("127.0.0.1",      entry.IpAddress);
            Assert.True(entry.Timestamp <= DateTime.UtcNow);
        }

        [Fact]
        public void Log_WithNullOldAndNewValues_DoesNotThrow()
        {
            var repo   = new FakeRepository();
            var logger = new AuditLogger(repo, new FakeContextProvider());

            var ex = Record.Exception(() => logger.Log("Entry", "1", AuditAction.Create));

            Assert.Null(ex);
            Assert.Single(repo.Entries);
        }

        [Fact]
        public void Log_DetectsChangedProperties()
        {
            var repo   = new FakeRepository();
            var logger = new AuditLogger(repo, new FakeContextProvider());

            var before = new { Title = "Old title", StatusId = 1 };
            var after  = new { Title = "New title", StatusId = 1 };

            logger.Log("Ticket", "1", AuditAction.Update, before, after);

            var entry = repo.Entries[0];
            Assert.Contains("Title", entry.ChangedProperties);
            Assert.DoesNotContain("StatusId", entry.ChangedProperties);
        }

        [Fact]
        public void Log_WhenNoPropertiesChanged_ChangedPropertiesIsEmptyArray()
        {
            var repo   = new FakeRepository();
            var logger = new AuditLogger(repo, new FakeContextProvider());

            var obj = new { Name = "Same", Value = 99 };
            logger.Log("Invoice", "5", AuditAction.Update, obj, obj);

            Assert.Equal("[]", repo.Entries[0].ChangedProperties);
        }

        [Fact]
        public void Log_WhenRepositoryThrows_DoesNotPropagate()
        {
            var logger = new AuditLogger(new ThrowingRepository(), new FakeContextProvider());

            var ex = Record.Exception(() => logger.Log("Ticket", "1", AuditAction.Delete));

            Assert.Null(ex);
        }

        [Fact]
        public void Log_WhenContextProviderThrows_DoesNotPropagate()
        {
            var repo   = new FakeRepository();
            var logger = new AuditLogger(repo, new ThrowingContextProvider());

            var ex = Record.Exception(() => logger.Log("Ticket", "1", AuditAction.Create));

            Assert.Null(ex);
            Assert.Single(repo.Entries);
            Assert.Equal(string.Empty, repo.Entries[0].UserId);
        }

        [Fact]
        public void Log_OldAndNewValues_SerializedAsJson()
        {
            var repo   = new FakeRepository();
            var logger = new AuditLogger(repo, new FakeContextProvider());

            logger.Log("Ticket", "1", AuditAction.Update,
                oldValues: new { Status = "Open" },
                newValues: new { Status = "Closed" });

            var entry = repo.Entries[0];
            Assert.Contains("Open",   entry.OldValues);
            Assert.Contains("Closed", entry.NewValues);
        }

        [Fact]
        public void Constructor_NullRepository_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AuditLogger(null, new FakeContextProvider()));
        }

        [Fact]
        public void Constructor_NullContextProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AuditLogger(new FakeRepository(), null));
        }

        [Fact]
        public void Log_ViewAction_DoesNotWriteToRepository()
        {
            var repo   = new FakeRepository();
            var logger = new AuditLogger(repo, new FakeContextProvider());

            logger.Log("Ticket", "1", AuditAction.View);

            Assert.Empty(repo.Entries);
        }

        [Fact]
        public void Log_ExportAction_DoesNotWriteToRepository()
        {
            var repo   = new FakeRepository();
            var logger = new AuditLogger(repo, new FakeContextProvider());

            logger.Log("Ticket", "1", AuditAction.Export);

            Assert.Empty(repo.Entries);
        }

        [Fact]
        public void Log_CreateUpdateDelete_AllWriteToRepository()
        {
            var repo   = new FakeRepository();
            var logger = new AuditLogger(repo, new FakeContextProvider());

            logger.Log("Ticket", "1", AuditAction.Create);
            logger.Log("Ticket", "2", AuditAction.Update);
            logger.Log("Ticket", "3", AuditAction.Delete);

            Assert.Equal(3, repo.Entries.Count);
        }
    }
}
