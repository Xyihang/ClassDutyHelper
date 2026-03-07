using ClassDutyHelper.Services;
using ClassDutyHelper.Views;
using System.Windows;

namespace ClassDutyHelper
{
    public partial class App : Application
    {
        private DataService? _dataService;
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDbContext.InitializeDatabasePath();
            
            using (var context = new AppDbContext())
            {
                context.Database.EnsureCreated();
                context.MigrateDatabase();
            }

            _dataService = new DataService();
            _dataService.InitializeDefaultCarouselItems();
            var settings = _dataService.GetAppSettings();

            _mainWindow = new MainWindow();

            if (settings.HideMainWindowOnStart)
            {
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _dataService?.Dispose();
            base.OnExit(e);
        }
    }
}
