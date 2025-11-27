using System;
using System.Collections.Generic;
using System.IO;
using ClipMaster.Models;
using Microsoft.Data.Sqlite;

namespace ClipMaster.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection? _connection;

        public DatabaseService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClipMaster");
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            var dbPath = Path.Combine(appDataPath, "cliphistory.db");
            _connectionString = $"Data Source={dbPath}";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ClipHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Text TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    SourceApp TEXT,
                    IsFavorite INTEGER DEFAULT 0,
                    Category TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_timestamp ON ClipHistory(Timestamp DESC);
                CREATE INDEX IF NOT EXISTS idx_favorite ON ClipHistory(IsFavorite);
            ";
            createTableCmd.ExecuteNonQuery();
        }

        public int AddClip(ClipItem clip)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Check for duplicate (same text within last second)
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = @"
                SELECT COUNT(*) FROM ClipHistory 
                WHERE Text = @text AND Timestamp > datetime('now', '-1 second')";
            checkCmd.Parameters.AddWithValue("@text", clip.Text);
            
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());
            if (count > 0) return -1;

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO ClipHistory (Text, Timestamp, SourceApp, IsFavorite, Category)
                VALUES (@text, @timestamp, @sourceApp, @isFavorite, @category);
                SELECT last_insert_rowid();";
            
            insertCmd.Parameters.AddWithValue("@text", clip.Text);
            insertCmd.Parameters.AddWithValue("@timestamp", clip.Timestamp.ToString("o"));
            insertCmd.Parameters.AddWithValue("@sourceApp", clip.SourceApp ?? "");
            insertCmd.Parameters.AddWithValue("@isFavorite", clip.IsFavorite ? 1 : 0);
            insertCmd.Parameters.AddWithValue("@category", clip.Category ?? "");

            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        public List<ClipItem> GetAllClips(int limit = 500)
        {
            var clips = new List<ClipItem>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Text, Timestamp, SourceApp, IsFavorite, Category 
                FROM ClipHistory 
                ORDER BY Timestamp DESC 
                LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                clips.Add(new ClipItem
                {
                    Id = reader.GetInt32(0),
                    Text = reader.GetString(1),
                    Timestamp = DateTime.Parse(reader.GetString(2)),
                    SourceApp = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    IsFavorite = reader.GetInt32(4) == 1,
                    Category = reader.IsDBNull(5) ? "" : reader.GetString(5)
                });
            }

            return clips;
        }

        public List<ClipItem> SearchClips(string searchTerm)
        {
            var clips = new List<ClipItem>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Text, Timestamp, SourceApp, IsFavorite, Category 
                FROM ClipHistory 
                WHERE Text LIKE @search
                ORDER BY Timestamp DESC 
                LIMIT 100";
            cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                clips.Add(new ClipItem
                {
                    Id = reader.GetInt32(0),
                    Text = reader.GetString(1),
                    Timestamp = DateTime.Parse(reader.GetString(2)),
                    SourceApp = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    IsFavorite = reader.GetInt32(4) == 1,
                    Category = reader.IsDBNull(5) ? "" : reader.GetString(5)
                });
            }

            return clips;
        }

        public void UpdateClip(ClipItem clip)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE ClipHistory 
                SET Text = @text, IsFavorite = @isFavorite, Category = @category
                WHERE Id = @id";
            
            cmd.Parameters.AddWithValue("@id", clip.Id);
            cmd.Parameters.AddWithValue("@text", clip.Text);
            cmd.Parameters.AddWithValue("@isFavorite", clip.IsFavorite ? 1 : 0);
            cmd.Parameters.AddWithValue("@category", clip.Category ?? "");

            cmd.ExecuteNonQuery();
        }

        public void DeleteClip(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ClipHistory WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void ClearHistory(bool keepFavorites = true)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = keepFavorites 
                ? "DELETE FROM ClipHistory WHERE IsFavorite = 0"
                : "DELETE FROM ClipHistory";
            cmd.ExecuteNonQuery();
        }

        public void TrimHistory(int maxItems)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM ClipHistory 
                WHERE Id NOT IN (
                    SELECT Id FROM ClipHistory 
                    WHERE IsFavorite = 1
                    UNION
                    SELECT Id FROM ClipHistory 
                    WHERE IsFavorite = 0
                    ORDER BY Timestamp DESC 
                    LIMIT @limit
                )";
            cmd.Parameters.AddWithValue("@limit", maxItems);
            cmd.ExecuteNonQuery();
        }

        public void ExportToFile(string filePath)
        {
            var clips = GetAllClips(int.MaxValue);
            var lines = new List<string>();
            
            lines.Add("ClipMaster Export - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            lines.Add(new string('=', 60));
            
            foreach (var clip in clips)
            {
                lines.Add($"\n[{clip.Timestamp:yyyy-MM-dd HH:mm:ss}] - {clip.SourceApp}");
                lines.Add(new string('-', 40));
                lines.Add(clip.Text);
                lines.Add("");
            }

            File.WriteAllLines(filePath, lines);
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
