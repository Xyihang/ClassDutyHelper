using System.Net.Http;
using System.Text.Json;

namespace ClassDutyHelper.Services
{
    public class SyncService
    {
        private readonly DataService _dataService;
        private readonly HttpClient _httpClient;
        private readonly System.Timers.Timer _syncTimer;

        public event Action<string>? SyncCompleted;
        public event Action<string>? SyncError;

        public SyncService(DataService dataService)
        {
            _dataService = dataService;
            _httpClient = new HttpClient();
            _syncTimer = new System.Timers.Timer();
            _syncTimer.Elapsed += async (s, e) => await SyncAsync();
        }

        public void StartSync(int intervalMinutes)
        {
            _syncTimer.Interval = intervalMinutes * 60 * 1000;
            _syncTimer.Start();
        }

        public void StopSync()
        {
            _syncTimer.Stop();
        }

        public async Task<bool> SyncAsync()
        {
            var settings = _dataService.GetAppSettings();

            if (string.IsNullOrEmpty(settings.CloudFormUrl) || string.IsNullOrEmpty(settings.AdminKey))
            {
                return false;
            }

            try
            {
                var response = await _httpClient.GetStringAsync(settings.CloudFormUrl);
                var changes = ParseCloudFormData(response, settings.AdminKey);

                if (changes.Count == 0)
                {
                    return true;
                }

                foreach (var change in changes)
                {
                    ApplyChange(change);
                }

                settings.LastSyncTime = DateTime.Now;
                _dataService.UpdateAppSettings(settings);

                SyncCompleted?.Invoke($"同步成功，共{changes.Count}条换班记录");
                return true;
            }
            catch (Exception ex)
            {
                SyncError?.Invoke($"同步失败：{ex.Message}");
                return false;
            }
        }

        private List<ChangeRecord> ParseCloudFormData(string data, string adminKey)
        {
            var changes = new List<ChangeRecord>();

            try
            {
                var jsonDoc = JsonDocument.Parse(data);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("records", out var records))
                {
                    foreach (var record in records.EnumerateArray())
                    {
                        var key = record.GetProperty("adminKey").GetString();
                        if (key != adminKey)
                            continue;

                        var change = new ChangeRecord
                        {
                            Date = DateTime.Parse(record.GetProperty("date").GetString() ?? ""),
                            ProjectName = record.GetProperty("project").GetString() ?? "",
                            OriginalStudentName = record.GetProperty("originalStudent").GetString() ?? "",
                            NewStudentName = record.GetProperty("newStudent").GetString() ?? ""
                        };

                        changes.Add(change);
                    }
                }
            }
            catch
            {
            }

            return changes;
        }

        private void ApplyChange(ChangeRecord change)
        {
            var project = _dataService.GetDutyProjectByName(change.ProjectName);
            var originalStudent = _dataService.GetStudentByName(change.OriginalStudentName);
            var newStudent = _dataService.GetStudentByName(change.NewStudentName);

            if (project == null || originalStudent == null || newStudent == null)
                return;

            var records = _dataService.GetDutyRecordsByDate(change.Date);
            var record = records.Find(r => r.DutyProjectId == project.Id && r.StudentId == originalStudent.Id);

            if (record != null)
            {
                record.StudentId = newStudent.Id;
                record.UpdatedAt = DateTime.Now;
                _dataService.UpdateDutyRecord(record);
            }
        }
    }

    public class ChangeRecord
    {
        public DateTime Date { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string OriginalStudentName { get; set; } = string.Empty;
        public string NewStudentName { get; set; } = string.Empty;
    }
}
