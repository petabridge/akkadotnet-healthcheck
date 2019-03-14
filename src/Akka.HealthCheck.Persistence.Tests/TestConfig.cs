using Akka.Util.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.HealthCheck.Persistence.Tests
{
    public static class TestConfig
    {

        public static string GetValidConfigurationString(int? dbId = null)
        {

            return  @"akka.persistence {
                                         journal {
                                                    plugin = ""akka.persistence.journal.sqlite""
                                                    recovery-event-timeout = 2s
                                                    circuit-breaker.reset-timeout = 2s
                                                    sqlite {
                                                            class = ""Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite""
                                                            auto-initialize = on
                                                            connection-string = ""Filename=file:memdb-" + dbId + @".db;Mode=Memory;Cache=Shared"" #Invalid connetion string             
                                                     }}
                                         snapshot-store {
                                                plugin = ""akka.persistence.snapshot-store.sqlite""
                                                sqlite {
                                                class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                                                auto-initialize = on
                                                connection-string = ""Filename=file:memdb-" + dbId + @".db;Mode=Memory;Cache=Shared""
                       }
                   }}";
        }
        public static string BadJournalConfig = @"akka.persistence {
                                         journal {
                                                    plugin = ""akka.persistence.journal.sqlite""
                                                    recovery-event-timeout = 2s
                                                    circuit-breaker.reset-timeout = 2s
                                                    sqlite {
                                                            class = ""Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite""
                                                            auto-initialize = on
                                                            connection-string = ""Fake=file:memdb.db;Mode=Memory;Cache=Shared"" #Invalid connetion string             
                                                     }}
                                         snapshot-store {
                                                plugin = ""akka.persistence.snapshot-store.sqlite""
                                                sqlite {
                                                class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                                                auto-initialize = on
                                                connection-string = ""Filename=file:memdb.db;Mode=Memory;Cache=Shared""
                       }
                   }}";

        public static string BadSnapshotConfig = @"akka.persistence {
                                         journal {
                                                    plugin = ""akka.persistence.journal.sqlite""
                                                    recovery-event-timeout = 2s
                                                    circuit-breaker.reset-timeout = 2s
                                                    sqlite {
                                                            class = ""Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite""
                                                            auto-initialize = on
                                                            connection-string = ""Filename=file:memdb.db;Mode=Memory;Cache=Shared"" #Invalid connetion string             
                                                     }}
                                         snapshot-store {
                                                plugin = ""akka.persistence.snapshot-store.sqlite""
                                                sqlite {
                                                class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                                                auto-initialize = on
                                                connection-string = ""Fake=file:memdb.db;Mode=Memory;Cache=Shared""
                       }
                   }}";
    }
}

