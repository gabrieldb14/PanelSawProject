using System;
using System.Data.SQLite;
using System.IO;
using static HMI_PanelSaw.Service.PasswordHashingService;

namespace HMI_PanelSaw.Service
{
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed = false;

        public DatabaseService(string databasePath = "users.db")
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(appDirectory, databasePath);
            _connectionString = $"Data Source={fullPath};Version=3;";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool isNewDatabase = !File.Exists(GetDatabasePath());

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                CreateUsersTable(connection);
                CreateUsernameIndex(connection);

                if (isNewDatabase)
                {
                    CreateUsersTable(connection);
                    CreateUsernameIndex(connection);
                    SeedDefaultUsers(connection);
                }

            }
        }

        private void CreateUsersTable(SQLiteConnection connection)
        {
            const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                    PasswordHash TEXT NOT NULL,
                    PasswordSalt TEXT,
                    Role INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    LastLoginAt TEXT,
                    IsActive INTEGER DEFAULT 1,
                    FailedLoginAttempts INTEGER DEFAULT 0,
                    LockedUntil TEXT,
                    ForcePasswordChange INTEGER DEFAULT 0,
                    LastPasswordChange TEXT
                );";

            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private void CreateUsernameIndex(SQLiteConnection connection)
        {
            const string createIndexQuery = @"
                CREATE INDEX IF NOT EXISTS idx_username 
                ON Users(Username);";

            using (var command = new SQLiteCommand(createIndexQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private void SeedDefaultUsers(SQLiteConnection connection)
        {
            var defaultUsers = new[]
            {
                new { Username = "operator", Password = "Op123456", Role = 0 },
                new { Username = "supervisor", Password = "Sup123456", Role = 1 },
                new { Username = "admin", Password = "Admin123456", Role = 2 }
            };

            const string insertQuery = @"
                INSERT INTO Users (Username, PasswordHash, Role, CreatedAt, IsActive, LastPasswordChange, ForcePasswordChange)
                VALUES (@Username, @PasswordHash, @Role, @CreatedAt, 1, @LastPasswordChange, 1);";

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (var user in defaultUsers)
                    {
                        using (var command = new SQLiteCommand(insertQuery, connection, transaction))
                        {
                            var hashedPassword = PasswordHashingService.HashPassword(user.Password);

                            command.Parameters.AddWithValue("@Username", user.Username);
                            command.Parameters.AddWithValue("@PasswordHash", hashedPassword.ToStorageString());
                            command.Parameters.AddWithValue("@Role", user.Role);
                            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            command.Parameters.AddWithValue("@LastPasswordChange", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public SQLiteConnection GetConnection()
        {
            ThrowIfDisposed();

            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private string GetDatabasePath()
        {
            var builder = new SQLiteConnectionStringBuilder(_connectionString);
            return builder.DataSource;
        }


        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatabaseService));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // No persistent connection to dispose
                    // Each GetConnection() creates a new connection
                }
                _disposed = true;
            }
        }

        ~DatabaseService()
        {
            Dispose(false);
        }
    }
}