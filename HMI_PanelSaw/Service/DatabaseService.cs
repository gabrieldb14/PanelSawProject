using System;
using System.Data.SQLite;
using System.IO;

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
                    Role INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    LastLoginAt TEXT,
                    IsActive INTEGER DEFAULT 1,
                    FailedLoginAttempts INTEGER DEFAULT 0,
                    LockedUntil TEXT
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
                new { Username = "operator", Password = "1234", Role = 0 },
                new { Username = "supervisor", Password = "1234", Role = 1 },
                new { Username = "admin", Password = "admin123", Role = 2 }
            };

            const string insertQuery = @"
                INSERT INTO Users (Username, PasswordHash, Role, CreatedAt, IsActive)
                VALUES (@Username, @PasswordHash, @Role, @CreatedAt, 1);";

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (var user in defaultUsers)
                    {
                        using (var command = new SQLiteCommand(insertQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Username", user.Username);
                            command.Parameters.AddWithValue("@PasswordHash", HashPassword(user.Password));
                            command.Parameters.AddWithValue("@Role", user.Role);
                            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
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