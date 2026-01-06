using System;
using System.Collections.Generic;
using System.Data.SQLite;
using HMI_PanelSaw.Models;

namespace HMI_PanelSaw.Service
{
    public class UserRepository :IDisposable
    {
        private readonly DatabaseService _database;

        public UserRepository()
        {
            _database = new DatabaseService();
        }

        public User GetByUsername(string username)
        {
            string query =
                @"
                SELECT Id, Username, PasswordHash, PasswordSalt, Role, CreatedAt, LastLoginAt, 
                       IsActive, FailedLoginAttempts, LockedUntil, ForcePasswordChange, LastPasswordChange
                FROM Users
                WHERE Username = @Username COLLATE NOCASE AND IsActive = 1;
                ";
            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);

                using(var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapReaderToUser(reader);
                    }
                }
            }

            return null;
        }
        public List<User> GetAllUsers()
        {
            var users = new List<User>();
            string query = @"
                SELECT Id, Username, PasswordHash, PasswordSalt, Role, CreatedAt, LastLoginAt, 
                       IsActive, FailedLoginAttempts, LockedUntil, ForcePasswordChange, LastPasswordChange
                FROM Users 
                WHERE IsActive = 1
                ORDER BY Username;";

            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(MapReaderToUser(reader));
                }
            }

            return users;
        }

        public void UpdateLastLogin(string username)
        {
            string query = @"
                UPDATE Users 
                SET LastLoginAt = @LastLoginAt,
                    FailedLoginAttempts = 0,
                    LockedUntil = NULL
                WHERE Username = @Username;";

            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@LastLoginAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Username", username);
                command.ExecuteNonQuery();
            }
        }

        public void IncrementFailedLoginAttempts(string username, int maxAttempts, int lockoutMinutes)
        {
            string query = @"
                UPDATE Users 
                SET FailedLoginAttempts = FailedLoginAttempts + 1,
                    LockedUntil = CASE 
                        WHEN FailedLoginAttempts + 1 >= @MaxAttempts 
                        THEN @LockedUntil 
                        ELSE LockedUntil 
                    END
                WHERE Username = @Username;";

            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@MaxAttempts", maxAttempts);
                command.Parameters.AddWithValue("@LockedUntil",
                    DateTime.Now.AddMinutes(lockoutMinutes).ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Username", username);
                command.ExecuteNonQuery();
            }
        }

        public bool IsAccountLocked(string username)
        {
            string query = @"
                SELECT LockedUntil 
                FROM Users 
                WHERE Username = @Username;";

            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    DateTime lockedUntil = DateTime.Parse(result.ToString());
                    return DateTime.Now < lockedUntil;
                }
            }

            return false;
        }

        public void AddUser(User user)
        {
            string query = @"
                INSERT INTO Users (Username, PasswordHash, Role, CreatedAt, IsActive, LastPasswordChange, ForcePasswordChange)
                VALUES (@Username, @PasswordHash, @Role, @CreatedAt, 1, @LastPasswordChange, @ForcePasswordChange);";

            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@Role", (int)user.Role);
                command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@LastPasswordChange", user.LastPasswordChange?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ForcePasswordChange", user.ForcePasswordChange ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }


        public void UpdatePassword(string username, string newPasswordHash)
        {
            string query = @"
                UPDATE Users 
                SET PasswordHash = @PasswordHash,
                    FailedLoginAttempts = 0,
                    LockedUntil = NULL,
                    LastPasswordChange = @LastPasswordChange,
                    ForcePasswordChange = 0
                WHERE Username = @Username;";

            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@LastPasswordChange", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
            }
        }

        public void DeleteUser(string username)
        {
            // Soft delete - set IsActive to 0
            string query = @"
                UPDATE Users 
                SET IsActive = 0
                WHERE Username = @Username;";

            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                command.ExecuteNonQuery();
            }
        }

        public void SetForcePasswordChange(string username, bool forceChange)
        {
            string query = @"
                UPDATE Users 
                SET ForcePasswordChange = @ForcePasswordChange
                WHERE Username = @Username;";

            using (var connection = _database.GetConnection())
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@ForcePasswordChange", forceChange ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        private User MapReaderToUser(SQLiteDataReader reader)
        {
            return new User
            {
                Id = Convert.ToInt32(reader["Id"]),
                Username = reader["Username"].ToString(),
                PasswordHash = reader["PasswordHash"].ToString(),
                PasswordSalt = reader["PasswordSalt"] != DBNull.Value ? reader["PasswordSalt"].ToString() : null,
                Role = (UserRole)Convert.ToInt32(reader["Role"]),
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                LastLoginAt = reader["LastLoginAt"] != DBNull.Value ? DateTime.Parse(reader["LastLoginAt"].ToString()) : (DateTime?)null,
                FailedLoginAttempts = Convert.ToInt32(reader["FailedLoginAttempts"]),
                LockedUntil = reader["LockedUntil"] != DBNull.Value ? DateTime.Parse(reader["LockedUntil"].ToString()) : (DateTime?)null,
                ForcePasswordChange = reader["ForcePasswordChange"] != DBNull.Value ? Convert.ToBoolean(reader["ForcePasswordChange"]) : false,
                LastPasswordChange = reader["LastPasswordChange"] != DBNull.Value ? DateTime.Parse(reader["LastPasswordChange"].ToString()) : (DateTime?)null,
            };
        }

        public void Dispose()
        {
            _database?.Dispose();
        }
    }

}
