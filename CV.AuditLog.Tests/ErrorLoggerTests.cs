using CV.AuditLog.Abstractions;
using CV.AuditLog.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace CV.AuditLog.Tests
{
    public class ErrorLoggerTests
    {
        // ── Fakes ────────────────────────────────────────────────────────────────

        private class FakeErrorRepository : IErrorRepository
        {
            public List<ErrorLogEntry> Entries { get; } = new();
            public void Add(ErrorLogEntry entry) => Entries.Add(entry);
        }

        private class FakeContextProvider : IAuditContextProvider
        {
            public string GetUserId()    => "7";
            public string GetUserName()  => "admin.user";
            public string GetIpAddress() => "10.0.0.1";
        }

        private class ThrowingContextProvider : IAuditContextProvider
        {
            public string GetUserId()    => throw new InvalidOperationException();
            public string GetUserName()  => throw new InvalidOperationException();
            public string GetIpAddress() => throw new InvalidOperationException();
        }

        private class ThrowingErrorRepository : IErrorRepository
        {
            public void Add(ErrorLogEntry entry) => throw new Exception("DB down");
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        [Fact]
        public void Log_PersistsEntryWithCorrectFields()
        {
            var repo   = new FakeErrorRepository();
            var logger = new ErrorLogger(repo, new FakeContextProvider());
            var ex     = new InvalidOperationException("something went wrong");

            logger.Log(ex, "Tickets", "ChangeStatus");

            Assert.Single(repo.Entries);
            var entry = repo.Entries[0];
            Assert.Equal("Tickets",                                entry.Module);
            Assert.Equal("ChangeStatus",                           entry.Action);
            Assert.Equal("7",                                      entry.UserId);
            Assert.Equal("admin.user",                             entry.UserName);
            Assert.Equal("10.0.0.1",                               entry.IpAddress);
            Assert.Equal(typeof(InvalidOperationException).FullName, entry.ExceptionType);
            Assert.Equal("something went wrong",                   entry.Message);
            Assert.True(entry.Timestamp <= DateTime.UtcNow);
        }

        [Fact]
        public void Log_WhenExceptionIsNull_DoesNotThrowAndDoesNotPersist()
        {
            var repo   = new FakeErrorRepository();
            var logger = new ErrorLogger(repo, new FakeContextProvider());

            var ex = Record.Exception(() => logger.Log(null, "Tickets", "Create"));

            Assert.Null(ex);
            Assert.Empty(repo.Entries);
        }

        [Fact]
        public void Log_WhenRepositoryThrows_DoesNotPropagate()
        {
            var logger = new ErrorLogger(new ThrowingErrorRepository(), new FakeContextProvider());

            var ex = Record.Exception(() =>
                logger.Log(new Exception("test"), "Tickets", "SaveWorkSetup"));

            Assert.Null(ex);
        }

        [Fact]
        public void Log_WhenContextProviderThrows_DoesNotPropagate()
        {
            var repo   = new FakeErrorRepository();
            var logger = new ErrorLogger(repo, new ThrowingContextProvider());

            var ex = Record.Exception(() =>
                logger.Log(new Exception("test"), "Tickets", "DeleteTicket"));

            Assert.Null(ex);
            Assert.Single(repo.Entries);
            Assert.Equal(string.Empty, repo.Entries[0].UserId);
        }

        [Fact]
        public void Log_CapturesStackTrace()
        {
            var repo   = new FakeErrorRepository();
            var logger = new ErrorLogger(repo, new FakeContextProvider());
            Exception captured;
            try { throw new ArgumentException("bad arg"); }
            catch (Exception e) { captured = e; }

            logger.Log(captured, "Tickets", "AcceptTicket");

            Assert.NotNull(repo.Entries[0].StackTrace);
            Assert.NotEmpty(repo.Entries[0].StackTrace);
        }

        [Fact]
        public void Log_SerializesAdditionalData()
        {
            var repo   = new FakeErrorRepository();
            var logger = new ErrorLogger(repo, new FakeContextProvider());

            logger.Log(new Exception("err"), "Tickets", "QuickAssign",
                additionalData: new { ticketId = 42, userId = 7 });

            Assert.Contains("42", repo.Entries[0].AdditionalData);
        }

        [Fact]
        public void Constructor_NullRepository_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ErrorLogger(null, new FakeContextProvider()));
        }

        [Fact]
        public void Constructor_NullContextProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ErrorLogger(new FakeErrorRepository(), null));
        }
    }
}
