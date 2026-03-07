using ClassDutyHelper.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ClassDutyHelper.Services
{
    public class AppDbContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<DutyProject> DutyProjects { get; set; }
        public DbSet<DutyRecord> DutyRecords { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<SyncLog> SyncLogs { get; set; }
        public DbSet<TimeSlotProject> TimeSlotProjects { get; set; }
        public DbSet<CarouselItem> CarouselItems { get; set; }

        private static string DbPath { get; set; } = string.Empty;

        public static void InitializeDatabasePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "ClassDutyHelper");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            DbPath = Path.Combine(appFolder, "duty.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (string.IsNullOrEmpty(DbPath))
            {
                InitializeDatabasePath();
            }
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<DutyProject>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<DutyRecord>()
                .HasIndex(d => new { d.DutyDate, d.DutyProjectId, d.StudentId });

            modelBuilder.Entity<AppSettings>()
                .HasData(new AppSettings { Id = 1 });
        }

        public static string GetDatabasePath()
        {
            if (string.IsNullOrEmpty(DbPath))
            {
                InitializeDatabasePath();
            }
            return DbPath;
        }

        public static string GetBackupFolderPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var backupFolder = Path.Combine(appDataPath, "ClassDutyHelper", "Backups");
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }
            return backupFolder;
        }

        public void MigrateDatabase()
        {
            var connection = Database.GetDbConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            
            command.CommandText = "PRAGMA table_info(AppSettings)";
            using var reader = command.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
            {
                columns.Add(reader.GetString(1));
            }
            reader.Close();

            if (!columns.Contains("TopBarPosition"))
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE AppSettings ADD COLUMN TopBarPosition INTEGER NOT NULL DEFAULT 0";
                alterCmd.ExecuteNonQuery();
            }

            if (!columns.Contains("HideMainWindowOnStart"))
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE AppSettings ADD COLUMN HideMainWindowOnStart INTEGER NOT NULL DEFAULT 0";
                alterCmd.ExecuteNonQuery();
            }

            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='TimeSlotProjects'";
            var result = command.ExecuteScalar();
            if (result == null)
            {
                using var createCmd = connection.CreateCommand();
                createCmd.CommandText = @"
                    CREATE TABLE TimeSlotProjects (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        StartTime TEXT NOT NULL,
                        EndTime TEXT NOT NULL,
                        ProjectIds TEXT NOT NULL DEFAULT '',
                        IsEnabled INTEGER NOT NULL DEFAULT 1,
                        SortOrder INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    )";
                createCmd.ExecuteNonQuery();
            }

            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='CarouselItems'";
            var carouselResult = command.ExecuteScalar();
            if (carouselResult == null)
            {
                using var createCmd = connection.CreateCommand();
                createCmd.CommandText = @"
                    CREATE TABLE CarouselItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Content TEXT NOT NULL,
                        SortOrder INTEGER NOT NULL DEFAULT 0,
                        IsEnabled INTEGER NOT NULL DEFAULT 1,
                        IsSystemDefault INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    )";
                createCmd.ExecuteNonQuery();
            }
        }
    }
}
