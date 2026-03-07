using ClassDutyHelper.Models;
using ElapsedEventArgs = System.Timers.ElapsedEventArgs;
using SystemTimer = System.Timers.Timer;

namespace ClassDutyHelper.Services
{
    public class ReminderService : IDisposable
    {
        private readonly DataService _dataService;
        private readonly SystemTimer _checkTimer;
        private readonly List<Reminder> _triggeredReminders = new();
        private readonly List<Reminder> _warningReminders = new();
        private readonly object _lock = new();

        public event Action<Reminder>? ReminderTriggered;
        public event Action<Reminder, int>? WarningTriggered;
        public event Action? RemindersCleared;

        public ReminderService(DataService dataService)
        {
            _dataService = dataService;
            _checkTimer = new SystemTimer(1000);
            _checkTimer.Elapsed += CheckReminders;
            _checkTimer.Start();
        }

        private void CheckReminders(object? sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                try
                {
                    var now = DateTime.Now;
                    var currentTime = now.TimeOfDay;
                    var reminders = _dataService.GetEnabledReminders();

                    foreach (var reminder in reminders)
                    {
                        var reminderTime = reminder.Time;
                        var warningTime = reminderTime.Subtract(TimeSpan.FromMinutes(reminder.AdvanceMinutes));

                        if (currentTime >= warningTime && currentTime < reminderTime)
                        {
                            if (!_warningReminders.Contains(reminder))
                            {
                                _warningReminders.Add(reminder);
                                var remainingMinutes = (int)Math.Ceiling((reminderTime - currentTime).TotalMinutes);
                                WarningTriggered?.Invoke(reminder, remainingMinutes);
                            }
                        }

                        if (currentTime.Hours == reminderTime.Hours &&
                            currentTime.Minutes == reminderTime.Minutes &&
                            currentTime.Seconds < 2)
                        {
                            if (!_triggeredReminders.Contains(reminder))
                            {
                                _triggeredReminders.Add(reminder);
                                _warningReminders.Remove(reminder);
                                TriggerReminder(reminder);
                            }
                        }
                    }

                    if (now.Hour == 0 && now.Minute == 0 && now.Second < 2)
                    {
                        _triggeredReminders.Clear();
                        _warningReminders.Clear();
                    }
                }
                catch
                {
                }
            }
        }

        private void TriggerReminder(Reminder reminder)
        {
            ReminderTriggered?.Invoke(reminder);
        }

        public void ClearReminder(Reminder reminder)
        {
            lock (_lock)
            {
                _triggeredReminders.Remove(reminder);
                _warningReminders.Remove(reminder);
                RemindersCleared?.Invoke();
            }
        }

        public void ClearAllReminders()
        {
            lock (_lock)
            {
                _triggeredReminders.Clear();
                _warningReminders.Clear();
                RemindersCleared?.Invoke();
            }
        }

        public Reminder? GetActiveWarning()
        {
            lock (_lock)
            {
                return _warningReminders.Count > 0 ? _warningReminders[0] : null;
            }
        }

        public int GetWarningMinutes(Reminder reminder)
        {
            var now = DateTime.Now.TimeOfDay;
            var remaining = reminder.Time - now;
            return (int)Math.Ceiling(remaining.TotalMinutes);
        }

        public void Dispose()
        {
            _checkTimer?.Stop();
            _checkTimer?.Dispose();
        }
    }
}
