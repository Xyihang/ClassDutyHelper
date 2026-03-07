using ClassDutyHelper.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;

namespace ClassDutyHelper.Services
{
    public class DataService
    {
        private readonly AppDbContext _context;
        private readonly object _lock = new object();

        public DataService()
        {
            _context = new AppDbContext();
            _context.Database.EnsureCreated();
        }

        public AppDbContext Context => _context;

        #region Student Operations
        public List<Student> GetAllStudents()
        {
            lock (_lock)
            {
                return _context.Students.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToList();
            }
        }

        public List<Student> GetEnabledStudents()
        {
            lock (_lock)
            {
                return _context.Students.Where(s => s.IsEnabled).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToList();
            }
        }

        public Student? GetStudentById(int id)
        {
            lock (_lock)
            {
                return _context.Students.Find(id);
            }
        }

        public Student? GetStudentByName(string name)
        {
            lock (_lock)
            {
                return _context.Students.FirstOrDefault(s => s.Name == name);
            }
        }

        public bool AddStudent(Student student)
        {
            lock (_lock)
            {
                if (_context.Students.Any(s => s.Name == student.Name))
                    return false;

                student.SortOrder = _context.Students.Count();
                student.CreatedAt = DateTime.Now;
                student.UpdatedAt = DateTime.Now;
                _context.Students.Add(student);
                _context.SaveChanges();
                return true;
            }
        }

        public void UpdateStudent(Student student)
        {
            lock (_lock)
            {
                student.UpdatedAt = DateTime.Now;
                _context.Students.Update(student);
                _context.SaveChanges();
            }
        }

        public void DeleteStudent(int id)
        {
            lock (_lock)
            {
                var student = _context.Students.Find(id);
                if (student != null)
                {
                    _context.Students.Remove(student);
                    _context.SaveChanges();
                }
            }
        }

        public void DeleteAllStudents()
        {
            lock (_lock)
            {
                _context.Students.RemoveRange(_context.Students);
                _context.SaveChanges();
            }
        }
        #endregion

        #region DutyProject Operations
        public List<DutyProject> GetAllDutyProjects()
        {
            lock (_lock)
            {
                return _context.DutyProjects.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();
            }
        }

        public List<DutyProject> GetEnabledDutyProjects()
        {
            lock (_lock)
            {
                return _context.DutyProjects.Where(p => p.IsEnabled).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();
            }
        }

        public DutyProject? GetDutyProjectById(int id)
        {
            lock (_lock)
            {
                return _context.DutyProjects.Find(id);
            }
        }

        public DutyProject? GetDutyProjectByName(string name)
        {
            lock (_lock)
            {
                return _context.DutyProjects.FirstOrDefault(p => p.Name == name);
            }
        }

        public bool AddDutyProject(DutyProject project)
        {
            lock (_lock)
            {
                if (_context.DutyProjects.Any(p => p.Name == project.Name))
                    return false;

                project.SortOrder = _context.DutyProjects.Count();
                project.CreatedAt = DateTime.Now;
                project.UpdatedAt = DateTime.Now;
                _context.DutyProjects.Add(project);
                _context.SaveChanges();
                return true;
            }
        }

        public void UpdateDutyProject(DutyProject project)
        {
            lock (_lock)
            {
                project.UpdatedAt = DateTime.Now;
                _context.DutyProjects.Update(project);
                _context.SaveChanges();
            }
        }

        public void DeleteDutyProject(int id)
        {
            lock (_lock)
            {
                var project = _context.DutyProjects.Find(id);
                if (project != null)
                {
                    _context.DutyProjects.Remove(project);
                    _context.SaveChanges();
                }
            }
        }
        #endregion

        #region DutyRecord Operations
        public List<DutyRecord> GetDutyRecordsByDate(DateTime date)
        {
            lock (_lock)
            {
                return _context.DutyRecords
                    .Include(d => d.Student)
                    .Include(d => d.DutyProject)
                    .Where(d => d.DutyDate.Date == date.Date)
                    .OrderBy(d => d.DutyProject!.SortOrder)
                    .ThenBy(d => d.Student!.Name)
                    .ToList();
            }
        }

        public List<DutyRecord> GetDutyRecordsByDateRange(DateTime startDate, DateTime endDate)
        {
            lock (_lock)
            {
                return _context.DutyRecords
                    .Include(d => d.Student)
                    .Include(d => d.DutyProject)
                    .Where(d => d.DutyDate.Date >= startDate.Date && d.DutyDate.Date <= endDate.Date)
                    .OrderBy(d => d.DutyDate)
                    .ThenBy(d => d.DutyProject!.SortOrder)
                    .ToList();
            }
        }

        public bool AddDutyRecord(DutyRecord record)
        {
            lock (_lock)
            {
                if (_context.DutyRecords.Any(d => d.DutyDate.Date == record.DutyDate.Date &&
                    d.DutyProjectId == record.DutyProjectId && d.StudentId == record.StudentId))
                    return false;

                if (_context.DutyRecords.Any(d => d.DutyDate.Date == record.DutyDate.Date && d.StudentId == record.StudentId))
                    return false;

                record.CreatedAt = DateTime.Now;
                record.UpdatedAt = DateTime.Now;
                _context.DutyRecords.Add(record);
                _context.SaveChanges();
                return true;
            }
        }

        public void UpdateDutyRecord(DutyRecord record)
        {
            lock (_lock)
            {
                record.UpdatedAt = DateTime.Now;
                _context.DutyRecords.Update(record);
                _context.SaveChanges();
            }
        }

        public void DeleteDutyRecord(int id)
        {
            lock (_lock)
            {
                var record = _context.DutyRecords.Find(id);
                if (record != null)
                {
                    _context.DutyRecords.Remove(record);
                    _context.SaveChanges();
                }
            }
        }

        public void DeleteDutyRecordsByDate(DateTime date)
        {
            lock (_lock)
            {
                var records = _context.DutyRecords.Where(d => d.DutyDate.Date == date.Date);
                _context.DutyRecords.RemoveRange(records);
                _context.SaveChanges();
            }
        }

        public void DeleteDutyRecordsByDateRange(DateTime startDate, DateTime endDate)
        {
            lock (_lock)
            {
                var records = _context.DutyRecords
                    .Where(d => d.DutyDate.Date >= startDate.Date && d.DutyDate.Date <= endDate.Date);
                _context.DutyRecords.RemoveRange(records);
                _context.SaveChanges();
            }
        }

        public void ClearAllDutyRecords()
        {
            lock (_lock)
            {
                _context.DutyRecords.RemoveRange(_context.DutyRecords);
                _context.SaveChanges();
            }
        }
        #endregion

        #region Reminder Operations
        public List<Reminder> GetAllReminders()
        {
            lock (_lock)
            {
                return _context.Reminders.AsEnumerable().OrderBy(r => r.SortOrder).ThenBy(r => r.Time).ToList();
            }
        }

        public List<Reminder> GetEnabledReminders()
        {
            lock (_lock)
            {
                return _context.Reminders.Where(r => r.IsEnabled).AsEnumerable().OrderBy(r => r.SortOrder).ThenBy(r => r.Time).ToList();
            }
        }

        public void AddReminder(Reminder reminder)
        {
            lock (_lock)
            {
                reminder.SortOrder = _context.Reminders.Count();
                reminder.CreatedAt = DateTime.Now;
                reminder.UpdatedAt = DateTime.Now;
                _context.Reminders.Add(reminder);
                _context.SaveChanges();
            }
        }

        public void UpdateReminder(Reminder reminder)
        {
            lock (_lock)
            {
                reminder.UpdatedAt = DateTime.Now;
                _context.Reminders.Update(reminder);
                _context.SaveChanges();
            }
        }

        public void DeleteReminder(int id)
        {
            lock (_lock)
            {
                var reminder = _context.Reminders.Find(id);
                if (reminder != null)
                {
                    _context.Reminders.Remove(reminder);
                    _context.SaveChanges();
                }
            }
        }
        #endregion

        #region AppSettings Operations
        public AppSettings GetAppSettings()
        {
            lock (_lock)
            {
                return _context.AppSettings.First();
            }
        }

        public void UpdateAppSettings(AppSettings settings)
        {
            lock (_lock)
            {
                settings.UpdatedAt = DateTime.Now;
                _context.AppSettings.Update(settings);
                _context.SaveChanges();
            }
        }
        #endregion

        #region Backup Operations
        public string CreateBackup()
        {
            lock (_lock)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
                var backupFileName = $"backup_{timestamp}.db";
                var backupPath = Path.Combine(AppDbContext.GetBackupFolderPath(), backupFileName);

                _context.Database.CloseConnection();
                File.Copy(AppDbContext.GetDatabasePath(), backupPath, true);

                var backupFiles = Directory.GetFiles(AppDbContext.GetBackupFolderPath(), "backup_*.db")
                    .OrderByDescending(f => f)
                    .Skip(3);

                foreach (var file in backupFiles)
                {
                    File.Delete(file);
                }

                return backupPath;
            }
        }

        public bool RestoreBackup(string backupPath)
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(backupPath))
                        return false;

                    _context.Database.CloseConnection();
                    File.Copy(backupPath, AppDbContext.GetDatabasePath(), true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public List<string> GetBackupFiles()
        {
            lock (_lock)
            {
                var folder = AppDbContext.GetBackupFolderPath();
                if (!Directory.Exists(folder))
                    return new List<string>();

                return Directory.GetFiles(folder, "backup_*.db")
                    .OrderByDescending(f => f)
                    .ToList();
            }
        }
        #endregion

        #region Auto Schedule
        public int AutoSchedule(int mode, int cycleDays, DateTime startDate, DateTime endDate, bool weekdaysOnly, bool skipHolidays = false, List<DateTime>? excludeDates = null)
        {
            lock (_lock)
            {
                var students = _context.Students.Where(s => s.IsEnabled).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToList();
                var projects = _context.DutyProjects.Where(p => p.IsEnabled).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();

                if (students.Count == 0 || projects.Count == 0)
                    return 0;

                var studentIndex = 0;
                var scheduledCount = 0;

                var currentDate = startDate;
                while (currentDate <= endDate)
                {
                    if (weekdaysOnly && (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday))
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    if (skipHolidays && IsHoliday(currentDate))
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    if (excludeDates != null && excludeDates.Contains(currentDate.Date))
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    var existingRecords = _context.DutyRecords
                        .Where(d => d.DutyDate.Date == currentDate.Date)
                        .ToList();

                    if (!existingRecords.Any())
                    {
                        foreach (var project in projects)
                        {
                            for (int i = 0; i < project.DefaultPersonCount; i++)
                            {
                                if (mode == 0)
                                {
                                    var attempts = 0;
                                    while (attempts < students.Count)
                                    {
                                        var student = students[studentIndex % students.Count];
                                        studentIndex++;
                                        attempts++;

                                        if (!_context.DutyRecords.Any(d => d.DutyDate.Date == currentDate.Date && d.StudentId == student.Id))
                                        {
                                            var record = new DutyRecord
                                            {
                                                DutyDate = currentDate,
                                                DutyProjectId = project.Id,
                                                StudentId = student.Id
                                            };
                                            _context.DutyRecords.Add(record);
                                            scheduledCount++;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    var groups = students.GroupBy(s => s.Group ?? "").ToList();
                                    if (groups.Count > 0)
                                    {
                                        var groupIndex = (scheduledCount + i) % groups.Count;
                                        var groupStudents = groups[groupIndex].ToList();
                                        var student = groupStudents[scheduledCount % groupStudents.Count];

                                        if (!_context.DutyRecords.Any(d => d.DutyDate.Date == currentDate.Date && d.StudentId == student.Id))
                                        {
                                            var record = new DutyRecord
                                            {
                                                DutyDate = currentDate,
                                                DutyProjectId = project.Id,
                                                StudentId = student.Id
                                            };
                                            _context.DutyRecords.Add(record);
                                            scheduledCount++;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                }

                _context.SaveChanges();
                return scheduledCount;
            }
        }

        private bool IsHoliday(DateTime date)
        {
            var holidays2024 = new List<DateTime>
            {
                new DateTime(2024, 1, 1),
                new DateTime(2024, 2, 10),
                new DateTime(2024, 2, 11),
                new DateTime(2024, 2, 12),
                new DateTime(2024, 2, 13),
                new DateTime(2024, 2, 14),
                new DateTime(2024, 2, 15),
                new DateTime(2024, 2, 16),
                new DateTime(2024, 2, 17),
                new DateTime(2024, 4, 4),
                new DateTime(2024, 4, 5),
                new DateTime(2024, 4, 6),
                new DateTime(2024, 5, 1),
                new DateTime(2024, 5, 2),
                new DateTime(2024, 5, 3),
                new DateTime(2024, 5, 4),
                new DateTime(2024, 5, 5),
                new DateTime(2024, 6, 10),
                new DateTime(2024, 9, 15),
                new DateTime(2024, 9, 16),
                new DateTime(2024, 9, 17),
                new DateTime(2024, 10, 1),
                new DateTime(2024, 10, 2),
                new DateTime(2024, 10, 3),
                new DateTime(2024, 10, 4),
                new DateTime(2024, 10, 5),
                new DateTime(2024, 10, 6),
                new DateTime(2024, 10, 7),
            };

            var holidays2025 = new List<DateTime>
            {
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 28),
                new DateTime(2025, 1, 29),
                new DateTime(2025, 1, 30),
                new DateTime(2025, 1, 31),
                new DateTime(2025, 2, 1),
                new DateTime(2025, 2, 2),
                new DateTime(2025, 2, 3),
                new DateTime(2025, 2, 4),
                new DateTime(2025, 4, 4),
                new DateTime(2025, 4, 5),
                new DateTime(2025, 4, 6),
                new DateTime(2025, 5, 1),
                new DateTime(2025, 5, 2),
                new DateTime(2025, 5, 3),
                new DateTime(2025, 5, 4),
                new DateTime(2025, 5, 5),
                new DateTime(2025, 5, 31),
                new DateTime(2025, 6, 1),
                new DateTime(2025, 6, 2),
                new DateTime(2025, 10, 1),
                new DateTime(2025, 10, 2),
                new DateTime(2025, 10, 3),
                new DateTime(2025, 10, 4),
                new DateTime(2025, 10, 5),
                new DateTime(2025, 10, 6),
                new DateTime(2025, 10, 7),
                new DateTime(2025, 10, 8),
            };

            return holidays2024.Contains(date.Date) || holidays2025.Contains(date.Date);
        }
        #endregion

        #region TimeSlotProject Operations
        public List<TimeSlotProject> GetAllTimeSlotProjects()
        {
            lock (_lock)
            {
                return _context.TimeSlotProjects.ToList().OrderBy(t => t.SortOrder).ThenBy(t => t.StartTime).ToList();
            }
        }

        public List<TimeSlotProject> GetEnabledTimeSlotProjects()
        {
            lock (_lock)
            {
                return _context.TimeSlotProjects.Where(t => t.IsEnabled).ToList().OrderBy(t => t.SortOrder).ThenBy(t => t.StartTime).ToList();
            }
        }

        public TimeSlotProject? GetTimeSlotProjectById(int id)
        {
            lock (_lock)
            {
                return _context.TimeSlotProjects.Find(id);
            }
        }

        public TimeSlotProject? GetCurrentTimeSlotProject()
        {
            lock (_lock)
            {
                var now = DateTime.Now.TimeOfDay;
                var allSlots = _context.TimeSlotProjects
                    .Where(t => t.IsEnabled)
                    .ToList();
                
                return allSlots
                    .Where(t => t.StartTime <= now && t.EndTime >= now)
                    .OrderBy(t => t.StartTime)
                    .FirstOrDefault();
            }
        }

        public List<int> GetProjectIdsForCurrentTimeSlot()
        {
            lock (_lock)
            {
                var timeSlot = GetCurrentTimeSlotProject();
                if (timeSlot == null || string.IsNullOrEmpty(timeSlot.ProjectIds))
                    return new List<int>();

                return timeSlot.ProjectIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();
            }
        }

        public void AddTimeSlotProject(TimeSlotProject timeSlot)
        {
            lock (_lock)
            {
                timeSlot.CreatedAt = DateTime.Now;
                timeSlot.UpdatedAt = DateTime.Now;
                timeSlot.SortOrder = _context.TimeSlotProjects.Count();
                _context.TimeSlotProjects.Add(timeSlot);
                _context.SaveChanges();
            }
        }

        public void UpdateTimeSlotProject(TimeSlotProject timeSlot)
        {
            lock (_lock)
            {
                timeSlot.UpdatedAt = DateTime.Now;
                _context.TimeSlotProjects.Update(timeSlot);
                _context.SaveChanges();
            }
        }

        public void DeleteTimeSlotProject(int id)
        {
            lock (_lock)
            {
                var timeSlot = _context.TimeSlotProjects.Find(id);
                if (timeSlot != null)
                {
                    _context.TimeSlotProjects.Remove(timeSlot);
                    _context.SaveChanges();
                }
            }
        }
        #endregion

        #region CarouselItem Operations
        public List<CarouselItem> GetAllCarouselItems()
        {
            lock (_lock)
            {
                return _context.CarouselItems.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToList();
            }
        }

        public List<CarouselItem> GetEnabledCarouselItems()
        {
            lock (_lock)
            {
                return _context.CarouselItems.Where(c => c.IsEnabled).OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToList();
            }
        }

        public CarouselItem? GetCarouselItemById(int id)
        {
            lock (_lock)
            {
                return _context.CarouselItems.Find(id);
            }
        }

        public void AddCarouselItem(CarouselItem item)
        {
            lock (_lock)
            {
                item.CreatedAt = DateTime.Now;
                item.UpdatedAt = DateTime.Now;
                _context.CarouselItems.Add(item);
                _context.SaveChanges();
            }
        }

        public void UpdateCarouselItem(CarouselItem item)
        {
            lock (_lock)
            {
                item.UpdatedAt = DateTime.Now;
                _context.CarouselItems.Update(item);
                _context.SaveChanges();
            }
        }

        public void DeleteCarouselItem(int id)
        {
            lock (_lock)
            {
                var item = _context.CarouselItems.Find(id);
                if (item != null)
                {
                    _context.CarouselItems.Remove(item);
                    _context.SaveChanges();
                }
            }
        }

        public void UpdateCarouselItemOrder(int id, int newSortOrder)
        {
            lock (_lock)
            {
                var item = _context.CarouselItems.Find(id);
                if (item != null)
                {
                    item.SortOrder = newSortOrder;
                    item.UpdatedAt = DateTime.Now;
                    _context.SaveChanges();
                }
            }
        }

        public void InitializeDefaultCarouselItems()
        {
            lock (_lock)
            {
                if (!_context.CarouselItems.Any())
                {
                    var defaultItems = new List<CarouselItem>
                    {
                        new CarouselItem { Content = "组长检查合格后方可离开。", SortOrder = 1, IsSystemDefault = true },
                        new CarouselItem { Content = "倒垃圾和整理工具是在每次室内/外打扫完毕后完成", SortOrder = 2, IsSystemDefault = true },
                        new CarouselItem { Content = "没做值日的组长负责督促完成值日。", SortOrder = 3, IsSystemDefault = true },
                        new CarouselItem { Content = "每位同学每周安排一次值日，请务必认真完成。", SortOrder = 4, IsSystemDefault = true }
                    };
                    _context.CarouselItems.AddRange(defaultItems);
                    _context.SaveChanges();
                }
            }
        }
        #endregion

        public void SaveChanges()
        {
            lock (_lock)
            {
                _context.SaveChanges();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _context.Dispose();
            }
        }
    }
}
